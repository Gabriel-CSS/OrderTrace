using Microsoft.Extensions.Logging;
using OrderTrace.Core.Entities;
using Polly;
using Polly.Retry;

namespace OrderTrace.Infrastructure.PaymentGateway;

public class PaymentGatewayMockService : IPaymentGatewayMockService
{
    private readonly ILogger<PaymentGatewayMockService> _logger;
    private const int MaxRetries = 3;
    private const string GatewayName = "MockGateway";
    private const double SuccessRate = 0.8; // 80% de chance de sucesso

    public PaymentGatewayMockService(ILogger<PaymentGatewayMockService> logger)
    {
        _logger = logger;
    }

    public async Task<PaymentGatewayResult> ProcessPaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        var transactions = new List<Transaction>();
        bool finalSuccess = false;

        // Configura a política de retry com Polly
        var retryPolicy = CreateRetryPolicy(payment.Id, transactions);

        try
        {
            // Executa com retry policy
            finalSuccess = await retryPolicy.ExecuteAsync(async (ct) =>
            {
                var transaction = await AttemptPaymentAsync(payment.Id, ct);
                transactions.Add(transaction);

                if (transaction.IsFailed())
                {
                    _logger.LogWarning(
                        "Tentativa de pagamento falhou. PaymentId: {PaymentId}, Attempt: {Attempt}, ResponseCode: {ResponseCode}",
                        payment.Id, transactions.Count, transaction.ResponseCode);

                    // Lança exceção para triggerar retry
                    throw new PaymentGatewayException($"Transação falhou com código: {transaction.ResponseCode}");
                }

                _logger.LogInformation(
                    "Pagamento processado com sucesso. PaymentId: {PaymentId}, TransactionId: {TransactionId}",
                    payment.Id, transaction.Id);

                return true;
            }, cancellationToken);
        }
        catch (PaymentGatewayException ex)
        {
            // Todas as tentativas falharam
            _logger.LogError(
                "Pagamento falhou após {MaxRetries} tentativas. PaymentId: {PaymentId}. Erro: {Error}",
                MaxRetries, payment.Id, ex.Message);

            finalSuccess = false;
        }

        return new PaymentGatewayResult
        {
            Success = finalSuccess,
            Transactions = transactions
        };
    }

    /// <summary>
    /// Cria a política de retry com Polly
    /// </summary>
    private ResiliencePipeline<bool> CreateRetryPolicy(Guid paymentId, List<Transaction> transactions)
    {
        return new ResiliencePipelineBuilder<bool>()
            .AddRetry(new RetryStrategyOptions<bool>
            {
                MaxRetryAttempts = MaxRetries - 1, // -1 porque a primeira tentativa não é retry
                Delay = TimeSpan.FromMilliseconds(100),
                BackoffType = DelayBackoffType.Linear,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<bool>()
                    .Handle<PaymentGatewayException>(),
                OnRetry = args =>
                {
                    _logger.LogInformation(
                        "Retry {Attempt} de {MaxRetries} para PaymentId: {PaymentId}. Aguardando {Delay}ms",
                        args.AttemptNumber + 1, MaxRetries, paymentId, args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Simula uma tentativa de pagamento no gateway
    /// </summary>
    private async Task<Transaction> AttemptPaymentAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        // Simula delay de rede/processamento (100ms a 1000ms)
        int delayMs = Random.Shared.Next(100, 1000);
        await Task.Delay(delayMs, cancellationToken);

        // Simula sucesso/falha baseado na taxa de sucesso configurada
        bool success = Random.Shared.NextDouble() < SuccessRate;

        var responseCode = success ? "00" : "99";
        var responseMessage = success ? "Approved" : "Insufficient funds";

        return Transaction.Create(
            paymentId: paymentId,
            gateway: GatewayName,
            responseCode: responseCode,
            responseMessage: responseMessage
        );
    }
}

/// <summary>
/// Exceção específica para falhas no gateway de pagamento
/// </summary>
public class PaymentGatewayException : Exception
{
    public PaymentGatewayException(string message) : base(message) { }
    public PaymentGatewayException(string message, Exception innerException) : base(message, innerException) { }
}

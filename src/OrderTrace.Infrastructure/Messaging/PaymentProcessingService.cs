using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderTrace.Core.Entities;
using OrderTrace.Infrastructure.Messaging.NotificationService;
using OrderTrace.Infrastructure.Messaging.PaymentQueue;
using OrderTrace.Infrastructure.PaymentGateway;
using System.Diagnostics;

namespace OrderTrace.Infrastructure.Messaging;

public class PaymentProcessingService(
    IPaymentQueue queue,
    IServiceScopeFactory scopeFactory,
    IPaymentGatewayMockService gateway,
    INotificationService notifier,
    ILogger<PaymentProcessingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PaymentProcessingService iniciado e aguardando pagamentos na fila");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var payment = await queue.DequeueAsync(stoppingToken);

                using var activity = Activity.Current?.Source.StartActivity("ProcessPayment");
                activity?.SetTag("payment.id", payment.Id);
                activity?.SetTag("payment.amount", payment.Amount);

                await ProcessPaymentAsync(payment, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("PaymentProcessingService está sendo encerrado");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro inesperado ao processar pagamento");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("PaymentProcessingService encerrado");
    }

    private async Task ProcessPaymentAsync(Payment payment, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Iniciando processamento de pagamento. PaymentId: {PaymentId}, OrderId: {OrderId}, Amount: {Amount}",
            payment.Id, payment.OrderId, payment.Amount);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderTraceDbContext>();

        try
        {
            var gatewayResult = await gateway.ProcessPaymentAsync(payment, cancellationToken);

            foreach (var transaction in gatewayResult.Transactions)
            {
                logger.LogInformation(
                    "Transação criada. TransactionId: {TransactionId}, PaymentId: {PaymentId}, Tentativa: {Attempt}, Status: {ResponseCode} - {ResponseMessage}",
                    transaction.Id, payment.Id, gatewayResult.Transactions.IndexOf(transaction) + 1,
                    transaction.ResponseCode, transaction.ResponseMessage);

                payment.AddTransaction(transaction);
            }

            if (gatewayResult.Success)
            {
                payment.Approve();
                logger.LogInformation(
                    "Pagamento aprovado após {Attempts} tentativa(s). PaymentId: {PaymentId}",
                    gatewayResult.AttemptsCount, payment.Id);
            }
            else
            {
                payment.Fail();
                logger.LogWarning(
                    "Pagamento falhou após {Attempts} tentativa(s). PaymentId: {PaymentId}",
                    gatewayResult.AttemptsCount, payment.Id);
            }

            await UpdateOrderStatusAsync(payment, db, cancellationToken);

            await SaveChangesWithRetryAsync(db, cancellationToken);

            await notifier.NotifyPaymentProcessed(payment);

            logger.LogInformation(
                "Pagamento processado com sucesso. PaymentId: {PaymentId}, Status: {Status}",
                payment.Id, payment.Status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Erro ao processar pagamento. PaymentId: {PaymentId}, OrderId: {OrderId}",
                payment.Id, payment.OrderId);

            try
            {
                payment.Fail();
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                logger.LogError(saveEx,
                    "Erro ao salvar status de falha do pagamento. PaymentId: {PaymentId}",
                    payment.Id);
            }

            throw;
        }
    }

    private async Task UpdateOrderStatusAsync(Payment payment, OrderTraceDbContext db, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FindAsync([payment.OrderId], cancellationToken: cancellationToken);

        if (order == null)
        {
            logger.LogWarning(
                "Pedido não encontrado para atualizar status. OrderId: {OrderId}, PaymentId: {PaymentId}",
                payment.OrderId, payment.Id);
            return;
        }

        try
        {
            if (payment.Status == Core.Enums.PaymentStatus.Approved)
            {
                order.MarkAsPaid();
            }
            else if (payment.Status == Core.Enums.PaymentStatus.Failed)
            {
                order.MarkAsFailed();
            }
            else if (payment.Status == Core.Enums.PaymentStatus.Cancelled)
            {
                order.Cancel();
            }

            logger.LogInformation(
                "Status do pedido atualizado. OrderId: {OrderId}, NewStatus: {Status}",
                order.Id, order.Status);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex,
                "Não foi possível atualizar status do pedido. OrderId: {OrderId}, CurrentStatus: {CurrentStatus}",
                order.Id, order.Status);
        }
    }

    private async Task SaveChangesWithRetryAsync(OrderTraceDbContext db, CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        int attempt = 0;

        while (attempt < maxRetries)
        {
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                attempt++;
                logger.LogWarning(ex,
                    "Erro de concorrência ao salvar alterações. Tentativa {Attempt} de {MaxRetries}",
                    attempt, maxRetries);

                if (attempt >= maxRetries)
                    throw;

                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), cancellationToken);
            }
        }
    }
}

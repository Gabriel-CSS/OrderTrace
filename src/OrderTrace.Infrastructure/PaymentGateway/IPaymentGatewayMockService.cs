using OrderTrace.Core.Entities;

namespace OrderTrace.Infrastructure.PaymentGateway;

public interface IPaymentGatewayMockService
{
    /// <summary>
    /// Simula o processamento de pagamento com retry automático
    /// </summary>
    /// <param name="payment">Payment que está sendo processado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado do processamento com todas as transações tentadas</returns>
    Task<PaymentGatewayResult> ProcessPaymentAsync(Payment payment, CancellationToken cancellationToken = default);
}

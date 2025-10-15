using OrderTrace.Core.Entities;

namespace OrderTrace.Infrastructure.PaymentGateway;

public interface IPaymentGatewayMockService
{
    /// <summary>
    /// Simula a tentativa de pagamento e retorna a transaction gerada
    /// </summary>
    /// <param name="payment">Payment que está sendo processado</param>
    /// <returns>Transaction simulada</returns>
    Task<List<Transaction>> ProcessPaymentWithRetriesAsync(Payment payment);
}

using OrderTrace.Core.Entities;
using OrderTrace.Core.Enums;
using Polly;

namespace OrderTrace.Infrastructure.PaymentGateway;

public class PaymentGatewayMockService : IPaymentGatewayMockService
{
    private readonly Random _random = new();
    private const int MaxRetries = 3;

    public async Task<List<Transaction>> ProcessPaymentWithRetriesAsync(Payment payment)
    {
        var transactions = new List<Transaction>();

        bool isSuccess = false;

        for (int attempt = 1; attempt <= MaxRetries && !isSuccess; attempt++)
        {
            // Simula delay variável (100ms a 1000ms)
            int delayMs = _random.Next(100, 1000);
            await Task.Delay(delayMs);

            // Simula sucesso/falha (80% chance de sucesso)
            bool attemptSuccess = _random.NextDouble() > 0.2;

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                Gateway = "MockGateway",
                ResponseCode = attemptSuccess ? "00" : "99",
                ResponseMessage = attemptSuccess ? "Approved" : "Failed",
                CreatedAt = DateTime.UtcNow
            };

            transactions.Add(transaction);

            if (attemptSuccess)
            {
                payment.Status = PaymentStatus.Approved;
                isSuccess = true;
            }
            else if (attempt == MaxRetries)
            {
                payment.Status = PaymentStatus.Failed;
            }
        }

        return transactions;
    }
}

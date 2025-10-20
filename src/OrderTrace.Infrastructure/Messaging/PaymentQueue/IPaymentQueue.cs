using OrderTrace.Core.Entities;

namespace OrderTrace.Infrastructure.Messaging.PaymentQueue;

public interface IPaymentQueue
{
    ValueTask EnqueueAsync(Payment payment, CancellationToken cancellationToken = default);
    ValueTask<Payment> DequeueAsync(CancellationToken cancellationToken = default);
}

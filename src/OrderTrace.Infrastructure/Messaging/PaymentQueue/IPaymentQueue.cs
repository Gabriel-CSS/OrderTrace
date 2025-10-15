using OrderTrace.Core.Entities;

namespace OrderTrace.Infrastructure.Messaging.PaymentQueue;

public interface IPaymentQueue
{
    ValueTask EnqueueAsync(Payment payment);
    ValueTask<Payment> DequeueAsync();
}

using OrderTrace.Core.Entities;
using System.Threading.Channels;

namespace OrderTrace.Infrastructure.Messaging.PaymentQueue;

public class PaymentQueue(int capacity = 100) : IPaymentQueue
{
    private readonly Channel<Payment> _channel = Channel.CreateBounded<Payment>(capacity);

    public async ValueTask EnqueueAsync(Payment payment)
    {
        await _channel.Writer.WriteAsync(payment);
    }

    public async ValueTask<Payment> DequeueAsync()
    {
        return await _channel.Reader.ReadAsync();
    }
}

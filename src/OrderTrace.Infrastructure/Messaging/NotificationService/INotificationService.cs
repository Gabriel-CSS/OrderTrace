using OrderTrace.Core.Entities;

namespace OrderTrace.Infrastructure.Messaging.NotificationService;

public interface INotificationService
{
    Task NotifyPaymentProcessed(Payment payment);
}

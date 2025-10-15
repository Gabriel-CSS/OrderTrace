using Microsoft.Extensions.Logging;
using OrderTrace.Core.Entities;

namespace OrderTrace.Infrastructure.Messaging.NotificationService;

public class NotificationServiceMock(ILogger<NotificationServiceMock> logger) : INotificationService
{
    public Task NotifyPaymentProcessed(Payment payment)
    {
        logger.LogInformation("Notification sent for Payment {PaymentId} with status {Status}", payment.Id, payment.Status);
        return Task.CompletedTask;
    }
}

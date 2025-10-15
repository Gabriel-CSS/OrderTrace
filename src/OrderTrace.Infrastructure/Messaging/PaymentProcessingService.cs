using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderTrace.Core.Enums;
using OrderTrace.Infrastructure.Messaging.NotificationService;
using OrderTrace.Infrastructure.Messaging.PaymentQueue;
using OrderTrace.Infrastructure.PaymentGateway;

namespace OrderTrace.Infrastructure.Messaging;

public class PaymentProcessingService(
    IPaymentQueue queue,
    OrderTraceDbContext db,
    IPaymentGatewayMockService gateway,
    INotificationService notifier,
    ILogger<PaymentProcessingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PaymentProcessingService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var payment = await queue.DequeueAsync();

            logger.LogInformation("Processing payment {PaymentId}", payment.Id);

            var transactions = await gateway.ProcessPaymentWithRetriesAsync(payment);
            foreach (var t in transactions)
            {
                logger.LogInformation("Transaction {TransactionId} created for Payment {PaymentId} - Status: {ResponseCode} {ResponseMessage}",
                    t.Id, payment.Id, t.ResponseCode, t.ResponseMessage);
            }

            logger.LogInformation("Payment {PaymentId} processed with final status {PaymentStatus}", payment.Id, payment.Status);

            var order = await db.Orders.FindAsync([payment.OrderId], cancellationToken: stoppingToken);
            if (order != null)
            {
                order.Status = PaymentStatusToOrderStatus(payment.Status);
            }

            db.Payments.Update(payment);
            await db.SaveChangesAsync(stoppingToken);

            await notifier.NotifyPaymentProcessed(payment);

            logger.LogInformation("Payment {PaymentId} processed.", payment.Id);
        }
    }

    private static OrderStatus PaymentStatusToOrderStatus(PaymentStatus paymentStatus)
    {
        return paymentStatus switch
        {
            PaymentStatus.Approved => OrderStatus.Paid,
            PaymentStatus.Failed => OrderStatus.Failed,
            PaymentStatus.Processing => OrderStatus.Pending,
            PaymentStatus.Cancelled => OrderStatus.Cancelled,
            _ => OrderStatus.Pending
        };
    }
}

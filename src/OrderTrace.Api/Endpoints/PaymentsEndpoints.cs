using Microsoft.EntityFrameworkCore;
using OrderTrace.Core.Entities;
using OrderTrace.Infrastructure;
using OrderTrace.Infrastructure.Messaging.PaymentQueue;
using OrderTrace.Observability;
using System.Diagnostics;

namespace OrderTrace.Api.Endpoints;

public class PaymentsEndpoints : IEndpointMapper
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/payments", async (CreatePaymentRequest request, OrderTraceDbContext db, IPaymentQueue queue) =>
        {
            var activity = Activity.Current;
            activity?.SetTag("payment.order_id", request.OrderId);
            activity?.SetTag("payment.amount", request.Amount);

            try
            {
                var order = await db.Orders.FindAsync(request.OrderId);
                if (order == null)
                {
                    activity?.SetTag("error.type", "OrderNotFound");
                    return Results.NotFound(new { error = $"Pedido {request.OrderId} não encontrado" });
                }

                activity?.SetTag("order.status", order.Status.ToString());

                var existingPayment = await db.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == request.OrderId);

                if (existingPayment != null)
                {
                    activity?.SetTag("error.type", "PaymentAlreadyExists");
                    return Results.Conflict(new { error = "Este pedido já possui um pagamento associado" });
                }

                var payment = Payment.Create(request.OrderId, request.Amount);

                db.Payments.Add(payment);
                await db.SaveChangesAsync();

                await queue.EnqueueAsync(payment);

                activity?.SetTag("payment.id", payment.Id);
                activity?.SetTag("payment.status", payment.Status.ToString());
                activity?.SetTag("payment.enqueued", true);

                return Results.Accepted($"/payments/{payment.Id}", payment);
            }
            catch (ArgumentException ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.RecordException(ex);
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapGet("/payments/{id:guid}", async (Guid id, OrderTraceDbContext db) =>
        {
            var activity = Activity.Current;
            activity?.SetTag("payment.id", id);

            var payment = await db.Payments
                .Include(p => p.Transactions)
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment is not null)
            {
                activity?.SetTag("payment.status", payment.Status.ToString());
                activity?.SetTag("payment.amount", payment.Amount);
                activity?.SetTag("payment.order_id", payment.OrderId);
                activity?.SetTag("payment.transactions_count", payment.Transactions.Count);
            }

            return payment is null ? Results.NotFound() : Results.Ok(payment);
        });

        app.MapGet("/payments", async (OrderTraceDbContext db) =>
        {
            var activity = Activity.Current;

            var payments = await db.Payments
                .Include(p => p.Transactions)
                .Include(p => p.Order)
                .ToListAsync();

            activity?.SetTag("payments.count", payments.Count);
            activity?.SetTag("payments.processing", payments.Count(p => p.Status == Core.Enums.PaymentStatus.Processing));
            activity?.SetTag("payments.approved", payments.Count(p => p.Status == Core.Enums.PaymentStatus.Approved));
            activity?.SetTag("payments.failed", payments.Count(p => p.Status == Core.Enums.PaymentStatus.Failed));

            return Results.Ok(payments);
        });
    }
}

public record CreatePaymentRequest(Guid OrderId, decimal Amount);

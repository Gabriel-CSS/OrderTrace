using Microsoft.EntityFrameworkCore;
using OrderTrace.Core.Entities;
using OrderTrace.Infrastructure;
using OrderTrace.Infrastructure.Messaging.PaymentQueue;

namespace OrderTrace.Api.Endpoints;

public class PaymentsEndpoints : IEndpointMapper
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/payments", async (CreatePaymentRequest request, OrderTraceDbContext db, IPaymentQueue queue) =>
        {
            try
            {
                var order = await db.Orders.FindAsync(request.OrderId);
                if (order == null)
                {
                    return Results.NotFound(new { error = $"Pedido {request.OrderId} não encontrado" });
                }

                var existingPayment = await db.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == request.OrderId);

                if (existingPayment != null)
                {
                    return Results.Conflict(new { error = "Este pedido já possui um pagamento associado" });
                }

                var payment = Payment.Create(request.OrderId, request.Amount);

                db.Payments.Add(payment);
                await db.SaveChangesAsync();

                await queue.EnqueueAsync(payment);

                return Results.Accepted($"/payments/{payment.Id}", payment);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapGet("/payments/{id:guid}", async (Guid id, OrderTraceDbContext db) =>
        {
            var payment = await db.Payments
                .Include(p => p.Transactions)
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == id);

            return payment is null ? Results.NotFound() : Results.Ok(payment);
        });

        app.MapGet("/payments", async (OrderTraceDbContext db) =>
        {
            var payments = await db.Payments
                .Include(p => p.Transactions)
                .Include(p => p.Order)
                .ToListAsync();

            return Results.Ok(payments);
        });
    }
}

public record CreatePaymentRequest(Guid OrderId, decimal Amount);

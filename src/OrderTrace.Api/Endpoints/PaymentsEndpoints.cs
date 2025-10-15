using Microsoft.EntityFrameworkCore;
using OrderTrace.Core.Entities;
using OrderTrace.Core.Enums;
using OrderTrace.Infrastructure;
using OrderTrace.Infrastructure.Messaging.PaymentQueue;

namespace OrderTrace.Api.Endpoints;

public static class PaymentsEndpoints
{
    public static void MapPaymentsEndpoints(this WebApplication app)
    {
        app.MapPost("/payments", async (Payment payment, OrderTraceDbContext db, IPaymentQueue queue) =>
        {
            payment.Id = Guid.NewGuid();
            payment.CreatedAt = DateTime.UtcNow;
            payment.Status = PaymentStatus.Processing;

            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            // Envia para fila
            await queue.EnqueueAsync(payment);

            return Results.Accepted($"/payments/{payment.Id}", payment);
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

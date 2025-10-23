using Microsoft.EntityFrameworkCore;
using OrderTrace.Infrastructure;
using System.Diagnostics;

namespace OrderTrace.Api.Endpoints;

public class TransactionsEndpoints : IEndpointMapper
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/transactions", async (Guid? orderId, OrderTraceDbContext db) =>
        {
            var activity = Activity.Current;
            activity?.SetTag("transaction.filter_by_order_id", orderId.HasValue);
            if (orderId.HasValue)
                activity?.SetTag("transaction.order_id", orderId.Value);

            var query = db.Transactions
                .Include(t => t.Payment)
                    .ThenInclude(p => p.Order)
                .AsQueryable();

            if (orderId.HasValue)
                query = query.Where(t => t.Payment.OrderId == orderId.Value);

            var transactions = await query.ToListAsync();

            activity?.SetTag("transactions.count", transactions.Count);
            activity?.SetTag("transactions.approved", transactions.Count(t => t.IsApproved()));
            activity?.SetTag("transactions.failed", transactions.Count(t => t.IsFailed()));

            return Results.Ok(transactions);
        });

        app.MapGet("/transactions/{id:guid}", async (Guid id, OrderTraceDbContext db) =>
        {
            var activity = Activity.Current;
            activity?.SetTag("transaction.id", id);

            var transaction = await db.Transactions
                .Include(t => t.Payment)
                    .ThenInclude(p => p.Order)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction is not null)
            {
                activity?.SetTag("transaction.payment_id", transaction.PaymentId);
                activity?.SetTag("transaction.gateway", transaction.Gateway);
                activity?.SetTag("transaction.response_code", transaction.ResponseCode);
                activity?.SetTag("transaction.is_approved", transaction.IsApproved());
            }

            return transaction is null ? Results.NotFound() : Results.Ok(transaction);
        });
    }
}

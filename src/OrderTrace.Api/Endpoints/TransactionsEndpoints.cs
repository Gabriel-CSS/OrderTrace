using Microsoft.EntityFrameworkCore;
using OrderTrace.Infrastructure;

namespace OrderTrace.Api.Endpoints;

public class TransactionsEndpoints : IEndpointMapper
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/transactions", async (Guid? orderId, OrderTraceDbContext db) =>
        {
            var query = db.Transactions
                .Include(t => t.Payment)
                    .ThenInclude(p => p.Order)
                .AsQueryable();

            if (orderId.HasValue)
                query = query.Where(t => t.Payment.OrderId == orderId.Value);

            var transactions = await query.ToListAsync();
            return Results.Ok(transactions);
        });

        app.MapGet("/transactions/{id:guid}", async (Guid id, OrderTraceDbContext db) =>
        {
            var transaction = await db.Transactions
                .Include(t => t.Payment)
                    .ThenInclude(p => p.Order)
                .FirstOrDefaultAsync(t => t.Id == id);

            return transaction is null ? Results.NotFound() : Results.Ok(transaction);
        });
    }
}

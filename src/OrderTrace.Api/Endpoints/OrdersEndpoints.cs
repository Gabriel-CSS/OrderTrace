using Microsoft.EntityFrameworkCore;
using OrderTrace.Core.Entities;
using OrderTrace.Infrastructure;

namespace OrderTrace.Api.Endpoints;

public static class OrdersEndpoints
{
    public static void MapOrdersEndpoints(this WebApplication app)
    {
        app.MapPost("/orders", async (Order order, OrderTraceDbContext db) =>
        {
            order.Id = Guid.NewGuid();
            order.CreatedAt = DateTime.UtcNow;
            order.Status = Core.Enums.OrderStatus.Pending;

            db.Orders.Add(order);
            await db.SaveChangesAsync();
            return Results.Created($"/orders/{order.Id}", order);
        });

        app.MapGet("/orders/{id:guid}", async (Guid id, OrderTraceDbContext db) =>
        {
            var order = await db.Orders
                .Include(o => o.Payment)
                    .ThenInclude(p => p.Transactions)
                .FirstOrDefaultAsync(o => o.Id == id);

            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        app.MapGet("/orders", async (OrderTraceDbContext db) =>
        {
            var orders = await db.Orders
                .Include(o => o.Payment)
                    .ThenInclude(p => p.Transactions)
                .ToListAsync();
            return Results.Ok(orders);
        });
    }
}

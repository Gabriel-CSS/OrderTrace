using Microsoft.EntityFrameworkCore;
using OrderTrace.Core.Entities;
using OrderTrace.Infrastructure;

namespace OrderTrace.Api.Endpoints;

public class OrdersEndpoints : IEndpointMapper
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", async (CreateOrderRequest request, OrderTraceDbContext db) =>
        {
            try
            {
                var order = Order.Create(request.ExternalOrderId, request.Amount);

                db.Orders.Add(order);
                await db.SaveChangesAsync();

                return Results.Created($"/orders/{order.Id}", order);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapGet("/orders/{id:guid}", async (Guid id, OrderTraceDbContext db) =>
        {
            var order = await db.Orders
                .Include(o => o.Payment)
                    .ThenInclude(p => p!.Transactions)
                .FirstOrDefaultAsync(o => o.Id == id);

            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        app.MapGet("/orders", async (OrderTraceDbContext db) =>
        {
            var orders = await db.Orders
                .Include(o => o.Payment)
                    .ThenInclude(p => p!.Transactions)
                .ToListAsync();
            return Results.Ok(orders);
        });
    }
}

public record CreateOrderRequest(string ExternalOrderId, decimal Amount);

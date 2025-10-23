using Microsoft.EntityFrameworkCore;
using OrderTrace.Core.Entities;
using OrderTrace.Infrastructure;
using OrderTrace.Observability;
using System.Diagnostics;

namespace OrderTrace.Api.Endpoints;

public class OrdersEndpoints : IEndpointMapper
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", async (CreateOrderRequest request, OrderTraceDbContext db) =>
        {
            var activity = Activity.Current;
            activity?.SetTag("order.external_id", request.ExternalOrderId);
            activity?.SetTag("order.amount", request.Amount);

            try
            {
                var order = Order.Create(request.ExternalOrderId, request.Amount);

                db.Orders.Add(order);
                await db.SaveChangesAsync();

                activity?.SetTag("order.id", order.Id);
                activity?.SetTag("order.status", order.Status.ToString());
                activity?.SetTag("order.created_at", order.CreatedAt);

                return Results.Created($"/orders/{order.Id}", order);
            }
            catch (ArgumentException ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.RecordException(ex);
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapGet("/orders/{id:guid}", async (Guid id, OrderTraceDbContext db) =>
        {
            var activity = Activity.Current;
            activity?.SetTag("order.id", id);

            var order = await db.Orders
                .Include(o => o.Payment)
                    .ThenInclude(p => p!.Transactions)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order is not null)
            {
                activity?.SetTag("order.status", order.Status.ToString());
                activity?.SetTag("order.amount", order.Amount);
                activity?.SetTag("order.has_payment", order.Payment != null);
            }

            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        app.MapGet("/orders", async (OrderTraceDbContext db) =>
        {
            var activity = Activity.Current;

            var orders = await db.Orders
                .Include(o => o.Payment)
                    .ThenInclude(p => p!.Transactions)
                .ToListAsync();

            activity?.SetTag("orders.count", orders.Count);
            activity?.SetTag("orders.with_payment", orders.Count(o => o.Payment != null));

            return Results.Ok(orders);
        });
    }
}

public record CreateOrderRequest(string ExternalOrderId, decimal Amount);

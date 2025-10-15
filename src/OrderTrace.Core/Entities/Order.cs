using OrderTrace.Core.Enums;

namespace OrderTrace.Core.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string ExternalOrderId { get; set; } = default!;
    public decimal Amount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navegação
    public Payment? Payment { get; set; }
}

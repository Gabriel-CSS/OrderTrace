using OrderTrace.Core.Enums;

namespace OrderTrace.Core.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Processing;
    public string TransactionId { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public Order Order { get; set; } = default!;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

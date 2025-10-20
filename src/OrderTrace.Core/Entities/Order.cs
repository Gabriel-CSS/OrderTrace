using OrderTrace.Core.Enums;

namespace OrderTrace.Core.Entities;

public class Order
{
    // Construtor privado para forçar uso do factory method
    private Order() { }

    public Guid Id { get; private set; }
    public string ExternalOrderId { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navegação
    public Payment? Payment { get; set; }

    /// <summary>
    /// Factory method para criar um novo pedido
    /// </summary>
    public static Order Create(string externalOrderId, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(externalOrderId))
            throw new ArgumentException("ExternalOrderId não pode ser vazio", nameof(externalOrderId));

        if (externalOrderId.Length > 50)
            throw new ArgumentException("ExternalOrderId não pode ter mais de 50 caracteres", nameof(externalOrderId));

        if (amount <= 0)
            throw new ArgumentException("Amount deve ser maior que zero", nameof(amount));

        if (amount > 999999.99m)
            throw new ArgumentException("Amount não pode exceder 999.999,99", nameof(amount));

        return new Order
        {
            Id = Guid.NewGuid(),
            ExternalOrderId = externalOrderId,
            Amount = amount,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Marca o pedido como pago
    /// </summary>
    public void MarkAsPaid()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Não é possível marcar pedido como pago. Status atual: {Status}");

        Status = OrderStatus.Paid;
    }

    /// <summary>
    /// Marca o pedido como falho
    /// </summary>
    public void MarkAsFailed()
    {
        if (Status == OrderStatus.Paid)
            throw new InvalidOperationException("Não é possível marcar um pedido pago como falho");

        Status = OrderStatus.Failed;
    }

    /// <summary>
    /// Cancela o pedido
    /// </summary>
    public void Cancel()
    {
        if (Status == OrderStatus.Paid)
            throw new InvalidOperationException("Não é possível cancelar um pedido já pago");

        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Pedido já está cancelado");

        Status = OrderStatus.Cancelled;
    }

    /// <summary>
    /// Verifica se o pedido pode ser processado
    /// </summary>
    public bool CanBeProcessed() => Status == OrderStatus.Pending;
}

using OrderTrace.Core.Enums;

namespace OrderTrace.Core.Entities;

public class Payment
{
    // Construtor privado para forçar uso do factory method
    private Payment() { }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string TransactionId { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public Order Order { get; set; } = default!;

    private readonly List<Transaction> _transactions = new();
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    /// <summary>
    /// Factory method para criar um novo pagamento
    /// </summary>
    public static Payment Create(Guid orderId, decimal amount)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId não pode ser vazio", nameof(orderId));

        if (amount <= 0)
            throw new ArgumentException("Amount deve ser maior que zero", nameof(amount));

        if (amount > 999999.99m)
            throw new ArgumentException("Amount não pode exceder 999.999,99", nameof(amount));

        return new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = amount,
            Status = PaymentStatus.Processing,
            TransactionId = GenerateTransactionId(),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Marca o pagamento como aprovado
    /// </summary>
    public void Approve()
    {
        if (Status == PaymentStatus.Approved)
            throw new InvalidOperationException("Pagamento já está aprovado");

        if (Status == PaymentStatus.Cancelled)
            throw new InvalidOperationException("Não é possível aprovar pagamento cancelado");

        Status = PaymentStatus.Approved;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca o pagamento como falho
    /// </summary>
    public void Fail()
    {
        if (Status == PaymentStatus.Approved)
            throw new InvalidOperationException("Não é possível falhar pagamento já aprovado");

        if (Status == PaymentStatus.Cancelled)
            throw new InvalidOperationException("Não é possível falhar pagamento cancelado");

        Status = PaymentStatus.Failed;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancela o pagamento
    /// </summary>
    public void Cancel()
    {
        if (Status == PaymentStatus.Approved)
            throw new InvalidOperationException("Não é possível cancelar pagamento aprovado");

        if (Status == PaymentStatus.Cancelled)
            throw new InvalidOperationException("Pagamento já está cancelado");

        Status = PaymentStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adiciona uma transação ao pagamento
    /// </summary>
    public void AddTransaction(Transaction transaction)
    {
        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));

        if (transaction.PaymentId != Id)
            throw new InvalidOperationException("A transação não pertence a este pagamento");

        _transactions.Add(transaction);
    }

    /// <summary>
    /// Verifica se o pagamento está em processamento
    /// </summary>
    public bool IsProcessing() => Status == PaymentStatus.Processing;

    /// <summary>
    /// Verifica se o pagamento foi concluído (aprovado ou falho)
    /// </summary>
    public bool IsCompleted() => Status == PaymentStatus.Approved || Status == PaymentStatus.Failed;

    /// <summary>
    /// Gera um ID de transação único
    /// </summary>
    private static string GenerateTransactionId()
    {
        return $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}

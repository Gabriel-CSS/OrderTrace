using OrderTrace.Core.Entities;

namespace OrderTrace.Infrastructure.PaymentGateway;

/// <summary>
/// Resultado do processamento de pagamento no gateway
/// </summary>
public record PaymentGatewayResult
{
    public bool Success { get; init; }
    public List<Transaction> Transactions { get; init; } = new();
    public int AttemptsCount => Transactions.Count;
    public Transaction? LastTransaction => Transactions.LastOrDefault();
}

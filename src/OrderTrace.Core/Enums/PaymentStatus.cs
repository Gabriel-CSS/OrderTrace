namespace OrderTrace.Core.Enums;

public enum PaymentStatus
{
    Processing,   // Pagamento iniciado
    Approved,     // Transação aprovada pelo gateway
    Failed,       // Transação falhou
    Cancelled     // Cancelado antes do processamento
}

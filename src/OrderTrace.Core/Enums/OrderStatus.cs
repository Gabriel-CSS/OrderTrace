namespace OrderTrace.Core.Enums;

public enum OrderStatus
{
    Pending,   // Pedido recebido, aguardando pagamento
    Paid,      // Pagamento aprovado
    Failed,    // Pagamento falhou
    Cancelled  // Pedido cancelado ou expirado
}

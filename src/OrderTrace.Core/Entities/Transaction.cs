namespace OrderTrace.Core.Entities;

public class Transaction
{
    // Construtor privado para forçar uso do factory method
    private Transaction() { }

    public Guid Id { get; private set; }
    public Guid PaymentId { get; private set; }
    public string Gateway { get; private set; } = default!;
    public string ResponseCode { get; private set; } = default!;
    public string ResponseMessage { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    public Payment Payment { get; set; } = default!;

    /// <summary>
    /// Factory method para criar uma nova transação
    /// </summary>
    public static Transaction Create(Guid paymentId, string gateway, string responseCode, string responseMessage)
    {
        if (paymentId == Guid.Empty)
            throw new ArgumentException("PaymentId não pode ser vazio", nameof(paymentId));

        if (string.IsNullOrWhiteSpace(gateway))
            throw new ArgumentException("Gateway não pode ser vazio", nameof(gateway));

        if (gateway.Length > 50)
            throw new ArgumentException("Gateway não pode ter mais de 50 caracteres", nameof(gateway));

        if (string.IsNullOrWhiteSpace(responseCode))
            throw new ArgumentException("ResponseCode não pode ser vazio", nameof(responseCode));

        if (responseCode.Length > 10)
            throw new ArgumentException("ResponseCode não pode ter mais de 10 caracteres", nameof(responseCode));

        if (string.IsNullOrWhiteSpace(responseMessage))
            throw new ArgumentException("ResponseMessage não pode ser vazio", nameof(responseMessage));

        if (responseMessage.Length > 200)
            throw new ArgumentException("ResponseMessage não pode ter mais de 200 caracteres", nameof(responseMessage));

        return new Transaction
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            Gateway = gateway,
            ResponseCode = responseCode,
            ResponseMessage = responseMessage,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Verifica se a transação foi aprovada
    /// </summary>
    public bool IsApproved() => ResponseCode == "00";

    /// <summary>
    /// Verifica se a transação falhou
    /// </summary>
    public bool IsFailed() => ResponseCode != "00";
}

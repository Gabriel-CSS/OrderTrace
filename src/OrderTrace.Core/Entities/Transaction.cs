namespace OrderTrace.Core.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; } // FK para Payment
    public string Gateway { get; set; } = "MockGateway";
    public string ResponseCode { get; set; } = "00";
    public string ResponseMessage { get; set; } = "Approved";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Payment Payment { get; set; } = default!;
}

using SmartPOS.Shared.Enums;

namespace SmartPOS.Data.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; } public Order Order { get; set; } = null!; public PaymentMethod PaymentMethod { get; set; } public decimal? AmountReceived { get; set; } public decimal? ChangeAmount { get; set; } public string? TransactionId { get; set; } public PaymentStatus PaymentStatus { get; set; } public string? VnpayResponse { get; set; } public DateTime CreatedAt { get; set; }
}


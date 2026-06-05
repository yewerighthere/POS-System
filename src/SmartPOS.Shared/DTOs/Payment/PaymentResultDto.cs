namespace SmartPOS.Shared.DTOs.Payment;

public class PaymentResultDto { public Guid OrderId { get; set; } public decimal AmountReceived { get; set; } public decimal ChangeAmount { get; set; } public string PaymentStatus { get; set; } = string.Empty; public string? PaymentUrl { get; set; } }


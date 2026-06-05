namespace SmartPOS.Shared.DTOs.Invoice;

public class InvoiceDto { public Guid Id { get; set; } public Guid OrderId { get; set; } public string InvoiceNumber { get; set; } = string.Empty; public decimal TotalAmount { get; set; } public DateTime IssuedAt { get; set; } }


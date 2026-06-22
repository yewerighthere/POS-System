using SmartPOS.Shared.Enums;

namespace SmartPOS.Data.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; } public Order Order { get; set; } = null!; public string InvoiceNumber { get; set; } = string.Empty; public decimal TotalAmount { get; set; } public DateTime IssuedAt { get; set; }
}


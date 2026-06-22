using SmartPOS.Shared.Enums;

namespace SmartPOS.Data.Entities;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; } public Order Order { get; set; } = null!; public Guid ProductId { get; set; } public Product Product { get; set; } = null!; public string ProductName { get; set; } = string.Empty; public string Sku { get; set; } = string.Empty; public decimal UnitPrice { get; set; } public int Quantity { get; set; } public decimal DiscountAmount { get; set; } public decimal Subtotal { get; set; }
}


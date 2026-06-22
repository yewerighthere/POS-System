namespace SmartPOS.Shared.DTOs.Order;

public class OrderItemDto { public Guid Id { get; set; } public Guid ProductId { get; set; } public string ProductName { get; set; } = string.Empty; public int Quantity { get; set; } public decimal Subtotal { get; set; } }


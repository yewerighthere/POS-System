namespace SmartPOS.Shared.DTOs.Order;

public class OrderDto { public Guid Id { get; set; } public decimal TotalAmount { get; set; } public string Status { get; set; } = string.Empty; public List<OrderItemDto> Items { get; set; } = new(); }


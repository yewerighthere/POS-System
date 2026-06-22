using SmartPOS.Shared.Enums;

namespace SmartPOS.Data.Entities;

public class ReturnItem
{
    public Guid Id { get; set; }
    public Guid ReturnId { get; set; } public Return Return { get; set; } = null!; public Guid OrderItemId { get; set; } public OrderItem OrderItem { get; set; } = null!; public int Quantity { get; set; }
}


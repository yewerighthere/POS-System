using SmartPOS.Shared.Enums;

namespace SmartPOS.Data.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; } public Shift Shift { get; set; } = null!; public Guid UserId { get; set; } public User User { get; set; } = null!; public Guid? CustomerId { get; set; } public Customer? Customer { get; set; } public OrderStatus Status { get; set; } public decimal Subtotal { get; set; } public decimal DiscountAmount { get; set; } public decimal TaxAmount { get; set; } public decimal TotalAmount { get; set; } public PaymentMethod? PaymentMethod { get; set; } public PaymentStatus PaymentStatus { get; set; } public bool IsLocked { get; set; } public DateTime CreatedAt { get; set; } public DateTime? UpdatedAt { get; set; } public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>(); public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}


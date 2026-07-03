namespace SmartPOS.Shared.DTOs.Customer;

public class CustomerOrderDetailDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    
    public int PointsEarned { get; set; }
    public int PointsUsed { get; set; }
    public decimal PointsDiscountAmount { get; set; }
    
    public string? PaymentMethod { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal AmountReceived { get; set; }
    public decimal ChangeAmount { get; set; }
    
    public List<CustomerOrderItemDto> Items { get; set; } = new();
}

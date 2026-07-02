namespace SmartPOS.Shared.DTOs.Customer;

public class CustomerOrderDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
}

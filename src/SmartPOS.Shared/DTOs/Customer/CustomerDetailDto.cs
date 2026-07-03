namespace SmartPOS.Shared.DTOs.Customer;

public class CustomerDetailDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int LoyaltyPoints { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public List<CustomerOrderDto> Orders { get; set; } = new();
}

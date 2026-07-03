namespace SmartPOS.Shared.DTOs.Customer;

public class CustomerListDto
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int OrderCount { get; set; }
    public int LoyaltyPoints { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

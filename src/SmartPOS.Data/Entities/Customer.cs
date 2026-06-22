using SmartPOS.Shared.Enums;

namespace SmartPOS.Data.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string? FullName { get; set; } public string? Phone { get; set; } public string? Email { get; set; } public string? MemberCode { get; set; } public int LoyaltyPoints { get; set; } public DateTime CreatedAt { get; set; } public DateTime? UpdatedAt { get; set; }
}


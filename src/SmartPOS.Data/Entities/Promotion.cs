using SmartPOS.Shared.Enums;

namespace SmartPOS.Data.Entities;

public class Promotion
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string? Description { get; set; } public string Type { get; set; } = string.Empty; public decimal DiscountValue { get; set; } public decimal? MinOrderAmount { get; set; } public Guid? ProductId { get; set; } public Product? Product { get; set; } public DateOnly StartDate { get; set; } public DateOnly EndDate { get; set; } public decimal? RequiresApprovalThreshold { get; set; } public bool IsActive { get; set; }
}


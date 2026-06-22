namespace SmartPOS.Shared.DTOs.Promotion;

public class PromotionDto { 
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty; // e.g., "Percentage", "FixedAmount"
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}


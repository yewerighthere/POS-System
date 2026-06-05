namespace SmartPOS.Shared.DTOs.Promotion;

public class PromotionDto { public Guid Id { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public decimal DiscountValue { get; set; } }


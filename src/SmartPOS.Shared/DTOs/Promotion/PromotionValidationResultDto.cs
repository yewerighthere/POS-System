namespace SmartPOS.Shared.DTOs.Promotion;

public class PromotionValidationResultDto { public bool IsValid { get; set; } public string Message { get; set; } = string.Empty; public decimal DiscountAmount { get; set; } }


namespace SmartPOS.Shared.DTOs.Return;

public class ReturnDto { public Guid Id { get; set; } public Guid OrderId { get; set; } public string Status { get; set; } = string.Empty; public decimal? RefundAmount { get; set; } }


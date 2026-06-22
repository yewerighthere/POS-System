namespace SmartPOS.Shared.DTOs.Shift;

public class ShiftDto { public Guid Id { get; set; } public Guid UserId { get; set; } public string Status { get; set; } = string.Empty; public decimal OpeningCash { get; set; } public DateTime OpenedAt { get; set; } }


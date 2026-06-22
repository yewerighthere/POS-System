using SmartPOS.Shared.Enums;

namespace SmartPOS.Data.Entities;

public class Shift
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; } public User User { get; set; } = null!; public ShiftStatus Status { get; set; } public decimal OpeningCash { get; set; } public decimal? ClosingCash { get; set; } public decimal? ExpectedCash { get; set; } public decimal? CashDifference { get; set; } public DateTime OpenedAt { get; set; } public DateTime? ClosedAt { get; set; }
}


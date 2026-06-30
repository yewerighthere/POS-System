namespace SmartPOS.Shared.DTOs.Report;

public class RecentShiftDto
{
    public Guid Id { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StaffName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public string DisplayId => $"#SH-{Id.ToString("N")[..4].ToUpper()}";
}

namespace SmartPOS.Shared.DTOs.Report;

public class ShiftReportDto
{
    public Guid ShiftId { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal OpeningCash { get; set; }
    public decimal? ClosingCash { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? CashDifference { get; set; }
    public decimal TotalSales { get; set; }
    public decimal CashRevenue { get; set; }
    public decimal VNPayRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageTicket => TotalOrders > 0 ? Math.Round(TotalSales / TotalOrders, 0) : 0;
    public bool IsCashDifferenceNegative => CashDifference.HasValue && CashDifference.Value < 0;
    public IReadOnlyList<OrderLogDto> OrderLog { get; set; } = new List<OrderLogDto>();
    public IReadOnlyList<TopProductDto> TopProducts { get; set; } = new List<TopProductDto>();
}

namespace SmartPOS.Shared.DTOs.Report;

public class SalesReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public decimal TotalRevenue  { get; set; }
    public int     TotalOrders   { get; set; }
    public int     TotalShifts   { get; set; }
    public decimal CashRevenue   { get; set; }
    public decimal VNPayRevenue  { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTax      { get; set; }

    public decimal AverageTicket => TotalOrders > 0 ? Math.Round(TotalRevenue / TotalOrders, 0) : 0;

    public IReadOnlyList<OrderLogDto>   OrderLog    { get; set; } = [];
    public IReadOnlyList<TopProductDto> TopProducts { get; set; } = [];
}

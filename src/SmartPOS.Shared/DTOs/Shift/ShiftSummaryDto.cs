namespace SmartPOS.Shared.DTOs.Shift;

public class ShiftSummaryDto
{
    public Guid    ShiftId       { get; set; }
    public decimal TotalSales    { get; set; }
    public decimal ExpectedCash  { get; set; }
    public decimal OpeningCash   { get; set; }
    public decimal ClosingCash   { get; set; }
    public decimal CashDifference { get; set; }
}


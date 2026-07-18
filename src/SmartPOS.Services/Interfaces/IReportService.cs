using SmartPOS.Shared.DTOs.Report;

namespace SmartPOS.Services.Interfaces;

public interface IReportService
{
    Task<ShiftReportDto> GetShiftReportAsync(Guid shiftId);
    Task<SalesReportDto> GetSalesReportAsync(SalesReportFilterDto filter);
    Task<IReadOnlyList<RecentShiftDto>> GetRecentShiftSummariesAsync(int count = 10);
}

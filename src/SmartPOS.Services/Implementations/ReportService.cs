using Microsoft.Extensions.Logging;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Report;
using SmartPOS.Shared.Exceptions;

namespace SmartPOS.Services.Implementations;

public class ReportService : IReportService
{
    private readonly ILogger<ReportService> _logger;
    private readonly IShiftRepository _shiftRepository;
    private readonly IOrderRepository _orderRepository;

    public ReportService(
        ILogger<ReportService> logger,
        IShiftRepository shiftRepository,
        IOrderRepository orderRepository)
    {
        _logger = logger;
        _shiftRepository = shiftRepository;
        _orderRepository = orderRepository;
    }

    public async Task<ShiftReportDto> GetShiftReportAsync(Guid shiftId)
    {
        var shift = await _shiftRepository.GetByIdAsync(shiftId).ConfigureAwait(false)
            ?? throw new BusinessException("Không tìm thấy ca làm việc");

        var totalSales   = await _shiftRepository.GetTotalSalesAsync(shiftId).ConfigureAwait(false);
        var cashRevenue  = await _shiftRepository.GetCashRevenueAsync(shiftId).ConfigureAwait(false);
        var vnpayRevenue = await _shiftRepository.GetVNPayRevenueAsync(shiftId).ConfigureAwait(false);
        var orderCount   = await _orderRepository.GetOrderCountByShiftAsync(shiftId).ConfigureAwait(false);
        var orders       = await _orderRepository.GetOrdersByShiftAsync(shiftId).ConfigureAwait(false);
        var topProducts  = await _orderRepository.GetTopProductsByShiftAsync(shiftId, 5).ConfigureAwait(false);

        var orderLog = orders.Select(o => new OrderLogDto
        {
            Id            = o.Id,
            CreatedAt     = o.CreatedAt.ToLocalTime(),
            StaffName     = o.User?.FullName ?? "—",
            ItemsSummary  = string.Join(", ", o.Items.Select(i => $"{i.Quantity}x {i.ProductName}")),
            PaymentMethod = o.PaymentMethod?.ToString() ?? "—",
            TotalAmount   = o.TotalAmount,
            PaymentStatus = o.PaymentStatus.ToString()
        }).ToList();

        _logger.LogInformation("Đã tạo báo cáo ca {ShiftId}: {Orders} đơn, doanh thu {Total}", shiftId, orderCount, totalSales);

        return new ShiftReportDto
        {
            ShiftId        = shift.Id,
            OpenedAt       = shift.OpenedAt.ToLocalTime(),
            ClosedAt       = shift.ClosedAt?.ToLocalTime(),
            Status         = shift.Status.ToString(),
            OpeningCash    = shift.OpeningCash,
            ClosingCash    = shift.ClosingCash,
            ExpectedCash   = shift.ExpectedCash,
            CashDifference = shift.CashDifference,
            TotalSales     = totalSales,
            CashRevenue    = cashRevenue,
            VNPayRevenue   = vnpayRevenue,
            TotalOrders    = orderCount,
            OrderLog       = orderLog,
            TopProducts    = topProducts
        };
    }

    public async Task<IReadOnlyList<RecentShiftDto>> GetRecentShiftSummariesAsync(int count = 10)
    {
        var shifts = await _shiftRepository.GetRecentShiftsAsync(count).ConfigureAwait(false);
        var summaries = new List<RecentShiftDto>();
        foreach (var s in shifts)
        {
            var revenue = await _shiftRepository.GetTotalSalesAsync(s.Id).ConfigureAwait(false);
            var orders  = await _orderRepository.GetOrderCountByShiftAsync(s.Id).ConfigureAwait(false);
            summaries.Add(new RecentShiftDto
            {
                Id           = s.Id,
                OpenedAt     = s.OpenedAt.ToLocalTime(),
                ClosedAt     = s.ClosedAt?.ToLocalTime(),
                Status       = s.Status.ToString(),
                StaffName    = s.User?.FullName ?? "—",
                TotalOrders  = orders,
                TotalRevenue = revenue
            });
        }
        return summaries;
    }

    public Task<SalesReportDto> GetSalesReportAsync(DateTime fromDate, DateTime toDate)
        => throw new NotImplementedException();
}

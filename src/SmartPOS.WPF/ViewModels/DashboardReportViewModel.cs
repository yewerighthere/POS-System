using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Report;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;
using System.Collections.ObjectModel;

namespace SmartPOS.WPF.ViewModels;

public partial class DashboardReportViewModel : ObservableObject
{
    private readonly IReportService _reportService;
    private readonly ILogger<DashboardReportViewModel> _logger;
    private readonly NavigationService _navigationService;
    private readonly CurrentSessionContext _sessionContext;

    public string CurrentUserName => _sessionContext.CurrentUser?.Username ?? "Unknown";
    public string CurrentUserRole => _sessionContext.CurrentUser?.Role ?? "Unknown";

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private ObservableCollection<RecentShiftDto> _recentShifts = new();
    [ObservableProperty] private RecentShiftDto? _selectedShift;
    [ObservableProperty] private ShiftReportDto? _shiftReport;
    [ObservableProperty] private ObservableCollection<OrderLogDto> _orderLog = new();
    [ObservableProperty] private ObservableCollection<TopProductDto> _topProducts = new();

    public int MaxTopSold => TopProducts.Count > 0 ? TopProducts.Max(p => p.TotalSold) : 1;

    public double CashPercent => ShiftReport is { TotalSales: > 0 }
        ? (double)(ShiftReport.CashRevenue / ShiftReport.TotalSales * 100) : 0;

    public double VNPayPercent => ShiftReport is { TotalSales: > 0 }
        ? (double)(ShiftReport.VNPayRevenue / ShiftReport.TotalSales * 100) : 0;

    public decimal TotalRevenueAllShifts => RecentShifts.Sum(s => s.TotalRevenue);
    public int     TotalOrdersAllShifts  => RecentShifts.Sum(s => s.TotalOrders);
    public decimal TodayRevenue => RecentShifts
        .Where(s => s.OpenedAt.Date == DateTime.Today).Sum(s => s.TotalRevenue);
    public int     TodayOrders  => RecentShifts
        .Where(s => s.OpenedAt.Date == DateTime.Today).Sum(s => s.TotalOrders);
    public int     TotalShiftsCount  => RecentShifts.Count;
    public int     ActiveShiftsCount => RecentShifts.Count(s => s.Status == "Open");

    public DashboardReportViewModel(
        IReportService reportService,
        ILogger<DashboardReportViewModel> logger,
        NavigationService navigationService,
        CurrentSessionContext sessionContext)
    {
        _reportService = reportService;
        _logger = logger;
        _navigationService = navigationService;
        _sessionContext = sessionContext;
    }

    [RelayCommand]
    private async Task LoadShiftsAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var shifts = await _reportService.GetRecentShiftSummariesAsync(20);
            RecentShifts = new ObservableCollection<RecentShiftDto>(shifts);
            OnPropertyChanged(nameof(TotalRevenueAllShifts));
            OnPropertyChanged(nameof(TotalOrdersAllShifts));
            OnPropertyChanged(nameof(TodayRevenue));
            OnPropertyChanged(nameof(TodayOrders));
            OnPropertyChanged(nameof(TotalShiftsCount));
            OnPropertyChanged(nameof(ActiveShiftsCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải danh sách ca");
            ErrorMessage = "Không thể tải danh sách ca làm việc";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectShiftAsync(RecentShiftDto shift)
    {
        SelectedShift = shift;
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            ShiftReport = await _reportService.GetShiftReportAsync(shift.Id);
            OrderLog    = new ObservableCollection<OrderLogDto>(ShiftReport.OrderLog);
            TopProducts = new ObservableCollection<TopProductDto>(ShiftReport.TopProducts);
            OnPropertyChanged(nameof(MaxTopSold));
            OnPropertyChanged(nameof(CashPercent));
            OnPropertyChanged(nameof(VNPayPercent));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải báo cáo ca {ShiftId}", shift.Id);
            ErrorMessage = "Không thể tải báo cáo ca";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void NavigateToOverview() => _navigationService.NavigateTo<DashboardViewModel>();

    [RelayCommand]
    private void NavigateToCatalogPromo() => _navigationService.NavigateTo<DashboardCatalogPromoViewModel>();

    [RelayCommand]
    private void NavigateToInventory() => _navigationService.NavigateTo<DashboardInventoryViewModel>();

    [RelayCommand]
    private void NavigateToUsers() => _navigationService.NavigateTo<DashboardUserStaffViewModel>();

    [RelayCommand]
    private void Logout()
    {
        _sessionContext.CurrentUser = null;
        _sessionContext.CurrentToken = string.Empty;
        _sessionContext.CurrentShift = null;
        _sessionContext.CurrentCart = null;
        _navigationService.NavigateTo<LoginViewModel>();
    }
}

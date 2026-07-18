using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Auth;
using SmartPOS.Shared.DTOs.Report;
using SmartPOS.Shared.Enums;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;
using System.Collections.ObjectModel;

namespace SmartPOS.WPF.ViewModels;

public partial class DashboardReportViewModel : ObservableObject
{
    private readonly IReportService _reportService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<DashboardReportViewModel> _logger;
    private readonly NavigationService _navigationService;
    private readonly CurrentSessionContext _sessionContext;

    public string CurrentUserName => _sessionContext.CurrentUser?.Username ?? "Unknown";
    public string CurrentUserRole => _sessionContext.CurrentUser?.Role ?? "Unknown";

    private bool IsManagerOrAdmin =>
        CurrentUserRole is "Manager" or "Admin";

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private ObservableCollection<RecentShiftDto> _recentShifts = new();
    [ObservableProperty] private RecentShiftDto? _selectedShift;
    [ObservableProperty] private ShiftReportDto? _shiftReport;
    [ObservableProperty] private ObservableCollection<OrderLogDto> _orderLog = new();
    [ObservableProperty] private ObservableCollection<TopProductDto> _topProducts = new();

    [ObservableProperty] private DateTime _salesFromDate = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime _salesToDate   = DateTime.Today;
    [ObservableProperty] private SalesReportDto? _salesReport;
    [ObservableProperty] private bool   _isSalesReportLoading;
    [ObservableProperty] private string _salesReportError = string.Empty;
    [ObservableProperty] private ObservableCollection<OrderLogDto>   _salesOrderLog    = new();
    [ObservableProperty] private ObservableCollection<TopProductDto> _salesTopProducts = new();

    [ObservableProperty] private ObservableCollection<UserDto> _staffList = new();
    [ObservableProperty] private UserDto?         _selectedStaff;
    [ObservableProperty] private string           _selectedPaymentMethodFilter = "Tất cả";

    public IReadOnlyList<string> PaymentMethodOptions { get; } = ["Tất cả", "Tiền mặt", "VNPay"];

    public int MaxTopSold => TopProducts.Count > 0 ? TopProducts.Max(p => p.TotalSold) : 1;

    public double CashPercent => ShiftReport is { TotalSales: > 0 }
        ? (double)(ShiftReport.CashRevenue / ShiftReport.TotalSales * 100) : 0;

    public double VNPayPercent => ShiftReport is { TotalSales: > 0 }
        ? (double)(ShiftReport.VNPayRevenue / ShiftReport.TotalSales * 100) : 0;

    public double SalesCashPercent => SalesReport is { TotalRevenue: > 0 }
        ? (double)(SalesReport.CashRevenue / SalesReport.TotalRevenue * 100) : 0;

    public double SalesVNPayPercent => SalesReport is { TotalRevenue: > 0 }
        ? (double)(SalesReport.VNPayRevenue / SalesReport.TotalRevenue * 100) : 0;

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
        IUserRepository userRepository,
        ILogger<DashboardReportViewModel> logger,
        NavigationService navigationService,
        CurrentSessionContext sessionContext)
    {
        _reportService = reportService;
        _userRepository = userRepository;
        _logger = logger;
        _navigationService = navigationService;
        _sessionContext = sessionContext;
    }

    [RelayCommand]
    private async Task LoadShiftsAsync()
    {
        if (!IsManagerOrAdmin)
        {
            ErrorMessage = "Bạn không có quyền xem báo cáo";
            return;
        }
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

            var users    = await _userRepository.GetAllAsync();
            var allStaff = new UserDto { Id = Guid.Empty, FullName = "Tất cả nhân viên" };
            StaffList = new ObservableCollection<UserDto>(
                new[] { allStaff }.Concat(users.Select(u => new UserDto
                {
                    Id       = u.Id,
                    FullName = u.FullName ?? u.Username,
                    Role     = u.Role.ToString()
                })));
            SelectedStaff = allStaff;
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
    private async Task GenerateSalesReportAsync()
    {
        if (!IsManagerOrAdmin)
        {
            SalesReportError = "Bạn không có quyền xem báo cáo";
            return;
        }
        IsSalesReportLoading = true;
        SalesReportError = string.Empty;
        try
        {
            var paymentMethod = SelectedPaymentMethodFilter switch
            {
                "Tiền mặt" => (PaymentMethod?)PaymentMethod.Cash,
                "VNPay"    => (PaymentMethod?)PaymentMethod.VNPay,
                _          => null
            };
            var staffId = SelectedStaff?.Id == Guid.Empty ? (Guid?)null : SelectedStaff?.Id;
            var filter = new SalesReportFilterDto(
                SalesFromDate, SalesToDate, staffId,
                null, paymentMethod,
                _sessionContext.CurrentUser?.UserId);
            SalesReport      = await _reportService.GetSalesReportAsync(filter);
            SalesOrderLog    = new ObservableCollection<OrderLogDto>(SalesReport.OrderLog);
            SalesTopProducts = new ObservableCollection<TopProductDto>(SalesReport.TopProducts);
            OnPropertyChanged(nameof(SalesCashPercent));
            OnPropertyChanged(nameof(SalesVNPayPercent));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo báo cáo doanh thu");
            SalesReportError = ex.Message;
        }
        finally
        {
            IsSalesReportLoading = false;
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

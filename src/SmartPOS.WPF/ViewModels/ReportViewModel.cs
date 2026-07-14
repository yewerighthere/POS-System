using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Report;
using SmartPOS.Shared.Exceptions;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;
using System.Collections.ObjectModel;

namespace SmartPOS.WPF.ViewModels;

public partial class ReportViewModel : ObservableObject
{
    private readonly IReportService _reportService;
    private readonly CurrentSessionContext _session;
    private readonly ILogger<ReportViewModel> _logger;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private ShiftReportDto? _shiftReport;
    [ObservableProperty] private ObservableCollection<RecentShiftDto> _recentShifts = new();
    [ObservableProperty] private ObservableCollection<OrderLogDto> _orderLog = new();
    [ObservableProperty] private ObservableCollection<TopProductDto> _topProducts = new();

    public string TodayDisplay => DateTime.Now.ToString("dd MMM yyyy");
    public int MaxTopSold => TopProducts.Count > 0 ? TopProducts.Max(p => p.TotalSold) : 1;

    public double CashPercent => ShiftReport is { TotalSales: > 0 }
        ? (double)(ShiftReport.CashRevenue / ShiftReport.TotalSales * 100) : 0;
    public double VNPayPercent => ShiftReport is { TotalSales: > 0 }
        ? (double)(ShiftReport.VNPayRevenue / ShiftReport.TotalSales * 100) : 0;

    public ReportViewModel(IReportService reportService, CurrentSessionContext session, ILogger<ReportViewModel> logger, NavigationService navigationService)
    {
        _reportService = reportService;
        _session = session;
        _logger = logger;
        _navigationService = navigationService;
    }

    [RelayCommand]
    private void NavigateToSales() => _navigationService.NavigateTo<SalesViewModel>();

    [RelayCommand]
    private void NavigateToCatalog() => _navigationService.NavigateTo<CatalogViewModel>();

    [RelayCommand]
    private void NavigateToSync() => _navigationService.NavigateTo<SyncViewModel>();

    [RelayCommand]
    private void NavigateToCustomer() => _navigationService.NavigateTo<CustomerViewModel>();

    [RelayCommand]
    public async Task GenerateReportAsync()
    {
        if (_session.CurrentShift is null)
        {
            ErrorMessage = "Chưa có ca nào đang mở";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            ShiftReport = await _reportService.GetShiftReportAsync(_session.CurrentShift.Id);
            var recent = await _reportService.GetRecentShiftSummariesAsync(10);
            RecentShifts = new ObservableCollection<RecentShiftDto>(recent);
            OrderLog     = new ObservableCollection<OrderLogDto>(ShiftReport.OrderLog);
            TopProducts  = new ObservableCollection<TopProductDto>(ShiftReport.TopProducts);
            OnPropertyChanged(nameof(MaxTopSold));
            OnPropertyChanged(nameof(CashPercent));
            OnPropertyChanged(nameof(VNPayPercent));
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải báo cáo ca");
            ErrorMessage = "Đã có lỗi xảy ra, vui lòng kiểm tra nhật ký";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

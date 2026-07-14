using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Dashboard;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;

namespace SmartPOS.WPF.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardService _dashboardService;
    private readonly NavigationService _navigationService;
    private readonly CurrentSessionContext _sessionContext;
    private readonly ILogger<DashboardViewModel> _logger;

    [ObservableProperty]
    private DashboardOverviewDto _overview = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;
    
    public string CurrentUserName => _sessionContext.CurrentUser?.Username ?? "Unknown";
    public string CurrentUserRole => _sessionContext.CurrentUser?.Role ?? "Unknown";

    public DashboardViewModel(
        IDashboardService dashboardService,
        NavigationService navigationService,
        CurrentSessionContext sessionContext,
        ILogger<DashboardViewModel> logger)
    {
        _dashboardService = dashboardService;
        _navigationService = navigationService;
        _sessionContext = sessionContext;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            Overview = await _dashboardService.GetOverviewAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard data");
            ErrorMessage = "Không thể tải dữ liệu Dashboard.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void NavigateToCatalogPromo() => _navigationService.NavigateTo<DashboardCatalogPromoViewModel>();

    [RelayCommand]
    private void NavigateToInventory() => _navigationService.NavigateTo<DashboardInventoryViewModel>();

    [RelayCommand]
    private void NavigateToReports() => _navigationService.NavigateTo<DashboardReportViewModel>();

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

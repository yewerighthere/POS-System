using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;

namespace SmartPOS.WPF.ViewModels;

public partial class DashboardUserStaffViewModel : ObservableObject
{
    private readonly NavigationService _navigationService;
    private readonly CurrentSessionContext _sessionContext;

    public string CurrentUserName => _sessionContext.CurrentUser?.Username ?? "Unknown";
    public string CurrentUserRole => _sessionContext.CurrentUser?.Role ?? "Unknown";

    public DashboardUserStaffViewModel(
        NavigationService navigationService,
        CurrentSessionContext sessionContext)
    {
        _navigationService = navigationService;
        _sessionContext = sessionContext;
    }

    [RelayCommand]
    private void NavigateToOverview() => _navigationService.NavigateTo<DashboardViewModel>();

    [RelayCommand]
    private void NavigateToCatalogPromo() => _navigationService.NavigateTo<DashboardCatalogPromoViewModel>();

    [RelayCommand]
    private void NavigateToInventory() => _navigationService.NavigateTo<DashboardInventoryViewModel>();

    [RelayCommand]
    private void NavigateToReports() => _navigationService.NavigateTo<DashboardReportViewModel>();

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

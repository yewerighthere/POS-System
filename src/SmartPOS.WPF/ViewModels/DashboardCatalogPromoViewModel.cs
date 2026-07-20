using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;

namespace SmartPOS.WPF.ViewModels;

public partial class DashboardCatalogPromoViewModel : ObservableObject
{
    private readonly NavigationService _navigationService;
    private readonly CurrentSessionContext _sessionContext;

    public string CurrentUserName => _sessionContext.CurrentUser?.Username ?? "Unknown";
    public string CurrentUserRole => _sessionContext.CurrentUser?.Role ?? "Unknown";

    [ObservableProperty]
    private CatalogViewModel _catalogVM;

    [ObservableProperty]
    private PromotionViewModel _promotionVM;

    public DashboardCatalogPromoViewModel(
        NavigationService navigationService,
        CurrentSessionContext sessionContext,
        CatalogViewModel catalogViewModel,
        PromotionViewModel promotionViewModel)
    {
        _navigationService = navigationService;
        _sessionContext = sessionContext;
        _catalogVM = catalogViewModel;
        _promotionVM = promotionViewModel;
        
        _catalogVM.ShowNavigation = false;
        _ = LoadTabsSequentiallyAsync();
    }

    private async Task LoadTabsSequentiallyAsync()
    {
        await _catalogVM.LoadAsync();
        await _promotionVM.LoadAsync();
    }

    [RelayCommand]
    private void NavigateToOverview() => _navigationService.NavigateTo<DashboardViewModel>();

    [RelayCommand]
    private void NavigateToInventory() => _navigationService.NavigateTo<DashboardInventoryViewModel>();

    [RelayCommand]
    private void NavigateToUsers() => _navigationService.NavigateTo<DashboardUserStaffViewModel>();

    [RelayCommand]
    private void NavigateToAuditLogs() => _navigationService.NavigateTo<AuditLogViewModel>();

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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Data.Entities;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;

namespace SmartPOS.WPF.ViewModels;

public partial class AuditLogViewModel : ObservableObject
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly NavigationService _navigationService;
    private readonly CurrentSessionContext _sessionContext;

    public string CurrentUserName => _sessionContext.CurrentUser?.Username ?? "Unknown";
    public string CurrentUserRole => _sessionContext.CurrentUser?.Role ?? "Unknown";

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    private List<AuditLog> _allLogs = new();

    private ObservableCollection<AuditLog> _logs = new();
    public ObservableCollection<AuditLog> Logs
    {
        get => _logs;
        set => SetProperty(ref _logs, value);
    }

    private AuditLog? _selectedLog;
    public AuditLog? SelectedLog
    {
        get => _selectedLog;
        set => SetProperty(ref _selectedLog, value);
    }

    private string _filterQuery = string.Empty;
    public string FilterQuery
    {
        get => _filterQuery;
        set
        {
            if (SetProperty(ref _filterQuery, value))
            {
                ApplyFilter();
            }
        }
    }

    public AuditLogViewModel(
        IAuditService auditService, 
        IAuditLogRepository auditLogRepository,
        NavigationService navigationService,
        CurrentSessionContext sessionContext)
    {
        _auditLogRepository = auditLogRepository;
        _navigationService = navigationService;
        _sessionContext = sessionContext;

        _ = LoadAsync();
    }

    [RelayCommand]
    private void SwitchToPos() => _navigationService.NavigateTo<SalesViewModel>();

    [RelayCommand]
    private void NavigateToOverview() => _navigationService.NavigateTo<DashboardViewModel>();

    [RelayCommand]
    private void NavigateToCatalogPromo() => _navigationService.NavigateTo<DashboardCatalogPromoViewModel>();

    [RelayCommand]
    private void NavigateToInventory() => _navigationService.NavigateTo<SyncViewModel>();

    [RelayCommand]
    private void NavigateToUsers() => _navigationService.NavigateTo<DashboardUserStaffViewModel>();

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

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            _allLogs = (await _auditLogRepository.GetRecentAsync(200)).ToList();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Lỗi khi tải nhật ký hoạt động: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshAsync() => await LoadAsync();

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(FilterQuery))
        {
            Logs = new ObservableCollection<AuditLog>(_allLogs);
            return;
        }

        var q = FilterQuery.Trim().ToLower();
        var filtered = _allLogs.Where(l =>
            (l.Action ?? string.Empty).ToLower().Contains(q) ||
            (l.Entity ?? string.Empty).ToLower().Contains(q) ||
            (l.OldValue ?? string.Empty).ToLower().Contains(q) ||
            (l.NewValue ?? string.Empty).ToLower().Contains(q) ||
            (l.User?.Username ?? string.Empty).ToLower().Contains(q) ||
            l.UserId.ToString().ToLower().Contains(q)
        ).ToList();

        Logs = new ObservableCollection<AuditLog>(filtered);
    }
}


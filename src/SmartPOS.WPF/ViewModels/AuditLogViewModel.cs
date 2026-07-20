using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Data.Entities;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using System.Collections.Generic;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;

namespace SmartPOS.WPF.ViewModels;

public record AuditLogDetailItem(string Attribute, string? OldValue, string? NewValue);

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
        set 
        {
            if (SetProperty(ref _selectedLog, value))
            {
                OnSelectedLogChanged(value);
            }
        }
    }

    [ObservableProperty]
    private ObservableCollection<AuditLogDetailItem> _selectedLogDetails = new();

    private void OnSelectedLogChanged(AuditLog? value)
    {
        SelectedLogDetails.Clear();

        if (value == null) return;

        var isAdded = value.Action == "Added";
        var isModified = value.Action == "Modified";

        var oldValues = new Dictionary<string, string>();
        var newValues = new Dictionary<string, string>();

        try
        {
            if (!string.IsNullOrWhiteSpace(value.OldValue))
            {
                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(value.OldValue);
                if (dict != null)
                {
                    foreach (var kvp in dict)
                        oldValues[kvp.Key] = kvp.Value.ToString();
                }
            }
        }
        catch { }

        try
        {
            if (!string.IsNullOrWhiteSpace(value.NewValue))
            {
                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(value.NewValue);
                if (dict != null)
                {
                    foreach (var kvp in dict)
                        newValues[kvp.Key] = kvp.Value.ToString();
                }
            }
        }
        catch { }

        var allKeys = oldValues.Keys.Union(newValues.Keys).Distinct().OrderBy(k => k);

        foreach (var key in allKeys)
        {
            var oldVal = oldValues.TryGetValue(key, out var o) ? o : null;
            var newVal = newValues.TryGetValue(key, out var n) ? n : null;

            if (isAdded)
            {
                if (newVal != null)
                {
                    SelectedLogDetails.Add(new AuditLogDetailItem(key, null, newVal));
                }
            }
            else if (isModified)
            {
                if (oldVal != newVal)
                {
                    SelectedLogDetails.Add(new AuditLogDetailItem(key, oldVal, newVal));
                }
            }
            else 
            {
                SelectedLogDetails.Add(new AuditLogDetailItem(key, oldVal, newVal));
            }
        }
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


using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Shift;
using SmartPOS.Shared.Exceptions;
using SmartPOS.WPF.Session;

namespace SmartPOS.WPF.ViewModels;

public partial class ShiftViewModel : ObservableObject
{
    private readonly IShiftService _shiftService;
    private readonly CurrentSessionContext _session;
    private readonly ILogger<ShiftViewModel> _logger;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private decimal _openingCash;

    [ObservableProperty]
    private decimal _closingCash;

    [ObservableProperty]
    private bool _hasOpenShift;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ShiftDto? _currentShift;

    [ObservableProperty]
    private ShiftSummaryDto? _lastSummary;

    public ShiftViewModel(IShiftService shiftService, CurrentSessionContext session, ILogger<ShiftViewModel> logger)
    {
        _shiftService = shiftService;
        _session = session;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_session.CurrentUser is null) return;

        var openShift = await _shiftService.GetOpenShiftAsync(_session.CurrentUser.UserId);
        if (openShift is not null)
        {
            CurrentShift = openShift;
            _session.CurrentShift = openShift;
            HasOpenShift = true;
            StatusMessage = $"Ca đang mở từ {openShift.OpenedAt:HH:mm dd/MM/yyyy}";
        }
    }

    [RelayCommand]
    private async Task OpenShiftAsync()
    {
        if (_session.CurrentUser is null)
        {
            ErrorMessage = "Chưa đăng nhập";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;

        try
        {
            var dto = new OpenShiftDto(_session.CurrentUser.UserId, OpeningCash);
            var shift = await _shiftService.OpenShiftAsync(dto);
            CurrentShift = shift;
            _session.CurrentShift = shift;
            HasOpenShift = true;
            StatusMessage = "Ca làm việc đã được mở thành công";
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không mong muốn khi mở ca làm việc");
            ErrorMessage = "Đã có lỗi xảy ra, vui lòng kiểm tra nhật ký";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CloseShiftAsync()
    {
        if (CurrentShift is null)
        {
            ErrorMessage = "Không có ca nào đang mở";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;

        try
        {
            var dto = new CloseShiftDto(CurrentShift.Id, ClosingCash);
            await _shiftService.CloseShiftAsync(dto);

            LastSummary = await _shiftService.GetShiftSummaryAsync(CurrentShift.Id);

            _session.CurrentShift = null;
            CurrentShift = null;
            HasOpenShift = false;
            StatusMessage = "Ca làm việc đã được đóng";
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không mong muốn khi đóng ca làm việc");
            ErrorMessage = "Đã có lỗi xảy ra, vui lòng kiểm tra nhật ký";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

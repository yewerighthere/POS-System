using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.Exceptions;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;

namespace SmartPOS.WPF.ViewModels;

public partial class PaymentViewModel : ObservableObject
{
    private readonly IPaymentService _paymentService;
    private readonly CurrentSessionContext _session;
    private readonly NavigationService _navigation;
    private readonly ILogger<PaymentViewModel> _logger;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChangeAmount))]
    [NotifyPropertyChangedFor(nameof(ChangeHint))]
    [NotifyPropertyChangedFor(nameof(IsAmountSufficient))]
    private decimal _totalAmount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChangeAmount))]
    [NotifyPropertyChangedFor(nameof(ChangeHint))]
    [NotifyPropertyChangedFor(nameof(IsAmountSufficient))]
    private decimal _amountReceived;

    [ObservableProperty]
    private bool _isPaymentDone;

    public bool IsNotLoading => !IsLoading;

    public decimal ChangeAmount => AmountReceived >= TotalAmount && TotalAmount > 0
        ? AmountReceived - TotalAmount
        : 0m;

    public bool IsAmountSufficient => TotalAmount > 0 && AmountReceived >= TotalAmount;

    public string ChangeHint
    {
        get
        {
            if (AmountReceived == 0) return "Đang chờ nhập tiền...";
            if (TotalAmount > 0 && AmountReceived < TotalAmount)
                return $"Cần thêm: {(TotalAmount - AmountReceived):N0} đ";
            return "Thanh toán đủ";
        }
    }

    public PaymentViewModel(
        IPaymentService paymentService,
        CurrentSessionContext session,
        NavigationService navigation,
        ILogger<PaymentViewModel> logger)
    {
        _paymentService = paymentService;
        _session = session;
        _navigation = navigation;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_session.CurrentShift is null)
        {
            ErrorMessage = "Chưa mở ca, không thể thanh toán";
            return;
        }

        if (_session.CurrentCart is null || _session.CurrentCart.Items.Count == 0)
        {
            ErrorMessage = "Không có giỏ hàng";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var order = await _paymentService.CreateOrderFromCartAsync(
                _session.CurrentCart,
                _session.CurrentShift.Id,
                _session.RequireUserId());

            TotalAmount = order.TotalAmount;
            _session.PendingOrderId = order.Id;
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo đơn hàng từ giỏ hàng");
            ErrorMessage = "Đã có lỗi xảy ra, vui lòng kiểm tra nhật ký";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void AddQuickCash(string amount)
    {
        if (decimal.TryParse(amount, out var value))
            AmountReceived += value;
    }

    [RelayCommand]
    private async Task ConfirmPaymentAsync()
    {
        if (IsLoading) return;

        if (_session.PendingOrderId is null)
        {
            ErrorMessage = "Đơn hàng chưa được tạo";
            return;
        }

        if (AmountReceived <= 0)
        {
            ErrorMessage = "Vui lòng nhập số tiền khách đưa";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            await _paymentService.RecordCashPaymentAsync(
                _session.PendingOrderId.Value,
                AmountReceived,
                _session.RequireUserId());

            _session.CurrentCart = null;
            _session.PendingOrderId = null;
            IsPaymentDone = true;
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi ghi nhận thanh toán tiền mặt");
            ErrorMessage = "Đã có lỗi xảy ra, vui lòng kiểm tra nhật ký";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _session.CurrentCart = null;
        _session.PendingOrderId = null;
        _navigation.NavigateTo<SalesViewModel>();
    }

    [RelayCommand]
    private void BackToSales()
    {
        _navigation.NavigateTo<SalesViewModel>();
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QRCoder;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.Constants;
using System.IO;
using System.Windows.Media.Imaging;
using SmartPOS.Shared.DTOs.Payment;
using SmartPOS.Shared.Enums;
using SmartPOS.Shared.Exceptions;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;

namespace SmartPOS.WPF.ViewModels;

public partial class PaymentViewModel : ObservableObject
{
    private readonly IPaymentService _paymentService;
    private readonly IInvoiceService _invoiceService;
    private readonly IConfiguration _configuration;
    private readonly CurrentSessionContext _session;
    private readonly NavigationService _navigation;
    private readonly ILogger<PaymentViewModel> _logger;
    private CancellationTokenSource? _vnpayPollingCts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    [NotifyPropertyChangedFor(nameof(IsPaymentInputEnabled))]
    [NotifyPropertyChangedFor(nameof(CanCreateVNPay))]
    [NotifyPropertyChangedFor(nameof(CanConfirmPayment))]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChangeAmount))]
    [NotifyPropertyChangedFor(nameof(ChangeHint))]
    [NotifyPropertyChangedFor(nameof(IsAmountSufficient))]
    [NotifyPropertyChangedFor(nameof(CanConfirmPayment))]
    private decimal _totalAmount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChangeAmount))]
    [NotifyPropertyChangedFor(nameof(ChangeHint))]
    [NotifyPropertyChangedFor(nameof(IsAmountSufficient))]
    [NotifyPropertyChangedFor(nameof(CanConfirmPayment))]
    private decimal _amountReceived;

    [ObservableProperty]
    private bool _isPaymentDone;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotWaitingForVNPay))]
    [NotifyPropertyChangedFor(nameof(IsPaymentInputEnabled))]
    [NotifyPropertyChangedFor(nameof(CanCreateVNPay))]
    [NotifyPropertyChangedFor(nameof(CanConfirmPayment))]
    private bool _isVNPayPending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPaymentUrl))]
    private string _paymentUrl = string.Empty;

    [ObservableProperty]
    private BitmapImage? _paymentQrImage;

    [ObservableProperty]
    private string _paymentMethodMessage = "Thanh toán tiền mặt";

    public bool IsNotLoading => !IsLoading;

    public bool IsNotWaitingForVNPay => !IsVNPayPending;

    public bool IsPaymentInputEnabled => !IsLoading && !IsVNPayPending;

    public bool CanCreateVNPay => !IsLoading && !IsVNPayPending && _session.PendingOrderId is not null;

    public decimal ChangeAmount => AmountReceived >= TotalAmount && TotalAmount > 0
        ? AmountReceived - TotalAmount
        : 0m;

    public bool IsAmountSufficient => TotalAmount > 0 && AmountReceived >= TotalAmount;

    public bool CanConfirmPayment => IsAmountSufficient && !IsLoading && !IsVNPayPending;

    public bool HasPaymentUrl => !string.IsNullOrWhiteSpace(PaymentUrl);

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
        IInvoiceService invoiceService,
        IConfiguration configuration,
        CurrentSessionContext session,
        NavigationService navigation,
        ILogger<PaymentViewModel> logger)
    {
        _paymentService = paymentService;
        _invoiceService = invoiceService;
        _configuration = configuration;
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
        PaymentUrl = string.Empty;
        PaymentQrImage = null;
        PaymentMethodMessage = "Thanh toán tiền mặt";
        IsPaymentDone = false;

        try
        {
            var order = await _paymentService.CreateOrderFromCartAsync(
                _session.CurrentCart,
                _session.CurrentShift.Id,
                _session.RequireUserId()).ConfigureAwait(true);

            TotalAmount = order.TotalAmount;
            _session.PendingOrderId = order.Id;
            OnPropertyChanged(nameof(CanCreateVNPay));
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
    private async Task CreateVNPayAsync()
    {
        if (IsLoading || IsVNPayPending)
            return;

        if (_session.PendingOrderId is null)
        {
            ErrorMessage = "Đơn hàng chưa được tạo";
            return;
        }

        if (TotalAmount <= 0)
        {
            ErrorMessage = "Tổng tiền không hợp lệ";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var orderId = _session.PendingOrderId.Value;
            var result = await _paymentService.CreateVNPayRequestAsync(
                new VNPayRequestDto(
                    orderId,
                    TotalAmount,
                    _configuration["VNPay:ReturnUrl"] ?? "http://localhost:5000/api/vnpay/return")).ConfigureAwait(true);

            PaymentUrl = result.PaymentUrl ?? string.Empty;
            PaymentQrImage = string.IsNullOrWhiteSpace(PaymentUrl) ? null : CreateQrImage(PaymentUrl);
            PaymentMethodMessage = "Đã tạo mã QR VNPay. Đang chờ khách thanh toán...";
            IsVNPayPending = true;
            StartVNPayPolling(orderId);
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo yêu cầu VNPay");
            ErrorMessage = "Đã có lỗi xảy ra, vui lòng kiểm tra nhật ký";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(CanCreateVNPay));
        }
    }

    private static BitmapImage CreateQrImage(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);
        var bytes = qrCode.GetGraphic(12);

        using var stream = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private void StartVNPayPolling(Guid orderId)
    {
        _vnpayPollingCts?.Cancel();
        _vnpayPollingCts?.Dispose();
        _vnpayPollingCts = new CancellationTokenSource();
        _ = PollVNPayStatusAsync(orderId, _vnpayPollingCts.Token);
    }

    private void StopVNPayPolling()
    {
        _vnpayPollingCts?.Cancel();
        _vnpayPollingCts?.Dispose();
        _vnpayPollingCts = null;
    }

    private async Task PollVNPayStatusAsync(Guid orderId, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(AppConstants.PaymentPollIntervalSeconds), cancellationToken).ConfigureAwait(true);
                var status = await _paymentService.GetOrderPaymentStatusAsync(orderId).ConfigureAwait(true);

                if (status == PaymentStatus.Pending)
                    continue;

                await HandleVNPayTerminalStatusAsync(orderId, status).ConfigureAwait(true);
                return;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi kiểm tra trạng thái VNPay cho đơn hàng {OrderId}", orderId);
            IsVNPayPending = false;
            ErrorMessage = "Không thể kiểm tra trạng thái VNPay, vui lòng thử lại";
            PaymentMethodMessage = "Mất kết nối khi kiểm tra trạng thái VNPay";
        }
    }

    private async Task<Guid?> EnsureInvoiceAsync(Guid orderId)
    {
        var invoice = await _invoiceService.GetByOrderIdAsync(orderId).ConfigureAwait(true)
            ?? await _invoiceService.CreateInvoiceAsync(orderId).ConfigureAwait(true);

        return invoice.Id;
    }

    private async Task HandleVNPayTerminalStatusAsync(Guid orderId, PaymentStatus status)
    {
        StopVNPayPolling();
        IsVNPayPending = false;

        if (status == PaymentStatus.Success)
        {
            ErrorMessage = string.Empty;
            PaymentMethodMessage = "Thanh toán VNPay thành công";
            _session.LastPaidOrderId = orderId;
            _session.LastInvoiceId = await EnsureInvoiceAsync(orderId).ConfigureAwait(true);
            _session.CurrentCart = null;
            _session.PendingOrderId = null;
            PaymentUrl = string.Empty;
            PaymentQrImage = null;
            IsPaymentDone = true;
            OnPropertyChanged(nameof(CanCreateVNPay));
            return;
        }

        if (status == PaymentStatus.Failed)
        {
            ErrorMessage = "Thanh toán VNPay thất bại";
            PaymentMethodMessage = "Thanh toán VNPay thất bại. Vui lòng thử lại hoặc chọn tiền mặt.";
            PaymentUrl = string.Empty;
            PaymentQrImage = null;
            OnPropertyChanged(nameof(CanCreateVNPay));
            return;
        }

        if (status == PaymentStatus.Timeout)
        {
            ErrorMessage = "Thanh toán VNPay đã hết hạn";
            PaymentMethodMessage = "Thanh toán VNPay đã hết hạn. Vui lòng tạo giao dịch mới.";
            PaymentUrl = string.Empty;
            PaymentQrImage = null;
            OnPropertyChanged(nameof(CanCreateVNPay));
        }
    }

    [RelayCommand]
    private async Task ConfirmPaymentAsync()
    {
        if (IsLoading) return;

        if (IsVNPayPending)
        {
            ErrorMessage = "Đang chờ thanh toán VNPay, vui lòng hủy VNPay trước khi thanh toán tiền mặt";
            return;
        }

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
            var result = await _paymentService.RecordCashPaymentAsync(
                _session.PendingOrderId.Value,
                AmountReceived,
                _session.RequireUserId()).ConfigureAwait(true);

            StopVNPayPolling();
            _session.LastPaidOrderId = result.OrderId;
            _session.LastInvoiceId = await EnsureInvoiceAsync(result.OrderId).ConfigureAwait(true);

            _session.CurrentCart = null;
            _session.PendingOrderId = null;
            PaymentUrl = string.Empty;
            PaymentQrImage = null;
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
    private async Task ShowInvoiceAsync()
    {
        if (_session.LastInvoiceId is null && _session.LastPaidOrderId is Guid orderId)
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                _session.LastInvoiceId = await EnsureInvoiceAsync(orderId).ConfigureAwait(true);
            }
            catch (BusinessException ex)
            {
                ErrorMessage = ex.Message;
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo hoặc tải hóa đơn cho đơn hàng {OrderId}", orderId);
                ErrorMessage = "Không thể mở hóa đơn, vui lòng kiểm tra nhật ký";
                return;
            }
            finally
            {
                IsLoading = false;
            }
        }

        if (_session.LastInvoiceId is null)
        {
            ErrorMessage = "Chưa có hóa đơn để xem";
            return;
        }

        _navigation.NavigateTo<InvoiceViewModel>();
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        var orderId = _session.PendingOrderId;

        if (IsVNPayPending && orderId is Guid pendingOrderId)
        {
            StopVNPayPolling();
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                await _paymentService.CancelVNPayAsync(pendingOrderId).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hủy thanh toán VNPay cho đơn hàng {OrderId}", pendingOrderId);
                IsVNPayPending = true;
                StartVNPayPolling(pendingOrderId);
                ErrorMessage = "Không thể hủy thanh toán VNPay, vui lòng thử lại";
                PaymentMethodMessage = "Đang chờ thanh toán VNPay";
                IsLoading = false;
                return;
            }
            finally
            {
                IsLoading = false;
            }
        }

        IsVNPayPending = false;
        _session.CurrentCart = null;
        _session.PendingOrderId = null;
        _session.LastPaidOrderId = null;
        _session.LastInvoiceId = null;
        PaymentUrl = string.Empty;
        PaymentQrImage = null;
        PaymentMethodMessage = "Đã hủy thanh toán VNPay";
        _navigation.NavigateTo<SalesViewModel>();
    }

    [RelayCommand]
    private void BackToSales()
    {
        StopVNPayPolling();
        IsVNPayPending = false;
        _session.LastPaidOrderId = null;
        _session.LastInvoiceId = null;
        PaymentUrl = string.Empty;
        PaymentQrImage = null;
        _navigation.NavigateTo<SalesViewModel>();
    }
}

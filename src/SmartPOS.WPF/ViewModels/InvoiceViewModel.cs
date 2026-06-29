using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SmartPOS.Services.Interfaces;
using System.Globalization;
using System.Text;
using SmartPOS.Shared.DTOs.Invoice;
using SmartPOS.Shared.Exceptions;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;

namespace SmartPOS.WPF.ViewModels;

public partial class InvoiceViewModel : ObservableObject
{
    private readonly IInvoiceService _invoiceService;
    private readonly CurrentSessionContext _session;
    private readonly NavigationService _navigation;
    private readonly ILogger<InvoiceViewModel> _logger;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IssuedAtLocal))]
    private InvoiceDto? _invoice;

    public DateTime? IssuedAtLocal => Invoice?.IssuedAt.ToLocalTime();

    [ObservableProperty]
    private string _invoiceStatus = "Chưa có hóa đơn";

    [ObservableProperty]
    private bool _isPreviewVisible;

    [ObservableProperty]
    private string _receiptPreview = string.Empty;

    public InvoiceViewModel(
        IInvoiceService invoiceService,
        CurrentSessionContext session,
        NavigationService navigation,
        ILogger<InvoiceViewModel> logger)
    {
        _invoiceService = invoiceService;
        _session = session;
        _navigation = navigation;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            if (_session.LastInvoiceId is Guid invoiceId)
            {
                Invoice = await _invoiceService.GetByIdAsync(invoiceId).ConfigureAwait(false);
            }
            else if (_session.LastPaidOrderId is Guid orderId)
            {
                Invoice = await _invoiceService.GetByOrderIdAsync(orderId).ConfigureAwait(false);
            }

            if (Invoice is null)
            {
                InvoiceStatus = "Không tìm thấy hóa đơn";
                ErrorMessage = "Chưa có hóa đơn để hiển thị";
                return;
            }

            InvoiceStatus = $"Hóa đơn {Invoice.InvoiceNumber}";
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Không thể tải hóa đơn");
            ErrorMessage = ex.Message;
            InvoiceStatus = "Không thể tải hóa đơn";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi tải hóa đơn");
            ErrorMessage = "Đã có lỗi xảy ra khi tải hóa đơn";
            InvoiceStatus = "Không thể tải hóa đơn";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string BuildReceiptPreview(InvoiceDto invoice)
    {
        var culture = CultureInfo.GetCultureInfo("vi-VN");
        var builder = new StringBuilder();
        builder.AppendLine("CỬA HÀNG SMARTPOS");
        builder.AppendLine("--------------------------------");
        builder.AppendLine($"Hóa đơn: {invoice.InvoiceNumber}");
        builder.AppendLine($"Mã đơn: {invoice.OrderId}");
        builder.AppendLine($"Ngày: {invoice.IssuedAt.ToLocalTime():dd/MM/yyyy HH:mm:ss}");
        builder.AppendLine("--------------------------------");
        builder.AppendLine($"Tổng tiền: {invoice.TotalAmount.ToString("N0", culture)} đ");
        builder.AppendLine("--------------------------------");
        builder.AppendLine("Thanh toán: Đã thanh toán");
        builder.AppendLine("In giả lập: Thành công");
        builder.AppendLine("Cảm ơn quý khách");
        return builder.ToString();
    }

    [RelayCommand]
    private async Task PreviewAsync()
    {
        if (Invoice is null)
        {
            ErrorMessage = "Chưa có hóa đơn để xem trước";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            await _invoiceService.PrintPreviewAsync(Invoice.Id).ConfigureAwait(true);
            ReceiptPreview = BuildReceiptPreview(Invoice);
            IsPreviewVisible = true;
            InvoiceStatus = $"In giả lập thành công {Invoice.InvoiceNumber}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xem trước hóa đơn");
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClosePreview()
    {
        IsPreviewVisible = false;
    }

    [RelayCommand]
    private void Close()
    {
        _navigation.NavigateTo<SalesViewModel>();
    }
}

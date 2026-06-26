using Microsoft.Extensions.Logging;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Invoice;
using SmartPOS.Shared.Enums;
using SmartPOS.Shared.Exceptions;

namespace SmartPOS.Services.Implementations;

public class InvoiceService : IInvoiceService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IDeviceService _deviceService;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IOrderRepository orderRepository,
        IInvoiceRepository invoiceRepository,
        IDeviceService deviceService,
        ILogger<InvoiceService> logger)
    {
        _orderRepository = orderRepository;
        _invoiceRepository = invoiceRepository;
        _deviceService = deviceService;
        _logger = logger;
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(Guid orderId)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(orderId).ConfigureAwait(false)
            ?? throw new BusinessException("Không tìm thấy đơn hàng");

        if (order.PaymentStatus != PaymentStatus.Success)
            throw new BusinessException("Chỉ tạo hóa đơn khi thanh toán thành công");

        var existing = await _invoiceRepository.GetByOrderIdAsync(orderId).ConfigureAwait(false);
        if (existing is not null)
        {
            return new InvoiceDto
            {
                Id = existing.Id,
                OrderId = existing.OrderId,
                InvoiceNumber = existing.InvoiceNumber,
                TotalAmount = existing.TotalAmount,
                IssuedAt = existing.IssuedAt
            };
        }

        var now = DateTime.UtcNow;
        var sequence = await _invoiceRepository.GetDailySequenceAsync(DateOnly.FromDateTime(now)).ConfigureAwait(false) + 1;
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            InvoiceNumber = $"INV-{now:yyyyMMdd}-{sequence:D4}",
            TotalAmount = order.TotalAmount,
            IssuedAt = now
        };

        await _invoiceRepository.AddAsync(invoice).ConfigureAwait(false);

        return new InvoiceDto
        {
            Id = invoice.Id,
            OrderId = invoice.OrderId,
            InvoiceNumber = invoice.InvoiceNumber,
            TotalAmount = invoice.TotalAmount,
            IssuedAt = invoice.IssuedAt
        };
    }

    public async Task<InvoiceDto?> GetByOrderIdAsync(Guid orderId)
    {
        var invoice = await _invoiceRepository.GetByOrderIdAsync(orderId).ConfigureAwait(false);
        return invoice is null
            ? null
            : new InvoiceDto
            {
                Id = invoice.Id,
                OrderId = invoice.OrderId,
                InvoiceNumber = invoice.InvoiceNumber,
                TotalAmount = invoice.TotalAmount,
                IssuedAt = invoice.IssuedAt
            };
    }

    public async Task<InvoiceDto?> GetByIdAsync(Guid invoiceId)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId).ConfigureAwait(false);
        return invoice is null
            ? null
            : new InvoiceDto
            {
                Id = invoice.Id,
                OrderId = invoice.OrderId,
                InvoiceNumber = invoice.InvoiceNumber,
                TotalAmount = invoice.TotalAmount,
                IssuedAt = invoice.IssuedAt
            };
    }

    public async Task PrintPreviewAsync(Guid invoiceId)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId).ConfigureAwait(false);
        if (invoice is null)
            throw new BusinessException("Không tìm thấy hóa đơn");

        await _deviceService.LogDeviceEventAsync(null, "INVOICE_PREVIEW", $"Xem trước hóa đơn {invoice.InvoiceNumber}").ConfigureAwait(false);
        _logger.LogInformation("Xem trước hóa đơn {InvoiceNumber}", invoice.InvoiceNumber);
    }
}

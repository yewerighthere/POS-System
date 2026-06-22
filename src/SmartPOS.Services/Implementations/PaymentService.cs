using Microsoft.Extensions.Logging;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Cart;
using SmartPOS.Shared.DTOs.Order;
using SmartPOS.Shared.DTOs.Payment;
using SmartPOS.Shared.Enums;
using SmartPOS.Shared.Exceptions;

namespace SmartPOS.Services.Implementations;

public class PaymentService : IPaymentService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventorySyncService _inventorySyncService;
    private readonly IAuditService _auditService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IOrderRepository orderRepository,
        IInventorySyncService inventorySyncService,
        IAuditService auditService,
        ILogger<PaymentService> logger)
    {
        _orderRepository = orderRepository;
        _inventorySyncService = inventorySyncService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<OrderDto> CreateOrderFromCartAsync(CartSummaryDto cart, Guid shiftId, Guid userId)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ShiftId = shiftId,
            UserId = userId,
            Status = OrderStatus.Draft,
            PaymentStatus = PaymentStatus.Pending,
            IsLocked = false,
            Subtotal = cart.Subtotal,
            DiscountAmount = cart.DiscountAmount,
            TaxAmount = cart.TaxAmount,
            TotalAmount = cart.TotalAmount,
            CreatedAt = DateTime.UtcNow,
            Items = cart.Items.Select(item => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Sku = string.Empty,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                DiscountAmount = 0,
                Subtotal = item.Subtotal
            }).ToList()
        };

        await _orderRepository.AddAsync(order).ConfigureAwait(false);

        return new OrderDto
        {
            Id = order.Id,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                Subtotal = i.Subtotal
            }).ToList()
        };
    }

    public async Task<PaymentResultDto> RecordCashPaymentAsync(Guid orderId, decimal amountReceived, Guid userId)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(orderId).ConfigureAwait(false)
            ?? throw new BusinessException("Không tìm thấy đơn hàng");

        if (order.IsLocked)
            throw new BusinessException("Đơn hàng đang bị khóa bởi phiên thanh toán khác");

        if (amountReceived < order.TotalAmount)
            throw new BusinessException("Số tiền khách đưa không đủ để thanh toán");

        var changeAmount = amountReceived - order.TotalAmount;

        order.PaymentMethod = PaymentMethod.Cash;
        order.PaymentStatus = PaymentStatus.Success;
        order.Status = OrderStatus.Confirmed;
        order.UpdatedAt = DateTime.UtcNow;

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            PaymentMethod = PaymentMethod.Cash,
            AmountReceived = (decimal?)amountReceived,
            ChangeAmount = (decimal?)changeAmount,
            PaymentStatus = PaymentStatus.Success,
            CreatedAt = DateTime.UtcNow
        };

        await _orderRepository.AddPaymentAsync(order, payment).ConfigureAwait(false);

        var orderItemDtos = order.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            Subtotal = i.Subtotal
        });

        try
        {
            await _inventorySyncService.SendStockDeductionAsync(orderItemDtos, orderId).ConfigureAwait(false);
        }
        catch (NotImplementedException)
        {
            _logger.LogWarning("InventorySyncService.SendStockDeductionAsync chưa được triển khai, bỏ qua trừ kho cho đơn hàng {OrderId}", orderId);
        }

        try
        {
            await _auditService.LogAsync("CASH_PAYMENT", "Order", orderId, null, new { orderId, amountReceived }, userId).ConfigureAwait(false);
        }
        catch (NotImplementedException)
        {
            _logger.LogWarning("AuditService.LogAsync chưa được triển khai, bỏ qua ghi audit cho đơn hàng {OrderId}", orderId);
        }

        _logger.LogInformation("Đã ghi nhận thanh toán tiền mặt cho đơn hàng {OrderId}", orderId);

        return new PaymentResultDto
        {
            OrderId = orderId,
            AmountReceived = amountReceived,
            ChangeAmount = changeAmount,
            PaymentStatus = PaymentStatus.Success.ToString()
        };
    }

    public Task<PaymentResultDto> CreateVNPayRequestAsync(VNPayRequestDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<PaymentResultDto> HandleVNPayCallbackAsync(VNPayCallbackDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<PaymentStatus> GetOrderPaymentStatusAsync(Guid orderId)
    {
        throw new NotImplementedException();
    }

    public Task CancelVNPayAsync(Guid orderId)
    {
        throw new NotImplementedException();
    }
}

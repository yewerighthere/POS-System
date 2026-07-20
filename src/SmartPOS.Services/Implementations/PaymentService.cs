using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.Constants;
using SmartPOS.Shared.DTOs.Cart;
using SmartPOS.Shared.DTOs.Order;
using SmartPOS.Shared.DTOs.Payment;
using SmartPOS.Shared.Enums;
using SmartPOS.Shared.Exceptions;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SmartPOS.Services.Implementations;

public class PaymentService : IPaymentService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventorySyncService _inventorySyncService;
    private readonly ICustomerService _customerService;
    private readonly IInvoiceService? _invoiceService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IOrderRepository orderRepository,
        IInventorySyncService inventorySyncService,
        ICustomerService customerService,
        ILogger<PaymentService> logger,
        IInvoiceService? invoiceService = null,
        IConfiguration? configuration = null)
    {
        _orderRepository = orderRepository;
        _inventorySyncService = inventorySyncService;
        _customerService = customerService;
        _invoiceService = invoiceService;
        _configuration = configuration ?? new ConfigurationBuilder().AddInMemoryCollection().Build();
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
            CustomerId = cart.Customer?.Id,
            PointsEarned = cart.PointsEarned,
            PointsUsed = cart.PointsUsed,
            PointsDiscountAmount = cart.PointsDiscountAmount,
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

        return MapOrder(order);
    }

    public async Task<PaymentResultDto> RecordCashPaymentAsync(Guid orderId, decimal amountReceived, Guid userId)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(orderId).ConfigureAwait(false)
            ?? throw new BusinessException("Không tìm thấy đơn hàng");

        if (order.IsLocked)
            throw new BusinessException("Đơn hàng đang bị khóa bởi phiên thanh toán khác");

        if (amountReceived < order.TotalAmount)
            throw new BusinessException("Số tiền khách đưa không đủ để thanh toán");

        if (order.PaymentStatus == PaymentStatus.Success)
            throw new BusinessException("Đơn hàng đã được thanh toán, không thể thanh toán lại");

        var changeAmount = amountReceived - order.TotalAmount;

        order.PaymentMethod = PaymentMethod.Cash;
        order.PaymentStatus = PaymentStatus.Success;
        order.Status = OrderStatus.Confirmed;
        order.IsLocked = false;
        order.UpdatedAt = DateTime.UtcNow;

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            PaymentMethod = PaymentMethod.Cash,
            AmountReceived = amountReceived,
            ChangeAmount = changeAmount,
            PaymentStatus = PaymentStatus.Success,
            CreatedAt = DateTime.UtcNow
        };

        await _orderRepository.AddPaymentAsync(order, payment).ConfigureAwait(false);
        
        if (order.CustomerId.HasValue)
        {
            await _customerService.DeductLoyaltyPointsAsync(order.CustomerId.Value, order.PointsUsed).ConfigureAwait(false);
            await _customerService.AddLoyaltyPointsAsync(order.CustomerId.Value, order.PointsEarned).ConfigureAwait(false);
        }
        
        await CreateInvoiceIfNeededAsync(orderId).ConfigureAwait(false);

        await NotifySideEffectsAsync(order, userId).ConfigureAwait(false);

        _logger.LogInformation("Đã ghi nhận thanh toán tiền mặt cho đơn hàng {OrderId}", orderId);

        return new PaymentResultDto
        {
            OrderId = orderId,
            AmountReceived = amountReceived,
            ChangeAmount = changeAmount,
            PaymentStatus = PaymentStatus.Success.ToString()
        };
    }

    public async Task<PaymentResultDto> CreateVNPayRequestAsync(VNPayRequestDto dto)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(dto.OrderId).ConfigureAwait(false)
            ?? throw new BusinessException("Không tìm thấy đơn hàng");

        if (order.PaymentStatus == PaymentStatus.Success)
            throw new BusinessException("Đơn hàng đã được thanh toán, không thể tạo yêu cầu VNPay");

        order.IsLocked = true;
        order.PaymentMethod = PaymentMethod.VNPay;
        order.PaymentStatus = PaymentStatus.Pending;
        order.UpdatedAt = DateTime.UtcNow;

        var pendingPayment = order.Payments.FirstOrDefault(payment => payment.PaymentMethod == PaymentMethod.VNPay && payment.PaymentStatus == PaymentStatus.Pending);
        if (pendingPayment is null)
        {
            pendingPayment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                PaymentMethod = PaymentMethod.VNPay,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _orderRepository.AddPaymentAsync(order, pendingPayment).ConfigureAwait(false);
        }
        else
        {
            await _orderRepository.UpdateAsync(order).ConfigureAwait(false);
        }

        var paymentUrl = BuildVNPayUrl(dto, order.Id);
        return new PaymentResultDto
        {
            OrderId = order.Id,
            PaymentStatus = PaymentStatus.Pending.ToString(),
            PaymentUrl = paymentUrl
        };
    }

    public async Task<PaymentResultDto> HandleVNPayCallbackAsync(VNPayCallbackDto dto)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(dto.OrderId).ConfigureAwait(false)
            ?? throw new BusinessException("Không tìm thấy đơn hàng");

        var payment = order.Payments
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault(p => p.PaymentMethod == PaymentMethod.VNPay);

        if (payment is null)
            throw new BusinessException("Không tìm thấy phiên thanh toán VNPay");

        if (payment.PaymentStatus == PaymentStatus.Success)
        {
            return new PaymentResultDto
            {
                OrderId = order.Id,
                PaymentStatus = PaymentStatus.Success.ToString(),
                PaymentUrl = BuildVNPayUrl(new VNPayRequestDto(order.Id, order.TotalAmount, GetVNPayReturnUrl()), order.Id)
            };
        }

        payment.TransactionId = dto.TransactionId;
        payment.VnpayResponse = JsonSerializer.Serialize(dto);

        var isSuccess = string.Equals(dto.ResponseCode, "00", StringComparison.OrdinalIgnoreCase);
        payment.PaymentStatus = isSuccess ? PaymentStatus.Success : PaymentStatus.Failed;
        order.PaymentStatus = payment.PaymentStatus;
        order.PaymentMethod = PaymentMethod.VNPay;
        order.IsLocked = false;
        order.UpdatedAt = DateTime.UtcNow;

        if (isSuccess)
        {
            order.Status = OrderStatus.Confirmed;
            if (order.CustomerId.HasValue)
            {
                await _customerService.DeductLoyaltyPointsAsync(order.CustomerId.Value, order.PointsUsed).ConfigureAwait(false);
                await _customerService.AddLoyaltyPointsAsync(order.CustomerId.Value, order.PointsEarned).ConfigureAwait(false);
            }
            await CreateInvoiceIfNeededAsync(order.Id).ConfigureAwait(false);
            await NotifySideEffectsAsync(order, order.UserId).ConfigureAwait(false);
        }
        else
        {
            order.Status = OrderStatus.Draft;
        }

        await _orderRepository.UpdateAsync(order).ConfigureAwait(false);

        return new PaymentResultDto
        {
            OrderId = order.Id,
            PaymentStatus = payment.PaymentStatus.ToString(),
            PaymentUrl = BuildVNPayUrl(new VNPayRequestDto(order.Id, order.TotalAmount, GetVNPayReturnUrl()), order.Id)
        };
    }

    public async Task<PaymentStatus> GetOrderPaymentStatusAsync(Guid orderId)
    {
        return await _orderRepository.GetPaymentStatusAsync(orderId).ConfigureAwait(false)
            ?? throw new BusinessException("Không tìm thấy đơn hàng");
    }

    public async Task CancelVNPayAsync(Guid orderId)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(orderId).ConfigureAwait(false)
            ?? throw new BusinessException("Không tìm thấy đơn hàng");

        order.IsLocked = false;
        order.PaymentStatus = PaymentStatus.Timeout;
        order.Status = OrderStatus.Draft;
        order.UpdatedAt = DateTime.UtcNow;

        var payment = order.Payments
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault(p => p.PaymentMethod == PaymentMethod.VNPay);

        if (payment is not null && payment.PaymentStatus == PaymentStatus.Pending)
        {
            payment.PaymentStatus = PaymentStatus.Timeout;
            payment.VnpayResponse = JsonSerializer.Serialize(new { reason = "timeout", orderId });
        }

        await _orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }

    private async Task CreateInvoiceIfNeededAsync(Guid orderId)
    {
        if (_invoiceService is null)
            return;

        try
        {
            await _invoiceService.CreateInvoiceAsync(orderId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể tạo hóa đơn cho đơn hàng {OrderId}", orderId);
        }
    }

    private async Task NotifySideEffectsAsync(Order order, Guid userId)
    {
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
            await _inventorySyncService.SendStockDeductionAsync(orderItemDtos, order.Id).ConfigureAwait(false);
        }
        catch (NotImplementedException)
        {
            _logger.LogWarning("InventorySyncService.SendStockDeductionAsync chưa được triển khai, bỏ qua trừ kho cho đơn hàng {OrderId}", order.Id);
        }
    }

    private string BuildVNPayUrl(VNPayRequestDto dto, Guid orderId)
    {
        var baseUrl = _configuration["VNPay:PaymentUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        var tmnCode = _configuration["VNPay:TmnCode"] ?? "YOUR_TMN_CODE";
        var hashSecret = _configuration["VNPay:HashSecret"] ?? "YOUR_HASH_SECRET";
        var returnUrl = string.IsNullOrWhiteSpace(dto.ReturnUrl) ? GetVNPayReturnUrl() : dto.ReturnUrl;
        var amount = ((long)Math.Round(dto.Amount, 0, MidpointRounding.AwayFromZero) * 100).ToString(CultureInfo.InvariantCulture);
        var txnRef = orderId.ToString("N");
        var createDate = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Amount"] = amount,
            ["vnp_Command"] = "pay",
            ["vnp_CreateDate"] = createDate,
            ["vnp_CurrCode"] = "VND",
            ["vnp_IpAddr"] = "127.0.0.1",
            ["vnp_Locale"] = "vn",
            ["vnp_OrderInfo"] = $"Thanh toan don hang {txnRef}",
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_TmnCode"] = tmnCode,
            ["vnp_TxnRef"] = txnRef,
            ["vnp_Version"] = AppConstants.VNPayVersion
        };

        var query = string.Join("&", parameters.Select(parameter => $"{parameter.Key}={WebUtility.UrlEncode(parameter.Value)}"));
        var signature = CreateHmacSha512(hashSecret, query);

        return $"{baseUrl}?{query}&vnp_SecureHash={signature}";
    }

    private string GetVNPayReturnUrl()
    {
        return _configuration["VNPay:ReturnUrl"] ?? "http://localhost:5000/api/vnpay/return";
    }

    private static string CreateHmacSha512(string key, string data)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        using var hmac = new HMACSHA512(bytes);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        return Convert.ToHexString(hmac.ComputeHash(dataBytes)).ToLowerInvariant();
    }

    private static OrderDto MapOrder(Order order) => new()
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

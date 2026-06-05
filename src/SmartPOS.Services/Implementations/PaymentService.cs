using Microsoft.Extensions.Logging;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Auth;
using SmartPOS.Shared.DTOs.Shift;
using SmartPOS.Shared.DTOs.Product;
using SmartPOS.Shared.DTOs.Cart;
using SmartPOS.Shared.DTOs.Order;
using SmartPOS.Shared.DTOs.Payment;
using SmartPOS.Shared.DTOs.Invoice;
using SmartPOS.Shared.DTOs.Customer;
using SmartPOS.Shared.DTOs.Return;
using SmartPOS.Shared.DTOs.Catalog;
using SmartPOS.Shared.DTOs.Inventory;
using SmartPOS.Shared.DTOs.Report;
using SmartPOS.Shared.DTOs.Promotion;
using SmartPOS.Shared.Enums;

namespace SmartPOS.Services.Implementations;

public class PaymentService : IPaymentService
{
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(ILogger<PaymentService> logger)
    {
        _logger = logger;
    }

    public Task<PaymentResultDto> RecordCashPaymentAsync(Guid orderId, decimal amountReceived, Guid userId)
    {
        throw new NotImplementedException();
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


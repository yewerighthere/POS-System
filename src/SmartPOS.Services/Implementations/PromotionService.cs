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

public class PromotionService : IPromotionService
{
    private readonly ILogger<PromotionService> _logger;

    public PromotionService(ILogger<PromotionService> logger)
    {
        _logger = logger;
    }

    public Task<PromotionValidationResultDto> ValidateCodeAsync(string code, CartSummaryDto cart)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RequestApprovalAsync(Guid promotionId, Guid managerId)
    {
        throw new NotImplementedException();
    }

    public Task<CartSummaryDto> ApplyPromotionAsync(string code, CartSummaryDto cart)
    {
        throw new NotImplementedException();
    }
}


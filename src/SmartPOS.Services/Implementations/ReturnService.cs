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

public class ReturnService : IReturnService
{
    private readonly ILogger<ReturnService> _logger;

    public ReturnService(ILogger<ReturnService> logger)
    {
        _logger = logger;
    }

    public Task<ReturnDto> CreateReturnAsync(ReturnRequestDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<ReturnDto> ApproveAsync(Guid returnId, Guid approvedBy)
    {
        throw new NotImplementedException();
    }

    public Task<ReturnDto> RejectAsync(Guid returnId, Guid approvedBy)
    {
        throw new NotImplementedException();
    }
}


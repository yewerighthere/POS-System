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

public class ShiftService : IShiftService
{
    private readonly ILogger<ShiftService> _logger;

    public ShiftService(ILogger<ShiftService> logger)
    {
        _logger = logger;
    }

    public Task<ShiftDto> OpenShiftAsync(OpenShiftDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<ShiftDto> CloseShiftAsync(CloseShiftDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<ShiftDto?> GetOpenShiftAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<ShiftSummaryDto> GetShiftSummaryAsync(Guid shiftId)
    {
        throw new NotImplementedException();
    }
}


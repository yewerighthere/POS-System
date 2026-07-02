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

namespace SmartPOS.Services.Interfaces;

public interface ICustomerService
{
    Task<CustomerDto?> FindByPhoneAsync(string phone); 
    Task<CustomerDto> CreateAsync(CreateCustomerDto dto); 
    Task AddLoyaltyPointsAsync(Guid customerId, int points); 
    Task DeductLoyaltyPointsAsync(Guid customerId, int points);
    int CalculatePoints(decimal subtotal);
    Task<IEnumerable<CustomerListDto>> GetCustomerListAsync(string? searchTerm, string? statusFilter, string? orderFilter, string? sortOption);
    Task ToggleCustomerStatusAsync(Guid customerId);
}


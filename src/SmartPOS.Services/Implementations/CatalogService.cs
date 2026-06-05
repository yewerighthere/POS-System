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

public class CatalogService : ICatalogService
{
    private readonly ILogger<CatalogService> _logger;

    public CatalogService(ILogger<CatalogService> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync()
    {
        throw new NotImplementedException();
    }

    public Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<ProductDto> CreateProductAsync(CreateProductDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<ProductDto> UpdatePriceAsync(UpdatePriceDto dto)
    {
        throw new NotImplementedException();
    }
}


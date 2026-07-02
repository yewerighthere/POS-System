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

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository productRepository, ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<ProductDto?> FindByBarcodeAsync(string barcode)
    {
        var product = await _productRepository.GetByBarcodeAsync(barcode).ConfigureAwait(false);
        return product is null ? null : MapToDto(product);
    }

    public async Task<ProductDto?> FindByIdAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id).ConfigureAwait(false);
        return product is null ? null : MapToDto(product);
    }

    public async Task<ProductSearchResultDto> SearchAsync(string keyword)
    {
        var products = string.IsNullOrWhiteSpace(keyword)
            ? await _productRepository.GetAllAsync().ConfigureAwait(false)
            : await _productRepository.SearchAsync(keyword).ConfigureAwait(false);
        return new ProductSearchResultDto
        {
            Products = products.Select(MapToDto).ToList()
        };
    }

    private static ProductDto MapToDto(SmartPOS.Data.Entities.Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Sku = p.Sku,
        Barcode = p.Barcode,
        UnitPrice = p.UnitPrice,
        LocalStockQuantity = p.LocalStockQuantity,
        IsActive = p.IsActive,
        TaxRate = p.TaxRate,
        ImagePath = p.ImagePath
    };
}


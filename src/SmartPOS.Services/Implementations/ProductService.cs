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

    private static readonly Dictionary<string, string> FallbackImages = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Cà phê sữa", "https://images.unsplash.com/photo-1541167760496-1628856ab772?w=200" },
        { "Trà sữa trân châu", "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=200" },
        { "Nước suối", "https://images.unsplash.com/photo-1608885898957-a599fb1b4a41?w=200" },
        { "Nước ngọt Coca", "https://images.unsplash.com/photo-1622483767028-3f66f32aef97?w=200" },
        { "Cơm gà xối mỡ", "https://images.unsplash.com/photo-1569058242253-92a9c755a0ec?w=200" },
        { "Bánh mì thịt", "https://images.unsplash.com/photo-1509722747041-616f39b57569?w=200" },
        { "Phở bò", "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?w=200" },
        { "Bún bò Huế", "https://images.unsplash.com/photo-1625398407796-82650a8c135f?w=200" },
        { "Oreo", "https://images.unsplash.com/photo-1558961309-dbdf71799f54?w=200" },
        { "Pringles", "https://images.unsplash.com/photo-1518047601542-79f18c655718?w=200" }
    };

    private static string GetProductImage(string productName, string? existingPath)
    {
        if (!string.IsNullOrEmpty(existingPath))
            return existingPath;

        foreach (var key in FallbackImages.Keys)
        {
            if (productName.Contains(key, StringComparison.OrdinalIgnoreCase))
                return FallbackImages[key];
        }

        return "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=200"; // Generic fallback
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
        CategoryName = p.Category?.Name ?? string.Empty,
        ImagePath = GetProductImage(p.Name, p.ImagePath)
    };
}


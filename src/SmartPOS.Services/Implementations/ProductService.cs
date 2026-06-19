using Microsoft.Extensions.Logging;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Product;

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
        _logger.LogInformation("Finding product by barcode: {Barcode}", barcode);
        if (string.IsNullOrWhiteSpace(barcode))
            return null;

        var product = await _productRepository.GetByBarcodeAsync(barcode).ConfigureAwait(false);
        if (product == null)
        {
            _logger.LogWarning("Product with barcode {Barcode} not found", barcode);
            return null;
        }

        return MapToDto(product);
    }

    public async Task<ProductDto?> FindByIdAsync(Guid id)
    {
        _logger.LogInformation("Finding product by ID: {Id}", id);
        var product = await _productRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (product == null)
        {
            _logger.LogWarning("Product with ID {Id} not found", id);
            return null;
        }

        return MapToDto(product);
    }

    public async Task<ProductSearchResultDto> SearchAsync(string keyword)
    {
        _logger.LogInformation("Searching products with keyword: {Keyword}", keyword);
        var products = await _productRepository.SearchAsync(keyword).ConfigureAwait(false);
        return new ProductSearchResultDto
        {
            Products = products.Select(MapToDto).ToList()
        };
    }

    private static ProductDto MapToDto(Data.Entities.Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            Barcode = product.Barcode,
            UnitPrice = product.UnitPrice,
            LocalStockQuantity = product.LocalStockQuantity
        };
    }
}


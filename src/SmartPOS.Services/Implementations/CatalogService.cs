using Microsoft.Extensions.Logging;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Auth;
using SmartPOS.Shared.DTOs.Cart;
using SmartPOS.Shared.DTOs.Catalog;
using SmartPOS.Shared.DTOs.Customer;
using SmartPOS.Shared.DTOs.Inventory;
using SmartPOS.Shared.DTOs.Invoice;
using SmartPOS.Shared.DTOs.Order;
using SmartPOS.Shared.DTOs.Payment;
using SmartPOS.Shared.DTOs.Product;
using SmartPOS.Shared.DTOs.Promotion;
using SmartPOS.Shared.DTOs.Report;
using SmartPOS.Shared.DTOs.Return;
using SmartPOS.Shared.DTOs.Shift;
using SmartPOS.Shared.Enums;

namespace SmartPOS.Services.Implementations;

public class CatalogService : ICatalogService
{
    private readonly ILogger<CatalogService> _logger;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;

    public CatalogService(ILogger<CatalogService> logger, ICategoryRepository categoryRepository, IProductRepository productRepository)
    {
        _logger = logger;
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name
        }).ToList();
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = dto.Name
        };
        await _categoryRepository.AddAsync(category);
        _logger.LogInformation("Created category {Name}", category.Name);
        return new CategoryDto { Id = category.Id, Name = category.Name };
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            UnitPrice = dto.UnitPrice,
            CategoryId = dto.CategoryId
        };
        await _productRepository.AddAsync(product);
        _logger.LogInformation("Created product {Name}", product.Name);
        return new ProductDto { Id = product.Id, Name = product.Name, UnitPrice = product.UnitPrice };
    }

    public async Task<ProductDto> UpdatePriceAsync(UpdatePriceDto dto)
    {
        var product = await _productRepository.GetByIdAsync(dto.ProductId);
        if (product == null) throw new Exception("Product not found");
        product.UnitPrice = dto.UnitPrice;
        await _productRepository.UpdateAsync(product);
        _logger.LogInformation("Updated price for {ProductId}", dto.ProductId);
        return new ProductDto { Id = product.Id, Name = product.Name, UnitPrice = product.UnitPrice };
    }
    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync()
    {
        var products = await _productRepository.GetAllAsync();
        return products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Sku = p.Sku,
            Barcode = p.Barcode,
            UnitPrice = p.UnitPrice,
            LocalStockQuantity = p.LocalStockQuantity
        }).ToList();
    }
}


using Microsoft.Extensions.Logging;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Catalog;
using SmartPOS.Shared.DTOs.Product;
using SmartPOS.Shared.Exceptions;

namespace SmartPOS.Services.Implementations;

public class CatalogService : ICatalogService
{
    private readonly ILogger<CatalogService> _logger;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAuditService _auditService;

    public CatalogService(
        ILogger<CatalogService> logger,
        ICategoryRepository categoryRepository,
        IProductRepository productRepository,
        IAuditService auditService)
    {
        _logger = logger;
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _auditService = auditService;
    }

    // ── Category ─────────────────────────────────────────────────────────────

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
        _logger.LogInformation("Đã tạo danh mục {Name}", category.Name);
        return new CategoryDto { Id = category.Id, Name = category.Name };
    }

    // ── Product ───────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync()
    {
        var products = await _productRepository.GetAllAsync();
        return products.Select(MapToDto).ToList();
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
    {
        // Validate SKU duy nhất
        if (!string.IsNullOrWhiteSpace(dto.Sku))
        {
            var existingBySku = await _productRepository.GetBySkuAsync(dto.Sku);
            if (existingBySku != null)
                throw new BusinessException($"SKU '{dto.Sku}' đã tồn tại trong hệ thống.");
        }

        // Validate Barcode duy nhất
        if (!string.IsNullOrWhiteSpace(dto.Barcode))
        {
            var existingByBarcode = await _productRepository.GetByBarcodeAsync(dto.Barcode);
            if (existingByBarcode != null)
                throw new BusinessException($"Barcode '{dto.Barcode}' đã tồn tại trong hệ thống.");
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = dto.CategoryId,
            Name = dto.Name,
            Sku = dto.Sku,
            Barcode = dto.Barcode,
            QrCode = dto.QrCode,
            UnitPrice = dto.UnitPrice,
            IsActive = true,
            LocalStockQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _productRepository.AddAsync(product);
        _logger.LogInformation("Đã tạo sản phẩm {Name} (SKU: {Sku})", product.Name, product.Sku);

        return MapToDto(product);
    }

    public async Task<ProductDto> UpdatePriceAsync(UpdatePriceDto dto, Guid userId)
    {
        var product = await _productRepository.GetByIdAsync(dto.ProductId)
            ?? throw new BusinessException("Sản phẩm không tồn tại.");

        if (!product.IsActive)
            throw new BusinessException("Không thể cập nhật giá cho sản phẩm đã ngừng kinh doanh.");

        if (dto.UnitPrice < 0)
            throw new BusinessException("Giá sản phẩm không được âm.");

        var oldPrice = product.UnitPrice;
        product.UnitPrice = dto.UnitPrice;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product);
        _logger.LogInformation("Đã cập nhật giá sản phẩm {ProductId}: {OldPrice} → {NewPrice}", dto.ProductId, oldPrice, dto.UnitPrice);

        await _auditService.LogAsync(
            action: "UPDATE_PRICE",
            entity: "Product",
            entityId: product.Id,
            oldValue: new { UnitPrice = oldPrice },
            newValue: new { UnitPrice = dto.UnitPrice },
            userId: userId);

        return MapToDto(product);
    }

    public async Task<ProductDto> DeactivateProductAsync(Guid productId, Guid userId)
    {
        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new BusinessException("Sản phẩm không tồn tại.");

        if (!product.IsActive)
            throw new BusinessException("Sản phẩm đã được ngừng kinh doanh trước đó.");

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product);
        _logger.LogInformation("Đã ngừng kinh doanh sản phẩm {ProductId} ({Name})", product.Id, product.Name);

        await _auditService.LogAsync(
            action: "DEACTIVATE_PRODUCT",
            entity: "Product",
            entityId: product.Id,
            oldValue: new { IsActive = true },
            newValue: new { IsActive = false },
            userId: userId);

        return MapToDto(product);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ProductDto MapToDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Sku = p.Sku,
        Barcode = p.Barcode,
        UnitPrice = p.UnitPrice,
        LocalStockQuantity = p.LocalStockQuantity
    };
}

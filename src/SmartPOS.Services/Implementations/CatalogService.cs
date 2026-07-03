using Microsoft.Extensions.Logging;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Catalog;
using SmartPOS.Shared.DTOs.Inventory;
using SmartPOS.Shared.DTOs.Product;
using SmartPOS.Shared.Exceptions;

namespace SmartPOS.Services.Implementations;

public class CatalogService : ICatalogService
{
    private readonly ILogger<CatalogService> _logger;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAuditService _auditService;
    private readonly IInventorySyncService _inventorySyncService;

    public CatalogService(
        ILogger<CatalogService> logger,
        ICategoryRepository categoryRepository,
        IProductRepository productRepository,
        IAuditService auditService,
        IInventorySyncService inventorySyncService)
    {
        _logger = logger;
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _auditService = auditService;
        _inventorySyncService = inventorySyncService;
    }

    // ── Category ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description
        }).ToList();
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new BusinessException("Tên danh mục không được để trống.");

        var existing = await _categoryRepository.GetByNameAsync(dto.Name);
        if (existing != null)
            throw new BusinessException($"Danh mục '{dto.Name}' đã tồn tại trong hệ thống.");

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description,
            IsActive = true
        };
        await _categoryRepository.AddAsync(category);
        _logger.LogInformation("Đã tạo danh mục {Name}", category.Name);

        await _auditService.LogAsync(
            action: "CREATE_CATEGORY",
            entity: "Category",
            entityId: category.Id,
            oldValue: (object?)null,
            newValue: new { category.Name, category.Description },
            userId: userId);

        return new CategoryDto { Id = category.Id, Name = category.Name, Description = category.Description };
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, string name, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessException("Tên danh mục không được để trống.");

        var category = await _categoryRepository.GetByIdAsync(id)
            ?? throw new BusinessException("Danh mục không tồn tại.");

        var existing = await _categoryRepository.GetByNameAsync(name);
        if (existing != null && existing.Id != id)
            throw new BusinessException($"Danh mục '{name}' đã tồn tại trong hệ thống.");

        var oldName = category.Name;
        category.Name = name.Trim();
        await _categoryRepository.UpdateAsync(category);
        _logger.LogInformation("Đã cập nhật danh mục {Id}: {Old} → {New}", id, oldName, name);

        await _auditService.LogAsync(
            action: "UPDATE_CATEGORY",
            entity: "Category",
            entityId: category.Id,
            oldValue: new { Name = oldName },
            newValue: new { Name = name },
            userId: userId);

        return new CategoryDto { Id = category.Id, Name = category.Name, Description = category.Description };
    }

    // ── Product ───────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync()
    {
        var products = await _productRepository.GetAllAsync();
        var categories = await _categoryRepository.GetAllAsync();
        var catMap = categories.ToDictionary(c => c.Id, c => c.Name);
        return products.Select(p => MapToDto(p, catMap)).ToList();
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new BusinessException("Tên sản phẩm không được để trống.");

        // Validate CategoryId tồn tại và còn active
        if (dto.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(dto.CategoryId.Value)
                ?? throw new BusinessException("Danh mục không tồn tại.");
            if (!category.IsActive)
                throw new BusinessException("Danh mục đã ngừng hoạt động, không thể thêm sản phẩm vào.");
        }

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

        // Validate QrCode duy nhất
        if (!string.IsNullOrWhiteSpace(dto.QrCode))
        {
            var existingByQr = await _productRepository.GetByQrCodeAsync(dto.QrCode);
            if (existingByQr != null)
                throw new BusinessException($"QR Code '{dto.QrCode}' đã tồn tại trong hệ thống.");
        }

        // ── Đăng ký sản phẩm lên Inventory API để lấy ExternalInventoryId ────
        string? externalInventoryId = null;
        try
        {
            var invDto = new RegisterInventoryProductDto
            {
                Name = dto.Name.Trim(),
                Sku = dto.Sku ?? string.Empty,
                Barcode = dto.Barcode,
                QrCode = dto.QrCode,
                UnitPrice = dto.UnitPrice,
                InitialStock = dto.InitialStock
                // CategoryId của Inventory khác CategoryId của POS → không map trực tiếp
            };

            var invResult = await _inventorySyncService.RegisterProductToInventoryAsync(invDto);
            if (invResult != null)
            {
                externalInventoryId = invResult.Id.ToString();
                _logger.LogInformation(
                    "Đã đăng ký sản phẩm '{Name}' lên Inventory API, ExternalInventoryId = {Id}",
                    dto.Name, externalInventoryId);
            }
            else
            {
                _logger.LogWarning(
                    "Không thể đăng ký sản phẩm '{Name}' lên Inventory API. " +
                    "Sản phẩm vẫn được tạo trong POS nhưng chưa có ExternalInventoryId. " +
                    "Hãy chạy Sync Catalog sau khi Inventory API hoạt động trở lại.",
                    dto.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đăng ký sản phẩm '{Name}' lên Inventory API", dto.Name);
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = dto.CategoryId,
            Name = dto.Name.Trim(),
            Sku = dto.Sku ?? string.Empty,
            Barcode = dto.Barcode,
            QrCode = dto.QrCode,
            UnitPrice = dto.UnitPrice,
            IsActive = true,
            ExternalInventoryId = externalInventoryId,
            LocalStockQuantity = dto.InitialStock,
            ImagePath = dto.ImagePath,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _productRepository.AddAsync(product);
        _logger.LogInformation("Đã tạo sản phẩm {Name} (SKU: {Sku}, ExternalId: {ExternalId})",
            product.Name, product.Sku, product.ExternalInventoryId ?? "null");

        await _auditService.LogAsync(
            action: "CREATE_PRODUCT",
            entity: "Product",
            entityId: product.Id,
            oldValue: (object?)null,
            newValue: new { product.CategoryId, product.Name, product.Sku, product.Barcode, product.UnitPrice, product.ExternalInventoryId },
            userId: userId);

        return MapToDto(product);
    }


    public async Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductDto dto, Guid userId)
    {
        var product = await _productRepository.GetByIdAsync(id)
            ?? throw new BusinessException("Sản phẩm không tồn tại.");

        if (!product.IsActive)
            throw new BusinessException("Không thể cập nhật sản phẩm đã ngừng kinh doanh.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new BusinessException("Tên sản phẩm không được để trống.");

        // Validate CategoryId
        if (dto.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(dto.CategoryId.Value)
                ?? throw new BusinessException("Danh mục không tồn tại.");
            if (!category.IsActive)
                throw new BusinessException("Danh mục đã ngừng hoạt động.");
        }

        // Validate SKU unique (excluding self)
        if (!string.IsNullOrWhiteSpace(dto.Sku))
        {
            var bySku = await _productRepository.GetBySkuAsync(dto.Sku);
            if (bySku != null && bySku.Id != id)
                throw new BusinessException($"SKU '{dto.Sku}' đã được sử dụng bởi sản phẩm khác.");
        }

        // Validate Barcode unique (excluding self)
        if (!string.IsNullOrWhiteSpace(dto.Barcode))
        {
            var byBarcode = await _productRepository.GetByBarcodeAsync(dto.Barcode);
            if (byBarcode != null && byBarcode.Id != id)
                throw new BusinessException($"Barcode '{dto.Barcode}' đã được sử dụng bởi sản phẩm khác.");
        }

        // Validate QrCode unique (excluding self)
        if (!string.IsNullOrWhiteSpace(dto.QrCode))
        {
            var byQr = await _productRepository.GetByQrCodeAsync(dto.QrCode);
            if (byQr != null && byQr.Id != id)
                throw new BusinessException($"QR Code '{dto.QrCode}' đã được sử dụng bởi sản phẩm khác.");
        }

        var oldSnapshot = new { product.CategoryId, product.Name, product.Sku, product.Barcode, product.QrCode, product.UnitPrice, product.TaxRate };

        product.CategoryId = dto.CategoryId;
        product.Name = dto.Name.Trim();
        product.Sku = dto.Sku;
        product.Barcode = dto.Barcode;
        product.QrCode = dto.QrCode;
        product.Description = dto.Description;
        product.UnitPrice = dto.UnitPrice;
        product.TaxRate = dto.TaxRate;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product);
        _logger.LogInformation("Đã cập nhật sản phẩm {ProductId} ({Name})", product.Id, product.Name);

        await _auditService.LogAsync(
            action: "UPDATE_PRODUCT",
            entity: "Product",
            entityId: product.Id,
            oldValue: oldSnapshot,
            newValue: new { product.CategoryId, product.Name, product.Sku, product.Barcode, product.QrCode, product.UnitPrice, product.TaxRate },
            userId: userId);

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

        if (!string.IsNullOrEmpty(product.ExternalInventoryId) && Guid.TryParse(product.ExternalInventoryId, out var invId))
        {
            var updateDto = new SmartPOS.Shared.DTOs.Inventory.UpdateInventoryProductDto(
                product.Name,
                product.Sku,
                product.Barcode,
                product.QrCode,
                product.Description,
                product.UnitPrice,
                product.TaxRate,
                product.CategoryId
            );
            await _inventorySyncService.UpdateProductInInventoryAsync(invId, updateDto);
        }

        await _auditService.LogAsync(
            action: "UPDATE_PRICE",
            entity: "Product",
            entityId: product.Id,
            oldValue: new { UnitPrice = oldPrice },
            newValue: new { UnitPrice = dto.UnitPrice },
            userId: userId);

        return MapToDto(product);
    }

    public async Task<ProductDto> UpdateStockAsync(Guid productId, int newStock, Guid userId)
    {
        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new BusinessException("Sản phẩm không tồn tại.");

        if (newStock < 0)
            throw new BusinessException("Số lượng tồn kho không được âm.");

        var oldStock = product.LocalStockQuantity;
        var diff = newStock - oldStock;

        if (diff != 0)
        {
            product.LocalStockQuantity = newStock;
            product.UpdatedAt = DateTime.UtcNow;

            await _productRepository.UpdateAsync(product);
            _logger.LogInformation("Đã cập nhật tồn kho sản phẩm {ProductId}: {OldStock} → {NewStock}", productId, oldStock, newStock);

            if (!string.IsNullOrEmpty(product.ExternalInventoryId) && Guid.TryParse(product.ExternalInventoryId, out var invId))
            {
                await _inventorySyncService.AdjustStockAsync(invId, diff, "Cập nhật thủ công từ POS");
            }

            await _auditService.LogAsync(
                action: "UPDATE_STOCK",
                entity: "Product",
                entityId: product.Id,
                oldValue: new { LocalStockQuantity = oldStock },
                newValue: new { LocalStockQuantity = newStock },
                userId: userId);
        }

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

    public async Task<ProductDto> ReactivateProductAsync(Guid productId, Guid userId)
    {
        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new BusinessException("Sản phẩm không tồn tại.");

        if (product.IsActive)
            throw new BusinessException("Sản phẩm đang được kinh doanh.");

        product.IsActive = true;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product);
        _logger.LogInformation("Đã kinh doanh lại sản phẩm {ProductId} ({Name})", product.Id, product.Name);

        await _auditService.LogAsync(
            action: "REACTIVATE_PRODUCT",
            entity: "Product",
            entityId: product.Id,
            oldValue: new { IsActive = false },
            newValue: new { IsActive = true },
            userId: userId);

        return MapToDto(product);
    }

    public async Task DeleteProductAsync(Guid productId, Guid userId)
    {
        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new BusinessException("Sản phẩm không tồn tại.");

        var productName = product.Name;
        await _productRepository.DeleteAsync(product);
        _logger.LogInformation("Đã xóa sản phẩm {ProductId} ({Name})", productId, productName);

        if (!string.IsNullOrEmpty(product.ExternalInventoryId) && Guid.TryParse(product.ExternalInventoryId, out var invId))
        {
            await _inventorySyncService.DeleteProductFromInventoryAsync(invId);
        }

        await _auditService.LogAsync(
            action: "DELETE_PRODUCT",
            entity: "Product",
            entityId: productId,
            oldValue: new { Name = productName },
            newValue: (object?)null,
            userId: userId);
    }

    public async Task<ProductDto> UpdateProductImageAsync(Guid productId, string? imagePath, Guid userId)
    {
        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new BusinessException("Sản phẩm không tồn tại.");

        var oldImagePath = product.ImagePath;
        product.ImagePath = imagePath;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product);
        _logger.LogInformation("Đã cập nhật hình ảnh sản phẩm {ProductId} ({Name})", product.Id, product.Name);

        await _auditService.LogAsync(
            action: "UPDATE_PRODUCT_IMAGE",
            entity: "Product",
            entityId: product.Id,
            oldValue: new { ImagePath = oldImagePath },
            newValue: new { ImagePath = imagePath },
            userId: userId);

        return MapToDto(product);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ProductDto MapToDto(Product p, Dictionary<Guid, string>? catMap = null) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Sku = p.Sku,
        Barcode = p.Barcode,
        UnitPrice = p.UnitPrice,
        LocalStockQuantity = p.LocalStockQuantity,
        IsActive = p.IsActive,
        TaxRate = p.TaxRate,
        ImagePath = p.ImagePath,
        CategoryName = catMap != null && p.CategoryId.HasValue && catMap.TryGetValue(p.CategoryId.Value, out var name) ? name : string.Empty
    };
}

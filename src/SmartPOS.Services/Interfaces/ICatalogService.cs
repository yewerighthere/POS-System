using SmartPOS.Shared.DTOs.Catalog;
using SmartPOS.Shared.DTOs.Product;

namespace SmartPOS.Services.Interfaces;

public interface ICatalogService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync();
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto, Guid userId);
    Task<CategoryDto> UpdateCategoryAsync(Guid id, string name, Guid userId);

    Task<IReadOnlyList<ProductDto>> GetProductsAsync();
    Task<ProductDto> CreateProductAsync(CreateProductDto dto, Guid userId);
    Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductDto dto, Guid userId);
    Task<ProductDto> UpdatePriceAsync(UpdatePriceDto dto, Guid userId);
    Task<ProductDto> DeactivateProductAsync(Guid productId, Guid userId);
    Task<ProductDto> ReactivateProductAsync(Guid productId, Guid userId);
    Task<ProductDto> UpdateProductImageAsync(Guid productId, string? imagePath, Guid userId);
}

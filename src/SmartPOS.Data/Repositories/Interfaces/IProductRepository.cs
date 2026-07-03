using SmartPOS.Data.Entities;

namespace SmartPOS.Data.Repositories.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product?> GetBySkuAsync(string sku);
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<Product?> GetByQrCodeAsync(string qrCode);
    Task<Product?> GetByExternalIdAsync(string externalId);
    Task<IReadOnlyList<Product>> SearchAsync(string keyword, bool includeInactive = false);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Product product);
    Task<IReadOnlyList<Product>> GetAllAsync();
}

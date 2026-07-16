using Microsoft.EntityFrameworkCore;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;

namespace SmartPOS.Data.Repositories.Implementations;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product?> GetByBarcodeAsync(string barcode)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.Barcode == barcode);
    }

    public async Task<Product?> GetBySkuAsync(string sku)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.Sku == sku);
    }

    public async Task<Product?> GetByQrCodeAsync(string qrCode)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.QrCode == qrCode);
    }

    public async Task<Product?> GetByExternalIdAsync(string externalId)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.ExternalInventoryId == externalId);
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(string keyword, bool includeInactive = false)
    {
        var query = _context.Products.AsQueryable();

        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(p =>
                p.Name.Contains(keyword) ||
                p.Sku.Contains(keyword) ||
                (p.Barcode != null && p.Barcode.Contains(keyword)) ||
                (p.QrCode != null && p.QrCode.Contains(keyword)));

        return await query.ToListAsync();
    }

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Product product)
    {
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync()
    {
        return await _context.Products
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}

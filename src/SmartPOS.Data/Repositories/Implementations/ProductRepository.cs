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
        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id)
            .ConfigureAwait(false);
    }

    public async Task<Product?> GetByBarcodeAsync(string barcode)
    {
        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive)
            .ConfigureAwait(false);
    }

    public async Task<Product?> GetByExternalIdAsync(string externalId)
    {
        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ExternalInventoryId == externalId)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            var allProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .ToListAsync()
                .ConfigureAwait(false);
            return allProducts;
        }

        var normalizedKeyword = keyword.Trim().ToLower();

        var filteredProducts = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && 
                       (p.Name.ToLower().Contains(normalizedKeyword) || 
                        p.Sku.ToLower().Contains(normalizedKeyword) || 
                        (p.Barcode != null && p.Barcode.ToLower().Contains(normalizedKeyword))))
            .ToListAsync()
            .ConfigureAwait(false);
        return filteredProducts;
    }

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}


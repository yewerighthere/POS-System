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

    public Task<Product?> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<Product?> GetByBarcodeAsync(string barcode)
    {
        throw new NotImplementedException();
    }

    public Task<Product?> GetByExternalIdAsync(string externalId)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Product>> SearchAsync(string keyword)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Product product)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Product product)
    {
        throw new NotImplementedException();
    }
}


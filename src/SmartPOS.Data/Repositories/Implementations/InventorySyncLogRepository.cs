using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;

namespace SmartPOS.Data.Repositories.Implementations;

public class InventorySyncLogRepository : IInventorySyncLogRepository
{
    private readonly AppDbContext _context;

    public InventorySyncLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(InventorySyncLog log)
    {
        throw new NotImplementedException();
    }

    public Task<InventorySyncLog?> GetLatestAsync(string syncType)
    {
        throw new NotImplementedException();
    }
}


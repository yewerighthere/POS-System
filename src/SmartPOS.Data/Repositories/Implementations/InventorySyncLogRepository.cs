using Microsoft.EntityFrameworkCore;
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

    public async Task AddAsync(InventorySyncLog log)
    {
        await _context.InventorySyncLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    public async Task<InventorySyncLog?> GetLatestAsync(string syncType)
    {
        return await _context.InventorySyncLogs
            .Where(l => l.SyncType == syncType)
            .OrderByDescending(l => l.SyncedAt)
            .FirstOrDefaultAsync();
    }
}

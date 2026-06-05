using SmartPOS.Data.Entities;

namespace SmartPOS.Data.Repositories.Interfaces;

public interface IInventorySyncLogRepository
{
    Task AddAsync(InventorySyncLog log); Task<InventorySyncLog?> GetLatestAsync(string syncType);
}


using SmartPOS.Data.Entities;

namespace SmartPOS.Data.Repositories.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log); Task<IReadOnlyList<AuditLog>> GetByEntityAsync(string entity, Guid entityId); Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count);
}


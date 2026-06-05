using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;

namespace SmartPOS.Data.Repositories.Implementations;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(AuditLog log)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<AuditLog>> GetByEntityAsync(string entity, Guid entityId)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count)
    {
        throw new NotImplementedException();
    }
}


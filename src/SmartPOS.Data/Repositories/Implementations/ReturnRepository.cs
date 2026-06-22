using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;

namespace SmartPOS.Data.Repositories.Implementations;

public class ReturnRepository : IReturnRepository
{
    private readonly AppDbContext _context;

    public ReturnRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Return?> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Return>> GetByOrderIdAsync(Guid orderId)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Return returnEntity)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Return returnEntity)
    {
        throw new NotImplementedException();
    }
}


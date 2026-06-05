using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;

namespace SmartPOS.Data.Repositories.Implementations;

public class ShiftRepository : IShiftRepository
{
    private readonly AppDbContext _context;

    public ShiftRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Shift?> GetOpenShiftAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<Shift?> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Shift shift)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Shift shift)
    {
        throw new NotImplementedException();
    }
}


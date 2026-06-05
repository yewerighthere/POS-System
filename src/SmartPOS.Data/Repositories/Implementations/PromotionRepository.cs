using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;

namespace SmartPOS.Data.Repositories.Implementations;

public class PromotionRepository : IPromotionRepository
{
    private readonly AppDbContext _context;

    public PromotionRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Promotion?> GetByCodeAsync(string code)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Promotion>> GetActiveAsync(DateOnly date)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Promotion promotion)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Promotion promotion)
    {
        throw new NotImplementedException();
    }
}


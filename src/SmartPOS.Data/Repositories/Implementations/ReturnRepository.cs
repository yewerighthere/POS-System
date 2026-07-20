using Microsoft.EntityFrameworkCore;
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

    public async Task<Return?> GetByIdAsync(Guid id)
    {
        return await _context.Returns
            .Include(r => r.Items)
                .ThenInclude(i => i.OrderItem)
                    .ThenInclude(oi => oi.Product)
            .Include(r => r.Order)
            .Include(r => r.RequestedByUser)
            .FirstOrDefaultAsync(r => r.Id == id)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Return>> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.Returns
            .Include(r => r.Items)
                .ThenInclude(i => i.OrderItem)
                    .ThenInclude(oi => oi.Product)
            .Include(r => r.RequestedByUser)
            .Where(r => r.OrderId == orderId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Return>> GetAllAsync()
    {
        return await _context.Returns
            .Include(r => r.Items)
                .ThenInclude(i => i.OrderItem)
                    .ThenInclude(oi => oi.Product)
            .Include(r => r.Order)
            .Include(r => r.RequestedByUser)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Return returnEntity)
    {
        await _context.Returns.AddAsync(returnEntity).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(Return returnEntity)
    {
        _context.Returns.Update(returnEntity);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}


using Microsoft.EntityFrameworkCore;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;

namespace SmartPOS.Data.Repositories.Implementations;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == id)
            .ConfigureAwait(false);
    }

    public async Task<Order?> GetByIdWithItemsAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == id)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task AddPaymentAsync(Order order, Payment payment)
    {
        _context.Orders.Update(order);
        await _context.Payments.AddAsync(payment).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}

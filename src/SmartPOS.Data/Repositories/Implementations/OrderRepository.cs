using Microsoft.EntityFrameworkCore;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Shared.DTOs.Report;
using SmartPOS.Shared.Enums;

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

    public async Task<PaymentStatus?> GetPaymentStatusAsync(Guid id)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => (PaymentStatus?)o.PaymentStatus)
            .FirstOrDefaultAsync()
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

    public async Task<int> GetOrderCountByShiftAsync(Guid shiftId)
        => await _context.Orders
            .CountAsync(o => o.ShiftId == shiftId && o.Status == OrderStatus.Confirmed)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Order>> GetOrdersByShiftAsync(Guid shiftId)
        => await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.User)
            .Where(o => o.ShiftId == shiftId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<TopProductDto>> GetTopProductsByShiftAsync(Guid shiftId, int count)
        => await _context.OrderItems
            .Where(oi => oi.Order.ShiftId == shiftId && oi.Order.Status == OrderStatus.Confirmed)
            .GroupBy(oi => oi.ProductName)
            .Select(g => new TopProductDto { ProductName = g.Key, TotalSold = g.Sum(oi => oi.Quantity) })
            .OrderByDescending(x => x.TotalSold)
            .Take(count)
            .ToListAsync()
            .ConfigureAwait(false);
}

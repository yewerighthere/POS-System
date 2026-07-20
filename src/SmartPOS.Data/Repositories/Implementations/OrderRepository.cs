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

    public async Task<IReadOnlyList<Order>> GetOrdersByDateRangeAsync(DateTime fromDate, DateTime toDate, Guid? staffId = null, Guid? shiftId = null, PaymentMethod? paymentMethod = null)
    {
        var start = DateTime.SpecifyKind(fromDate.Date, DateTimeKind.Utc);
        var end   = DateTime.SpecifyKind(toDate.Date.AddDays(1), DateTimeKind.Utc);
        var query = _context.Orders.Where(o => o.CreatedAt >= start && o.CreatedAt < end);
        if (staffId.HasValue)       query = query.Where(o => o.UserId == staffId.Value);
        if (shiftId.HasValue)       query = query.Where(o => o.ShiftId == shiftId.Value);
        if (paymentMethod.HasValue) query = query.Where(o => o.PaymentMethod == paymentMethod.Value);
        return await query
            .Include(o => o.Items)
            .Include(o => o.User)
            .OrderBy(o => o.CreatedAt)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TopProductDto>> GetTopProductsByDateRangeAsync(DateTime fromDate, DateTime toDate, int count = 10, Guid? staffId = null, Guid? shiftId = null, PaymentMethod? paymentMethod = null)
    {
        var start = DateTime.SpecifyKind(fromDate.Date, DateTimeKind.Utc);
        var end   = DateTime.SpecifyKind(toDate.Date.AddDays(1), DateTimeKind.Utc);
        var query = _context.OrderItems
            .Where(oi => oi.Order.CreatedAt >= start && oi.Order.CreatedAt < end
                      && oi.Order.Status == OrderStatus.Confirmed);
        if (staffId.HasValue)       query = query.Where(oi => oi.Order.UserId == staffId.Value);
        if (shiftId.HasValue)       query = query.Where(oi => oi.Order.ShiftId == shiftId.Value);
        if (paymentMethod.HasValue) query = query.Where(oi => oi.Order.PaymentMethod == paymentMethod.Value);
        return await query
            .GroupBy(oi => oi.ProductName)
            .Select(g => new TopProductDto { ProductName = g.Key, TotalSold = g.Sum(oi => oi.Quantity) })
            .OrderByDescending(x => x.TotalSold)
            .Take(count)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }
}

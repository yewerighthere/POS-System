using Microsoft.EntityFrameworkCore;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Shared.Enums;
using SmartPOS.Data;

namespace SmartPOS.Data.Repositories.Implementations;

public class ShiftRepository : IShiftRepository
{
    private readonly AppDbContext _context;

    public ShiftRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Shift?> GetOpenShiftAsync(Guid userId)
        => await _context.Shifts
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == ShiftStatus.Open)
            .ConfigureAwait(false);

    public async Task<Shift?> GetByIdAsync(Guid id)
        => await _context.Shifts.FindAsync(id).ConfigureAwait(false);

    public async Task AddAsync(Shift shift)
    {
        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(Shift shift)
    {
        _context.Shifts.Update(shift);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<decimal> GetCashRevenueAsync(Guid shiftId)
        => await _context.Orders
            .Where(o => o.ShiftId == shiftId
                && o.PaymentMethod == PaymentMethod.Cash
                && o.PaymentStatus == PaymentStatus.Success)
            .SumAsync(o => o.TotalAmount)
            .ConfigureAwait(false);

    public async Task<decimal> GetTotalSalesAsync(Guid shiftId)
        => await _context.Orders
            .Where(o => o.ShiftId == shiftId && o.Status == OrderStatus.Confirmed)
            .SumAsync(o => o.TotalAmount)
            .ConfigureAwait(false);
}


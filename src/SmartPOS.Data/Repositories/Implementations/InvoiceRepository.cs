using Microsoft.EntityFrameworkCore;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;

namespace SmartPOS.Data.Repositories.Implementations;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _context;

    public InvoiceRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Invoice?> GetByOrderIdAsync(Guid orderId)
    {
        return _context.Invoices.FirstOrDefaultAsync(invoice => invoice.OrderId == orderId);
    }

    public Task<Invoice?> GetByIdAsync(Guid invoiceId)
    {
        return _context.Invoices.FirstOrDefaultAsync(invoice => invoice.Id == invoiceId);
    }

    public Task<int> GetDailySequenceAsync(DateOnly date)
    {
        var start = date.ToDateTime(TimeOnly.MinValue);
        var end = start.AddDays(1);
        return _context.Invoices.CountAsync(invoice => invoice.IssuedAt >= start && invoice.IssuedAt < end);
    }

    public Task AddAsync(Invoice invoice)
    {
        _context.Invoices.Add(invoice);
        return _context.SaveChangesAsync();
    }
}

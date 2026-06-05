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
        throw new NotImplementedException();
    }

    public Task<int> GetDailySequenceAsync(DateOnly date)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Invoice invoice)
    {
        throw new NotImplementedException();
    }
}


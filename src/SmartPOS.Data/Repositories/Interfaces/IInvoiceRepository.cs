using SmartPOS.Data.Entities;

namespace SmartPOS.Data.Repositories.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid invoiceId);
    Task<Invoice?> GetByOrderIdAsync(Guid orderId);
    Task<int> GetDailySequenceAsync(DateTime startUtc, DateTime endUtc);
    Task AddAsync(Invoice invoice);
}


using SmartPOS.Data.Entities;

namespace SmartPOS.Data.Repositories.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByOrderIdAsync(Guid orderId); Task<int> GetDailySequenceAsync(DateOnly date); Task AddAsync(Invoice invoice);
}


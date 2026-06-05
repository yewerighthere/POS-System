using SmartPOS.Data.Entities;

namespace SmartPOS.Data.Repositories.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id); Task<Order?> GetByIdWithItemsAsync(Guid id); Task AddAsync(Order order); Task UpdateAsync(Order order); Task AddPaymentAsync(Order order, Payment payment);
}


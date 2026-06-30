using SmartPOS.Data.Entities;
using SmartPOS.Shared.DTOs.Report;
using SmartPOS.Shared.Enums;

namespace SmartPOS.Data.Repositories.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<Order?> GetByIdWithItemsAsync(Guid id);
    Task<PaymentStatus?> GetPaymentStatusAsync(Guid id);
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task AddPaymentAsync(Order order, Payment payment);
    Task<int> GetOrderCountByShiftAsync(Guid shiftId);
    Task<IReadOnlyList<Order>> GetOrdersByShiftAsync(Guid shiftId);
    Task<IReadOnlyList<TopProductDto>> GetTopProductsByShiftAsync(Guid shiftId, int count);
}

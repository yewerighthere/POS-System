using SmartPOS.Data.Entities;

namespace SmartPOS.Data.Repositories.Interfaces;

public interface IReturnRepository
{
    Task<Return?> GetByIdAsync(Guid id); Task<IReadOnlyList<Return>> GetByOrderIdAsync(Guid orderId); Task AddAsync(Return returnEntity); Task UpdateAsync(Return returnEntity);
}


using SmartPOS.Data.Entities;

namespace SmartPOS.Data.Repositories.Interfaces;

public interface IShiftRepository
{
    Task<Shift?> GetOpenShiftAsync(Guid userId); Task<Shift?> GetByIdAsync(Guid id); Task AddAsync(Shift shift); Task UpdateAsync(Shift shift);
}


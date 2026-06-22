using SmartPOS.Data.Entities;

namespace SmartPOS.Data.Repositories.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(); Task<Category?> GetByIdAsync(Guid id); Task AddAsync(Category category); Task UpdateAsync(Category category);
}


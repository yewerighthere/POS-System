using SmartPOS.Data.Entities;

namespace SmartPOS.Data.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(Guid id);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task<System.Collections.Generic.IReadOnlyList<User>> GetAllAsync();
}


using SmartPOS.Data.Entities;

namespace SmartPOS.Data.Repositories.Interfaces;

public interface IUserSessionRepository
{
    Task<UserSession?> GetByIdAsync(Guid id);
    Task<UserSession?> GetActiveByUserIdAsync(Guid userId);
    Task AddAsync(UserSession session);
    Task UpdateLogoutAsync(UserSession session);
}


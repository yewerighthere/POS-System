using SmartPOS.Data.Entities;

namespace SmartPOS.Data.Repositories.Interfaces;

public interface IUserSessionRepository
{
    Task AddAsync(UserSession session); Task UpdateLogoutAsync(UserSession session);
}


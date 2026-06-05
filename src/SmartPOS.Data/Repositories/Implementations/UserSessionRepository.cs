using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;

namespace SmartPOS.Data.Repositories.Implementations;

public class UserSessionRepository : IUserSessionRepository
{
    private readonly AppDbContext _context;

    public UserSessionRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(UserSession session)
    {
        throw new NotImplementedException();
    }

    public Task UpdateLogoutAsync(UserSession session)
    {
        throw new NotImplementedException();
    }
}


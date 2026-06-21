using Microsoft.EntityFrameworkCore;
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

    public async Task<UserSession?> GetByIdAsync(Guid id)
    {
        return await _context.UserSessions
            .Include(session => session.User)
            .FirstOrDefaultAsync(session => session.Id == id)
            .ConfigureAwait(false);
    }

    public async Task<UserSession?> GetActiveByUserIdAsync(Guid userId)
    {
        return await _context.UserSessions
            .Include(session => session.User)
            .FirstOrDefaultAsync(session => session.UserId == userId && session.LogoutAt == null)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(UserSession session)
    {
        await _context.UserSessions.AddAsync(session).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateLogoutAsync(UserSession session)
    {
        _context.UserSessions.Update(session);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}

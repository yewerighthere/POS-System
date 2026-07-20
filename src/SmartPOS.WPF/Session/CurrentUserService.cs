using SmartPOS.Shared.Interfaces;
using System;

namespace SmartPOS.WPF.Session;

public class CurrentUserService : ICurrentUserService
{
    private readonly CurrentSessionContext _sessionContext;

    public CurrentUserService(CurrentSessionContext sessionContext)
    {
        _sessionContext = sessionContext;
    }

    public Guid? GetCurrentUserId()
    {
        return _sessionContext.CurrentUser?.UserId;
    }
}

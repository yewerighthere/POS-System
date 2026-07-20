using System;

namespace SmartPOS.Shared.Interfaces;

public interface ICurrentUserService
{
    Guid? GetCurrentUserId();
}

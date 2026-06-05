using SmartPOS.Shared.DTOs.Auth;
using SmartPOS.Shared.DTOs.Shift;

namespace SmartPOS.WPF.Session;

public class CurrentSessionContext
{
    public UserSessionDto? CurrentUser { get; set; }
    public ShiftDto? CurrentShift { get; set; }
    public bool IsAuthenticated => CurrentUser is not null;
    public bool HasOpenShift => CurrentShift is not null;

    public Guid RequireUserId()
    {
        throw new NotImplementedException();
    }
}

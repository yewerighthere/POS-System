using SmartPOS.Shared.DTOs.Auth;
using SmartPOS.Shared.DTOs.Cart;
using SmartPOS.Shared.DTOs.Shift;
using SmartPOS.Shared.Exceptions;

namespace SmartPOS.WPF.Session;

public class CurrentSessionContext
{
    public UserSessionDto? CurrentUser { get; set; }
    public string CurrentToken { get; set; } = string.Empty;
    public ShiftDto? CurrentShift { get; set; }
    public CartSummaryDto? CurrentCart { get; set; }
    public Guid? PendingOrderId { get; set; }
    public Guid? LastPaidOrderId { get; set; }
    public Guid? LastInvoiceId { get; set; }
    public bool IsAuthenticated => CurrentUser is not null;
    public bool HasOpenShift => CurrentShift is not null;

    public Guid RequireUserId()
    {
        return CurrentUser?.UserId
            ?? throw new BusinessException("Bạn không có quyền truy cập chức năng này");
    }
}

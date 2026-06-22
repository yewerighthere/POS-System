using SmartPOS.Shared.Enums;

namespace SmartPOS.Data.Entities;

public class UserSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime LoginAt { get; set; }
    public DateTime? LogoutAt { get; set; }
    public string? IpAddress { get; set; }
}


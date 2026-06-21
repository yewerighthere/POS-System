using SmartPOS.Shared.Enums;

namespace SmartPOS.Data.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty; 
    public string PasswordHash { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string Email { get; set;  } = string.Empty;

    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}


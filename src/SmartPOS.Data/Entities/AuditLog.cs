using SmartPOS.Shared.Enums;

namespace SmartPOS.Data.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; } public User User { get; set; } = null!; public string Action { get; set; } = string.Empty; public string? Entity { get; set; } public Guid? EntityId { get; set; } public string? OldValue { get; set; } public string? NewValue { get; set; } public DateTime CreatedAt { get; set; }
}


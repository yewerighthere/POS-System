using SmartPOS.Shared.Enums;

namespace SmartPOS.Data.Entities;

public class DeviceLog
{
    public Guid Id { get; set; }
    public Guid? DeviceId { get; set; } public Device? Device { get; set; } public string EventType { get; set; } = string.Empty; public string? Message { get; set; } public DateTime CreatedAt { get; set; }
}


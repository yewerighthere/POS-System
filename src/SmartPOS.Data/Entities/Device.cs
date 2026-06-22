using SmartPOS.Shared.Enums;

namespace SmartPOS.Data.Entities;

public class Device
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; public string DeviceType { get; set; } = string.Empty; public string ConnectionType { get; set; } = string.Empty; public string? SerialNumber { get; set; } public string? PortName { get; set; } public bool IsActive { get; set; } public ICollection<DeviceLog> Logs { get; set; } = new List<DeviceLog>();
}


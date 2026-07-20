using SmartPOS.Shared.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartPOS.Data.Entities;

public class InventorySyncLog
{
    public Guid Id { get; set; }
    public string SyncType { get; set; } = string.Empty;
    public SyncStatus Status { get; set; }
    public string? Message { get; set; }
    [NotMapped]
    public int AffectedRows { get; set; }
    public DateTime SyncedAt { get; set; }
}

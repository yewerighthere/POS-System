using SmartPOS.Shared.Enums;

namespace SmartPOS.Data.Entities;

public class Return
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; } public Order Order { get; set; } = null!; public Guid RequestedBy { get; set; } public User RequestedByUser { get; set; } = null!; public Guid? ApprovedBy { get; set; } public ReturnStatus Status { get; set; } public string Reason { get; set; } = string.Empty; public decimal? RefundAmount { get; set; } public DateTime CreatedAt { get; set; } public DateTime? ResolvedAt { get; set; } public ICollection<ReturnItem> Items { get; set; } = new List<ReturnItem>();
}


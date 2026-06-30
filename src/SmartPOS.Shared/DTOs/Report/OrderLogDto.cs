namespace SmartPOS.Shared.DTOs.Report;

public class OrderLogDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string ItemsSummary { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string DisplayId => $"#ORD-{Id.ToString("N")[..5].ToUpper()}";
}

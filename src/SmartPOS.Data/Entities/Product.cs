using SmartPOS.Shared.Enums;

namespace SmartPOS.Data.Entities;

public class Product
{
    public Guid Id { get; set; }
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? QrCode { get; set; }
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public bool IsActive { get; set; }
    public string? ExternalInventoryId { get; set; }
    public int LocalStockQuantity { get; set; }
    public string? ImagePath { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}


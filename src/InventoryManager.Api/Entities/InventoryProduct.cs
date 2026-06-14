namespace InventoryManager.Api.Entities;

public class InventoryProduct
{
    public Guid Id { get; set; }
    public Guid? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? QrCode { get; set; }
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public InventoryCategory? Category { get; set; }
    public StockItem? StockItem { get; set; }
    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}

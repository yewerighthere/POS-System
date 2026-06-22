namespace InventoryManager.Api.Entities;

public class StockItem
{
    public Guid Id { get; set; }
    public Guid InventoryProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public InventoryProduct InventoryProduct { get; set; } = null!;
}

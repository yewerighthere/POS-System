namespace InventoryManager.Api.Entities;

public class StockTransaction
{
    public Guid Id { get; set; }
    public Guid InventoryProductId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? ReferenceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public InventoryProduct InventoryProduct { get; set; } = null!;
}

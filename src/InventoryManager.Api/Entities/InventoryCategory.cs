namespace InventoryManager.Api.Entities;

public class InventoryCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<InventoryProduct> Products { get; set; } = new List<InventoryProduct>();
}

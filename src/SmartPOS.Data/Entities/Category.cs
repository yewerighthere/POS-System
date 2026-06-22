using SmartPOS.Shared.Enums;

namespace SmartPOS.Data.Entities;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; public string? Description { get; set; } public bool IsActive { get; set; } public ICollection<Product> Products { get; set; } = new List<Product>();
}


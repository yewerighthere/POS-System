namespace SmartPOS.Shared.DTOs.Product;

public class ProductDto { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; public string Sku { get; set; } = string.Empty; public string? Barcode { get; set; } public decimal UnitPrice { get; set; } public int LocalStockQuantity { get; set; } }


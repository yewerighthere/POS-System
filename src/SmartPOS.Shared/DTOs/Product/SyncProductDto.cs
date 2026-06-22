namespace SmartPOS.Shared.DTOs.Product;

public record SyncProductDto(string ExternalInventoryId, string Name, string Sku, decimal UnitPrice, int StockQuantity);


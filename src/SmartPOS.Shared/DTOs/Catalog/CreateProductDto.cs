namespace SmartPOS.Shared.DTOs.Catalog;

public record CreateProductDto(Guid? CategoryId, string Name, string Sku, decimal UnitPrice, string? Barcode = null, string? QrCode = null, int InitialStock = 0, string? ImagePath = null);


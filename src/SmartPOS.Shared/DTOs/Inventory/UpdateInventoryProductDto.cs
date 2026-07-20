namespace SmartPOS.Shared.DTOs.Inventory;

public record UpdateInventoryProductDto(
    string Name,
    string Sku,
    string? Barcode,
    string? QrCode,
    string? Description,
    decimal UnitPrice,
    decimal TaxRate,
    Guid? CategoryId
);

namespace SmartPOS.Shared.DTOs.Catalog;

public record UpdateProductDto(
    Guid? CategoryId,
    string Name,
    string Sku,
    decimal UnitPrice,
    string? Barcode = null,
    string? QrCode = null,
    string? Description = null,
    decimal TaxRate = 0);

namespace SmartPOS.Shared.DTOs.Inventory;

public record StockDeductionEventDto(Guid OrderId, Guid ProductId, int Quantity);


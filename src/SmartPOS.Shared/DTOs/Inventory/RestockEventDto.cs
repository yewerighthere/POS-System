namespace SmartPOS.Shared.DTOs.Inventory;

public record RestockEventDto(Guid ProductId, int Quantity, string Reason);


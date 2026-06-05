namespace SmartPOS.Shared.DTOs.Order;

public record CreateOrderDto(Guid ShiftId, Guid UserId, Guid? CustomerId, List<OrderItemInputDto> Items);


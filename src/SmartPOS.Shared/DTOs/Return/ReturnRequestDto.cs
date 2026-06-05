namespace SmartPOS.Shared.DTOs.Return;

public record ReturnRequestDto(Guid OrderId, Guid RequestedBy, string Reason, List<ReturnItemInputDto> Items);


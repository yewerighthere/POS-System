namespace SmartPOS.Shared.DTOs.Payment;

public record VNPayCallbackDto(Guid OrderId, string TransactionId, string ResponseCode);


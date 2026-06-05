namespace SmartPOS.Shared.DTOs.Payment;

public record CashPaymentDto(Guid OrderId, decimal AmountReceived, Guid UserId);


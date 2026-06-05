namespace SmartPOS.Shared.DTOs.Payment;

public record VNPayRequestDto(Guid OrderId, decimal Amount, string ReturnUrl);


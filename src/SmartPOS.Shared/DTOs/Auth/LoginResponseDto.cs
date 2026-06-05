namespace SmartPOS.Shared.DTOs.Auth;

public class LoginResponseDto { public string Token { get; set; } = string.Empty; public UserSessionDto? User { get; set; } }


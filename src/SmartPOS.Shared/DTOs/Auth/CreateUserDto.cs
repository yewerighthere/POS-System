namespace SmartPOS.Shared.DTOs.Auth;

public record CreateUserDto(string Username, string Password, string FullName, string Role);


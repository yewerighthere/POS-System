namespace SmartPOS.Shared.DTOs.Auth;

public class UserDto { public Guid Id { get; set; } public string Username { get; set; } = string.Empty; public string FullName { get; set; } = string.Empty; public string Role { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; }


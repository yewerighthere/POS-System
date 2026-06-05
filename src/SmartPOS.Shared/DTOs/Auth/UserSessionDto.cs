namespace SmartPOS.Shared.DTOs.Auth;

public class UserSessionDto { public Guid Id { get; set; } public Guid UserId { get; set; } public string Username { get; set; } = string.Empty; public string FullName { get; set; } = string.Empty; public string Role { get; set; } = string.Empty; }


using SmartPOS.Shared.DTOs.Auth;

namespace SmartPOS.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
    Task LogoutAsync(Guid sessionId);
    Task<UserSessionDto?> ValidateTokenAsync(string token);
    Task CreateDemoUserIfNeededAsync();
    Task<UserDto> CreateUserAsync(CreateUserDto request, Guid createdByUserId);
}

using SmartPOS.Shared.DTOs.Auth;

namespace SmartPOS.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
    Task LogoutAsync(Guid sessionId);
    Task<UserSessionDto?> ValidateTokenAsync(string token);
    Task CreateDemoUserIfNeededAsync();
    Task<UserDto> CreateUserAsync(CreateUserDto request, Guid createdByUserId);
    Task<System.Collections.Generic.IReadOnlyList<UserDto>> GetAllUsersAsync(Guid requestingUserId);
    Task<UserDto> UpdateUserAsync(UpdateUserDto request, Guid requestingUserId);
    Task<UserDto> ToggleUserStatusAsync(Guid userId, Guid requestingUserId);
    Task<UserDto> ResetPasswordAsync(Guid userId, string newPassword, Guid requestingUserId);
}

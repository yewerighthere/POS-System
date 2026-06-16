using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Auth;
using SmartPOS.Shared.Enums;
using SmartPOS.Shared.Exceptions;

namespace SmartPOS.Services.Implementations;

public class AuthService : IAuthService
{
    private const string InvalidLoginMessage = "Tên đăng nhập hoặc mật khẩu không đúng";
    private const string LockedMessage = "Tài khoản đã bị khóa, vui lòng liên hệ quản lý";
    private const string InactiveMessage = "Tài khoản đã ngừng hoạt động";
    private const string ForbiddenMessage = "Bạn không có quyền truy cập chức năng này";

    private readonly IUserRepository _userRepository;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IUserSessionRepository userSessionRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _userSessionRepository = userSessionRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var username = dto.Username.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(dto.Password))
            throw new BusinessException(InvalidLoginMessage);

        var user = await _userRepository.GetByUsernameAsync(username).ConfigureAwait(false);
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new BusinessException(InvalidLoginMessage);

        if (user.Status == UserStatus.Locked)
            throw new BusinessException(LockedMessage);

        if (user.Status == UserStatus.Inactive)
            throw new BusinessException(InactiveMessage);

        var activeSession = await _userSessionRepository.GetActiveByUserIdAsync(user.Id).ConfigureAwait(false);
        if (activeSession is not null)
        {
            activeSession.LogoutAt = DateTime.UtcNow;
            await _userSessionRepository.UpdateLogoutAsync(activeSession).ConfigureAwait(false);
        }

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            LoginAt = DateTime.UtcNow
        };

        await _userSessionRepository.AddAsync(session).ConfigureAwait(false);

        var sessionDto = ToSessionDto(session, user);
        var token = CreateJwt(user, session.Id);

        _logger.LogInformation("Người dùng {Username} đã đăng nhập thành công", user.Username);

        return new LoginResponseDto
        {
            Token = token,
            User = sessionDto
        };
    }

    public async Task LogoutAsync(Guid sessionId)
    {
        var session = await _userSessionRepository.GetByIdAsync(sessionId).ConfigureAwait(false);
        if (session is null || session.LogoutAt is not null)
            return;

        session.LogoutAt = DateTime.UtcNow;
        await _userSessionRepository.UpdateLogoutAsync(session).ConfigureAwait(false);
        _logger.LogInformation("Phiên đăng nhập {SessionId} đã đăng xuất", sessionId);
    }

    public async Task<UserSessionDto?> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var principal = ValidateJwt(token);
            var sessionIdValue = principal.FindFirst(JwtRegisteredClaimNames.Sid)?.Value
                ?? principal.FindFirst("sid")?.Value;

            if (!Guid.TryParse(sessionIdValue, out var sessionId))
                return null;

            var session = await _userSessionRepository.GetByIdAsync(sessionId).ConfigureAwait(false);
            if (session is null || session.LogoutAt is not null)
                return null;

            return ToSessionDto(session, session.User);
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            _logger.LogWarning(ex, "JWT không hợp lệ");
            return null;
        }
    }

    public async Task CreateDemoUserIfNeededAsync()
    {
        await CreateDemoUserAsync("quantri", "Quản trị", UserRole.Admin).ConfigureAwait(false);
        await CreateDemoUserAsync("quanly", "Quản lý", UserRole.Manager).ConfigureAwait(false);
        await CreateDemoUserAsync("nhanvien", "Nhân viên", UserRole.Staff).ConfigureAwait(false);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto request, Guid createdByUserId)
    {
        var creator = await _userRepository.GetByIdAsync(createdByUserId).ConfigureAwait(false)
            ?? throw new BusinessException(ForbiddenMessage);

        if (creator.Role != UserRole.Manager && creator.Role != UserRole.Admin)
            throw new BusinessException(ForbiddenMessage);

        var username = request.Username.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
            throw new BusinessException("Tên đăng nhập và mật khẩu không được để trống");

        var existingUser = await _userRepository.GetByUsernameAsync(username).ConfigureAwait(false);
        if (existingUser is not null)
            throw new BusinessException("Tên đăng nhập đã tồn tại");

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
            throw new BusinessException("Vai trò không hợp lệ");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Role = role,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user).ConfigureAwait(false);
        _logger.LogInformation("Đã tạo tài khoản {Username}", user.Username);

        return ToUserDto(user);
    }

    private async Task CreateDemoUserAsync(string username, string fullName, UserRole role)
    {
        var existingUser = await _userRepository.GetByUsernameAsync(username).ConfigureAwait(false);
        if (existingUser is not null)
            return;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            FullName = fullName,
            Role = role,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user).ConfigureAwait(false);
    }

    private string CreateJwt(User user, Guid sessionId)
    {
        var jwtSettings = GetJwtSettings();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sid, sessionId.ToString()),
            new(JwtRegisteredClaimNames.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(jwtSettings.ExpiresMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal ValidateJwt(string token)
    {
        var jwtSettings = GetJwtSettings();
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

        return tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        }, out _);
    }

    private JwtSettings GetJwtSettings()
    {
        var issuer = _configuration["Jwt:Issuer"] ?? "SmartPOS";
        var audience = _configuration["Jwt:Audience"] ?? "SmartPOS.Client";
        var secretKey = _configuration["Jwt:SecretKey"] ?? "SmartPOS_Demo_Secret_Key_2026_ChangeMe";
        var expiresMinutesText = _configuration["Jwt:ExpiresMinutes"];

        if (secretKey.Length < 32)
            throw new InvalidOperationException("Jwt:SecretKey phải có tối thiểu 32 ký tự");

        return new JwtSettings(
            issuer,
            audience,
            secretKey,
            int.TryParse(expiresMinutesText, out var expiresMinutes) ? expiresMinutes : 480);
    }

    private static UserSessionDto ToSessionDto(UserSession session, User user)
    {
        return new UserSessionDto
        {
            Id = session.Id,
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName ?? string.Empty,
            Role = user.Role.ToString()
        };
    }

    private static UserDto ToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName ?? string.Empty,
            Role = user.Role.ToString(),
            Status = user.Status.ToString()
        };
    }

    private sealed record JwtSettings(string Issuer, string Audience, string SecretKey, int ExpiresMinutes);
}

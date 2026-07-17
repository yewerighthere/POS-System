using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Implementations;
using SmartPOS.Shared.DTOs.Auth;
using SmartPOS.Shared.Enums;
using SmartPOS.Shared.Exceptions;
using Xunit;

namespace SmartPOS.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task CreateDemoUserIfNeededAsync_CreatesThreeDemoUsers()
    {
        var userRepository = new FakeUserRepository();
        var service = CreateService(userRepository);

        await service.CreateDemoUserIfNeededAsync();
        await service.CreateDemoUserIfNeededAsync();

        userRepository.Users.Should().HaveCount(3);
        userRepository.Users.Select(user => user.Username)
            .Should().BeEquivalentTo("quantri", "quanly", "nhanvien");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsJwtAndCreatesSession()
    {
        var userRepository = new FakeUserRepository();
        var sessionRepository = new FakeUserSessionRepository();
        var user = AddUser(userRepository, "nhanvien", "123456", UserRole.Staff, UserStatus.Active);
        var service = CreateService(userRepository, sessionRepository);

        var result = await service.LoginAsync(new LoginRequestDto("nhanvien", "123456"));

        result.Token.Should().NotBeNullOrWhiteSpace();
        result.User.Should().NotBeNull();
        result.User!.UserId.Should().Be(user.Id);
        sessionRepository.Sessions.Should().ContainSingle();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsBusinessException()
    {
        var userRepository = new FakeUserRepository();
        AddUser(userRepository, "nhanvien", "123456", UserRole.Staff, UserStatus.Active);
        var service = CreateService(userRepository);

        var act = () => service.LoginAsync(new LoginRequestDto("nhanvien", "wrong"));

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Tên đăng nhập hoặc mật khẩu không đúng");
    }

    [Fact]
    public async Task LoginAsync_WithLockedUser_ThrowsLockedMessage()
    {
        var userRepository = new FakeUserRepository();
        AddUser(userRepository, "locked", "123456", UserRole.Staff, UserStatus.Locked);
        var service = CreateService(userRepository);

        var act = () => service.LoginAsync(new LoginRequestDto("locked", "123456"));

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Tài khoản đã bị khóa, vui lòng liên hệ quản lý");
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ThrowsInactiveMessage()
    {
        var userRepository = new FakeUserRepository();
        AddUser(userRepository, "inactive", "123456", UserRole.Staff, UserStatus.Inactive);
        var service = CreateService(userRepository);

        var act = () => service.LoginAsync(new LoginRequestDto("inactive", "123456"));

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Tài khoản đã ngừng hoạt động");
    }

    [Fact]
    public async Task LoginAsync_ClosesExistingActiveSession()
    {
        var userRepository = new FakeUserRepository();
        var sessionRepository = new FakeUserSessionRepository();
        var user = AddUser(userRepository, "nhanvien", "123456", UserRole.Staff, UserStatus.Active);
        sessionRepository.Sessions.Add(new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            LoginAt = DateTime.UtcNow.AddMinutes(-5)
        });
        var service = CreateService(userRepository, sessionRepository);

        await service.LoginAsync(new LoginRequestDto("nhanvien", "123456"));

        sessionRepository.Sessions.Should().HaveCount(2);
        sessionRepository.Sessions.Count(session => session.LogoutAt is null).Should().Be(1);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ReturnsSession()
    {
        var userRepository = new FakeUserRepository();
        var sessionRepository = new FakeUserSessionRepository();
        AddUser(userRepository, "nhanvien", "123456", UserRole.Staff, UserStatus.Active);
        var service = CreateService(userRepository, sessionRepository);
        var login = await service.LoginAsync(new LoginRequestDto("nhanvien", "123456"));

        var session = await service.ValidateTokenAsync(login.Token);

        session.Should().NotBeNull();
        session!.Username.Should().Be("nhanvien");
    }

    [Fact]
    public async Task ValidateTokenAsync_AfterLogout_ReturnsNull()
    {
        var userRepository = new FakeUserRepository();
        var sessionRepository = new FakeUserSessionRepository();
        AddUser(userRepository, "nhanvien", "123456", UserRole.Staff, UserStatus.Active);
        var service = CreateService(userRepository, sessionRepository);
        var login = await service.LoginAsync(new LoginRequestDto("nhanvien", "123456"));

        await service.LogoutAsync(login.User!.Id);
        var session = await service.ValidateTokenAsync(login.Token);

        session.Should().BeNull();
    }

    private static AuthService CreateService(
        FakeUserRepository userRepository,
        FakeUserSessionRepository? sessionRepository = null,
        IDictionary<string, string?>? configValues = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues ?? new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "SmartPOS",
                ["Jwt:Audience"] = "SmartPOS.Client",
                ["Jwt:SecretKey"] = "SmartPOS_Demo_Secret_Key_2026_ChangeMe",
                ["Jwt:ExpiresMinutes"] = "480"
            })
            .Build();

        return new AuthService(
            userRepository,
            sessionRepository ?? new FakeUserSessionRepository(),
            configuration,
            NullLogger<AuthService>.Instance);
    }

    private static User AddUser(
        FakeUserRepository repository,
        string username,
        string password,
        UserRole role,
        UserStatus status)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = username,
            Role = role,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        repository.Users.Add(user);
        return user;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = new();

        public Task<User?> GetByUsernameAsync(string username)
        {
            return Task.FromResult(Users.FirstOrDefault(user => user.Username == username));
        }

        public Task<User?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(Users.FirstOrDefault(user => user.Id == id));
        }

        public Task AddAsync(User user)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user)
        {
            return Task.CompletedTask;
        }

        public Task<System.Collections.Generic.IReadOnlyList<User>> GetAllAsync()
        {
            return Task.FromResult<System.Collections.Generic.IReadOnlyList<User>>(Users);
        }
    }

    private sealed class FakeUserSessionRepository : IUserSessionRepository
    {
        public List<UserSession> Sessions { get; } = new();

        public Task<UserSession?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(Sessions.FirstOrDefault(session => session.Id == id));
        }

        public Task<UserSession?> GetActiveByUserIdAsync(Guid userId)
        {
            return Task.FromResult(Sessions.FirstOrDefault(session => session.UserId == userId && session.LogoutAt is null));
        }

        public Task AddAsync(UserSession session)
        {
            Sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task UpdateLogoutAsync(UserSession session)
        {
            return Task.CompletedTask;
        }
    }
}

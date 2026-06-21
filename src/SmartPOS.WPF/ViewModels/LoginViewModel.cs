using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Auth;
using SmartPOS.Shared.Enums;
using SmartPOS.Shared.Exceptions;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;

namespace SmartPOS.WPF.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly CurrentSessionContext _sessionContext;
    private readonly NavigationService _navigationService;
    private readonly ILogger<LoginViewModel> _logger;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public LoginViewModel(
        IAuthService authService,
        CurrentSessionContext sessionContext,
        NavigationService navigationService,
        ILogger<LoginViewModel> logger)
    {
        _authService = authService;
        _sessionContext = sessionContext;
        _navigationService = navigationService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            await _authService.CreateDemoUserIfNeededAsync();
            var result = await _authService.LoginAsync(new LoginRequestDto(Username, Password));

            _sessionContext.CurrentUser = result.User;
            _sessionContext.CurrentToken = result.Token;

            NavigateAfterLogin(result.User?.Role);
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Đã có lỗi xảy ra khi đăng nhập hoặc điều hướng sau đăng nhập");
            ErrorMessage = "Đã có lỗi xảy ra, vui lòng kiểm tra nhật ký";
        }
        finally
        {
            IsLoading = false;
            Password = string.Empty;
        }
    }

    private void NavigateAfterLogin(string? role)
    {
        if (Enum.TryParse<UserRole>(role, out var parsedRole) && parsedRole == UserRole.Staff)
        {
            _navigationService.NavigateTo<ShiftViewModel>();
            return;
        }

        _navigationService.NavigateTo<SyncViewModel>();
    }
}

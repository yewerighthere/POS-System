using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;
using SmartPOS.WPF.Navigation;

namespace SmartPOS.WPF.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly NavigationService _navigationService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public LoginViewModel(IAuthService authService, NavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    [RelayCommand]
    private void Execute()
    {
        // TODO
    }

    [RelayCommand]
    private void GoToSales()
    {
        _navigationService.NavigateTo<SalesViewModel>();
    }
}


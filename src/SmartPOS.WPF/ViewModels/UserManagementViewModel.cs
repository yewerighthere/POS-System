using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;

namespace SmartPOS.WPF.ViewModels;

public partial class UserManagementViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public UserManagementViewModel(IAuthService authService)
    {
    }

    [RelayCommand]
    private void Execute()
    {
        // TODO
    }
}


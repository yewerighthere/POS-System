using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;

namespace SmartPOS.WPF.ViewModels;

public partial class AuditLogViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public AuditLogViewModel(IAuditService auditService)
    {
    }

    [RelayCommand]
    private void Execute()
    {
        // TODO
    }
}


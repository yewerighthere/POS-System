using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;

namespace SmartPOS.WPF.ViewModels;

public partial class ReportViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public ReportViewModel(IReportService reportService)
    {
    }

    [RelayCommand]
    private void Execute()
    {
        // TODO
    }
}


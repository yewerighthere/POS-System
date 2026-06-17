using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.ViewModels;
using System.Windows;

namespace SmartPOS.WPF;

public partial class MainWindow : Window
{
    private readonly NavigationService _navigationService;

    public MainWindow(NavigationService navigationService)
    {
        InitializeComponent();
        _navigationService = navigationService;
        _navigationService.MainFrame = MainFrame;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _navigationService.NavigateTo<LoginViewModel>();
    }
}

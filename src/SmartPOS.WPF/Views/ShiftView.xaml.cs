using SmartPOS.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SmartPOS.WPF.Views;

public partial class ShiftView : UserControl
{
    public ShiftView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ShiftViewModel vm)
            await vm.InitializeAsync();
    }
}

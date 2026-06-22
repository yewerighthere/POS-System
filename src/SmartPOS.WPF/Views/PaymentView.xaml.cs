using System.Windows;
using System.Windows.Controls;
using SmartPOS.WPF.ViewModels;

namespace SmartPOS.WPF.Views;

public partial class PaymentView : UserControl
{
    public PaymentView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PaymentViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}

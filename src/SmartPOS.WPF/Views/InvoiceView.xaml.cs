using System.Windows;
using System.Windows.Controls;
using SmartPOS.WPF.ViewModels;

namespace SmartPOS.WPF.Views;

public partial class InvoiceView : UserControl
{
    public InvoiceView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is InvoiceViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using SmartPOS.WPF.ViewModels;

namespace SmartPOS.WPF.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel viewModel)
        {
            if (viewModel.LoadDataCommand.CanExecute(null))
            {
                viewModel.LoadDataCommand.Execute(null);
            }
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using SmartPOS.WPF.ViewModels;

namespace SmartPOS.WPF.Views;

public partial class DashboardReportView : UserControl
{
    public DashboardReportView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardReportViewModel vm)
            vm.LoadShiftsCommand.Execute(null);
    }
}

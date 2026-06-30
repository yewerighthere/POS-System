using SmartPOS.WPF.ViewModels;
using System.Windows.Controls;

namespace SmartPOS.WPF.Views;

public partial class ReportView : UserControl
{
    public ReportView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ReportViewModel vm)
            _ = vm.GenerateReportCommand.ExecuteAsync(null);
    }
}

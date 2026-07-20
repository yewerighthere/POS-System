using System.Windows.Controls;

namespace SmartPOS.WPF.Views;

public partial class ReturnView : UserControl
{
    public ReturnView(ViewModels.ReturnViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}


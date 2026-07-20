using System.Windows.Controls;

namespace SmartPOS.WPF.Views;

public partial class SalesView : UserControl
{
    public SalesView()
    {
        InitializeComponent();
        PreviewKeyDown += SalesView_PreviewKeyDown;
    }

    private void SalesView_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.F8)
        {
            CustomerPhoneInput.Focus();
            CustomerPhoneInput.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.F9)
        {
            if (DataContext is ViewModels.SalesViewModel vm && vm.CheckoutCommand.CanExecute(null))
            {
                vm.CheckoutCommand.Execute(null);
            }
            e.Handled = true;
        }
    }
}


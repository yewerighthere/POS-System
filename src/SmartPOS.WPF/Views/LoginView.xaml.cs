using SmartPOS.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SmartPOS.WPF.Views;

public partial class LoginView : UserControl
{
    private bool _isSyncingPassword;

    public LoginView()
    {
        InitializeComponent();
    }

    private LoginViewModel? ViewModel => DataContext as LoginViewModel;

    private void PasswordInput_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isSyncingPassword)
            return;

        _isSyncingPassword = true;
        VisiblePasswordInput.Text = PasswordInput.Password;
        if (ViewModel is not null)
            ViewModel.Password = PasswordInput.Password;
        _isSyncingPassword = false;
    }

    private void VisiblePasswordInput_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isSyncingPassword)
            return;

        _isSyncingPassword = true;
        PasswordInput.Password = VisiblePasswordInput.Text;
        if (ViewModel is not null)
            ViewModel.Password = VisiblePasswordInput.Text;
        _isSyncingPassword = false;
    }

    private void TogglePasswordButton_OnToggled(object sender, RoutedEventArgs e)
    {
        var showPassword = TogglePasswordButton.IsChecked == true;
        PasswordInput.Visibility = showPassword ? Visibility.Collapsed : Visibility.Visible;
        VisiblePasswordInput.Visibility = showPassword ? Visibility.Visible : Visibility.Collapsed;
        PasswordToggleIcon.Text = showPassword ? "🙈" : "👁";
    }
}

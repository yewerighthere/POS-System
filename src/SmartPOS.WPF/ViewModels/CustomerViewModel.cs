using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Customer;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;
using System;
using System.Threading.Tasks;

namespace SmartPOS.WPF.ViewModels;

public partial class CustomerViewModel : ObservableObject
{
    private readonly ICustomerService _customerService;
    private readonly NavigationService _navigationService;
    private readonly CurrentSessionContext _session;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public CustomerViewModel(
        ICustomerService customerService,
        NavigationService navigationService,
        CurrentSessionContext session)
    {
        _customerService = customerService;
        _navigationService = navigationService;
        _session = session;

        if (!string.IsNullOrEmpty(_session.CustomerPhoneInput))
        {
            Phone = _session.CustomerPhoneInput;
        }
    }

    [RelayCommand]
    private async Task AcceptAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(FullName))
        {
            ErrorMessage = "Vui lòng nhập tên khách hàng.";
            return;
        }

        var result = System.Windows.MessageBox.Show(
            "Bạn có chắc chắn muốn tạo khách hàng này không?",
            "Xác nhận tạo",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        IsLoading = true;
        try
        {
            var dto = new CreateCustomerDto(this.FullName, this.Phone, string.IsNullOrWhiteSpace(this.Email) ? null : this.Email);

            var customer = await _customerService.CreateAsync(dto);
            
            // Cập nhật session và quay về SalesView
            _session.CustomerInfo = $"{customer.FullName ?? customer.MemberCode} ({customer.Phone})";
            _navigationService.NavigateTo<SalesViewModel>();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi tạo khách hàng: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        var result = System.Windows.MessageBox.Show(
            "Bạn có chắc chắn muốn hủy tạo khách hàng không?",
            "Xác nhận hủy",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            _navigationService.NavigateTo<SalesViewModel>();
        }
    }
}

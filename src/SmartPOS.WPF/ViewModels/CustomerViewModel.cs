using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Customer;
using SmartPOS.WPF.Navigation;
using System.Windows;

namespace SmartPOS.WPF.ViewModels;

public partial class CustomerViewModel : ObservableObject
{
    private readonly ICustomerService _customerService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _statusFilters = new() { "All", "Active", "Banned" };

    [ObservableProperty]
    private string _selectedStatusFilter = "All";

    [ObservableProperty]
    private ObservableCollection<string> _orderFilters = new() { "All", "Has Orders", "No Orders" };

    [ObservableProperty]
    private string _selectedOrderFilter = "All";

    [ObservableProperty]
    private ObservableCollection<string> _sortOptions = new() 
    { 
        "DateDesc", "DateAsc", "NameAsc", "NameDesc", "IdAsc", "PointsDesc", "PointsAsc", "OrdersDesc", "OrdersAsc" 
    };

    [ObservableProperty]
    private string _selectedSortOption = "DateDesc";

    [ObservableProperty]
    private bool _isCustomerDetailPopupOpen;

    [ObservableProperty]
    private bool _isOrderDetailPopupOpen;

    [ObservableProperty]
    private CustomerDetailDto? _selectedCustomerDetail;

    [ObservableProperty]
    private CustomerOrderDetailDto? _selectedOrderDetail;

    [ObservableProperty]
    private string _editingCustomerName = string.Empty;

    [ObservableProperty]
    private string _editingCustomerPhone = string.Empty;

    [ObservableProperty]
    private string _editingCustomerEmail = string.Empty;

    public ObservableCollection<CustomerListDto> Customers { get; } = new();

    private readonly NavigationService _navigationService;

    public CustomerViewModel(ICustomerService customerService, NavigationService navigationService)
    {
        _customerService = customerService;
        _navigationService = navigationService;
        LoadDataCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteSearchAsync();
    }

    [RelayCommand]
    private async Task ExecuteSearchAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var result = await _customerService.GetCustomerListAsync(SearchTerm, SelectedStatusFilter, SelectedOrderFilter, SelectedSortOption);
            
            Customers.Clear();
            foreach (var item in result)
            {
                Customers.Add(item);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading customers: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ToggleStatusAsync(Guid customerId)
    {
        try
        {
            await _customerService.ToggleCustomerStatusAsync(customerId);
            await ExecuteSearchAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to toggle status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ViewDetailAsync(Guid customerId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            
            var detail = await _customerService.GetCustomerDetailAsync(customerId);
            if (detail != null)
            {
                SelectedCustomerDetail = detail;
                EditingCustomerName = detail.FullName;
                EditingCustomerPhone = detail.Phone;
                EditingCustomerEmail = detail.Email ?? string.Empty;
                IsCustomerDetailPopupOpen = true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải thông tin khách hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CloseCustomerDetailPopup()
    {
        IsCustomerDetailPopupOpen = false;
        SelectedCustomerDetail = null;
    }

    [RelayCommand]
    private async Task UpdateCustomerAsync()
    {
        if (SelectedCustomerDetail == null) return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var dto = new UpdateCustomerDto
            {
                Id = SelectedCustomerDetail.Id,
                FullName = EditingCustomerName,
                Phone = EditingCustomerPhone,
                Email = string.IsNullOrWhiteSpace(EditingCustomerEmail) ? null : EditingCustomerEmail,
                IsActive = SelectedCustomerDetail.IsActive
            };

            var updatedCustomer = await _customerService.UpdateCustomerAsync(dto);
            SelectedCustomerDetail = updatedCustomer;
            await ExecuteSearchAsync(); // Refresh list
            MessageBox.Show("Cập nhật thông tin thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi cập nhật: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ToggleCustomerStatusFromPopupAsync()
    {
        if (SelectedCustomerDetail == null) return;
        
        try
        {
            IsLoading = true;
            await _customerService.ToggleCustomerStatusAsync(SelectedCustomerDetail.Id);
            
            // Refresh customer detail to reflect new status
            var updatedCustomer = await _customerService.GetCustomerDetailAsync(SelectedCustomerDetail.Id);
            if (updatedCustomer != null)
            {
                SelectedCustomerDetail = updatedCustomer;
            }
            await ExecuteSearchAsync(); // Refresh list
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ViewOrderDetailAsync(Guid orderId)
    {
        try
        {
            IsLoading = true;
            var orderDetail = await _customerService.GetCustomerOrderDetailAsync(orderId);
            if (orderDetail != null)
            {
                SelectedOrderDetail = orderDetail;
                IsOrderDetailPopupOpen = true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải thông tin đơn hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CloseOrderDetailPopup()
    {
        IsOrderDetailPopupOpen = false;
        SelectedOrderDetail = null;
    }

    [RelayCommand]
    private void CloseBothPopups()
    {
        IsOrderDetailPopupOpen = false;
        IsCustomerDetailPopupOpen = false;
        SelectedOrderDetail = null;
        SelectedCustomerDetail = null;
    }

    [RelayCommand]
    private void NavigateToSales()
    {
        _navigationService.NavigateTo<SalesViewModel>();
    }

    [RelayCommand]
    private void NavigateToCatalog()
    {
        _navigationService.NavigateTo<CatalogViewModel>();
    }

    [RelayCommand]
    private void NavigateToSync()
    {
        _navigationService.NavigateTo<SyncViewModel>();
    }

    [RelayCommand]
    private void NavigateToReport()
    {
        _navigationService.NavigateTo<ReportViewModel>();
    }
}


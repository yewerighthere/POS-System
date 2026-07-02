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
    private void ViewDetail(Guid customerId)
    {
        MessageBox.Show($"Viewing details for Customer {customerId} will be implemented soon.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
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


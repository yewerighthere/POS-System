using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Data.Entities;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Catalog;
using SmartPOS.Shared.DTOs.Product;
using System.Collections.ObjectModel;

namespace SmartPOS.WPF.ViewModels;

public partial class CatalogViewModel : ObservableObject
{
    private readonly ICatalogService _catalogService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CategoryDto> _categories = new();

    [ObservableProperty]
    private ObservableCollection<ProductDto> _products = new();

    [ObservableProperty]
    private CategoryDto? _selectedCategory;

    [ObservableProperty]
    private string _newCategoryName = string.Empty;

    [ObservableProperty]
    private string _newProductName = string.Empty;

    [ObservableProperty]
    private string _newProductSku = string.Empty;

    [ObservableProperty]
    private decimal _newProductPrice;

    public CatalogViewModel(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [RelayCommand]
    private async Task Execute()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var categories = await _catalogService.GetCategoriesAsync();
            Categories = new ObservableCollection<CategoryDto>(categories);

            var products = await _catalogService.GetProductsAsync();
            Products = new ObservableCollection<ProductDto>(products);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Không tải được dữ liệu: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddCategory()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName)) return;

        await _catalogService.CreateCategoryAsync(new CreateCategoryDto(NewCategoryName, null));
        NewCategoryName = string.Empty;
        await Execute();
    }

    [RelayCommand]
    private async Task AddProduct()
    {
        if (string.IsNullOrWhiteSpace(NewProductName) || SelectedCategory == null) return;

        var dto = new CreateProductDto(
            SelectedCategory.Id,
            NewProductName,
            NewProductSku,
            NewProductPrice
        );

        var created = await _catalogService.CreateProductAsync(dto);
        Products.Add(created);

        NewProductName = string.Empty;
        NewProductSku = string.Empty;
        NewProductPrice = 0;
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Catalog;
using SmartPOS.Shared.DTOs.Product;
using SmartPOS.Shared.Exceptions;
using SmartPOS.WPF.Session;
using System.Collections.ObjectModel;

namespace SmartPOS.WPF.ViewModels;

public partial class CatalogViewModel : ObservableObject
{
    private readonly ICatalogService _catalogService;
    private readonly CurrentSessionContext _session;

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
    private ProductDto? _selectedProduct;

    [ObservableProperty]
    private string _newCategoryName = string.Empty;

    [ObservableProperty]
    private string _newProductName = string.Empty;

    [ObservableProperty]
    private string _newProductSku = string.Empty;

    [ObservableProperty]
    private string _newProductBarcode = string.Empty;

    [ObservableProperty]
    private decimal _newProductPrice;

    [ObservableProperty]
    private decimal _updatedPrice;

    public CatalogViewModel(ICatalogService catalogService, CurrentSessionContext session)
    {
        _catalogService = catalogService;
        _session = session;
    }

    [RelayCommand]
    public async Task LoadAsync()
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

        ErrorMessage = string.Empty;
        try
        {
            await _catalogService.CreateCategoryAsync(new CreateCategoryDto(NewCategoryName, null));
            NewCategoryName = string.Empty;
            await LoadAsync();
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task AddProduct()
    {
        if (string.IsNullOrWhiteSpace(NewProductName) || SelectedCategory == null) return;

        ErrorMessage = string.Empty;
        try
        {
            var dto = new CreateProductDto(
                CategoryId: SelectedCategory.Id,
                Name: NewProductName,
                Sku: NewProductSku,
                UnitPrice: NewProductPrice,
                Barcode: string.IsNullOrWhiteSpace(NewProductBarcode) ? null : NewProductBarcode
            );

            var created = await _catalogService.CreateProductAsync(dto);
            Products.Add(created);

            NewProductName = string.Empty;
            NewProductSku = string.Empty;
            NewProductBarcode = string.Empty;
            NewProductPrice = 0;
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task UpdatePrice()
    {
        if (SelectedProduct == null) return;

        ErrorMessage = string.Empty;
        try
        {
            var userId = _session.RequireUserId();
            var dto = new UpdatePriceDto(SelectedProduct.Id, UpdatedPrice);
            var updated = await _catalogService.UpdatePriceAsync(dto, userId);

            // Cập nhật item trong danh sách
            var index = Products.IndexOf(SelectedProduct);
            if (index >= 0) Products[index] = updated;
            SelectedProduct = updated;
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeactivateProduct()
    {
        if (SelectedProduct == null) return;

        ErrorMessage = string.Empty;
        try
        {
            var userId = _session.RequireUserId();
            await _catalogService.DeactivateProductAsync(SelectedProduct.Id, userId);
            Products.Remove(SelectedProduct);
            SelectedProduct = null;
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
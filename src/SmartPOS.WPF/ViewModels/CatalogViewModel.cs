using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Catalog;
using SmartPOS.Shared.DTOs.Product;
using SmartPOS.Shared.Exceptions;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using SmartPOS.Shared.DTOs.Inventory;

namespace SmartPOS.WPF.ViewModels;

public partial class CatalogViewModel : ObservableObject
{
    private readonly ICatalogService _catalogService;
    private readonly CurrentSessionContext _session;
    private readonly NavigationService _navigationService;
    private readonly IInventorySyncService _inventorySyncService;

    private List<ProductDto> _allProducts = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private ObservableCollection<CategoryDto> _categories = new();
    [ObservableProperty] private ObservableCollection<ProductDto> _filteredProducts = new();
    [ObservableProperty] private CategoryDto? _selectedCategory;
    [ObservableProperty] private ProductDto? _selectedProduct;

    [ObservableProperty] private string _newCategoryName = string.Empty;
    [ObservableProperty] private string _newProductName = string.Empty;
    [ObservableProperty] private string _newProductSku = string.Empty;
    [ObservableProperty] private string _newProductBarcode = string.Empty;
    [ObservableProperty] private decimal _newProductPrice;
    [ObservableProperty] private decimal _updatedPrice;

    [ObservableProperty] private bool _isAddProductVisible;
    [ObservableProperty] private bool _isSyncing;
    [ObservableProperty] private string _syncStatus = string.Empty;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private string _categoryFilter = "Tất cả";
    [ObservableProperty] private string _statusFilter = "Tất cả";

    public int FilteredCount => FilteredProducts.Count;

    public List<string> StatusOptions { get; } = new() { "Tất cả", "Active", "Inactive" };

    [ObservableProperty] private List<string> _categoryFilterOptions = new() { "Tất cả" };

    public CatalogViewModel(ICatalogService catalogService, CurrentSessionContext session, NavigationService navigationService, IInventorySyncService inventorySyncService)
    {
        _catalogService = catalogService;
        _session = session;
        _navigationService = navigationService;
        _inventorySyncService = inventorySyncService;
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilter();
    partial void OnCategoryFilterChanged(string value) => ApplyFilter();
    partial void OnStatusFilterChanged(string value) => ApplyFilter();

    [RelayCommand]
    private void NavigateToSales() => _navigationService.NavigateTo<SalesViewModel>();

    [RelayCommand]
    private void ToggleAddProduct() => IsAddProductVisible = !IsAddProductVisible;

    [RelayCommand]
    private async Task SyncFromInventory()
    {
        IsSyncing = true;
        SyncStatus = string.Empty;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _inventorySyncService.SyncCatalogAsync();
            if (result.Status == "SUCCESS")
            {
                SyncStatus = result.Message ?? string.Empty;
                await LoadAsync();
            }
            else
            {
                ErrorMessage = result.Message ?? "Sync thất bại";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Sync thất bại: " + ex.Message;
        }
        finally
        {
            IsSyncing = false;
        }
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
            CategoryFilterOptions = new List<string>(new[] { "Tất cả" }.Concat(categories.Select(c => c.Name)));

            var products = await _catalogService.GetProductsAsync();
            _allProducts = products.ToList();
            ApplyFilter();
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

    private void ApplyFilter()
    {
        var query = SearchQuery.Trim();
        var result = _allProducts.AsEnumerable();

        if (!string.IsNullOrEmpty(query))
            result = result.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Sku.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (p.Barcode?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));

        if (CategoryFilter != "Tất cả")
            result = result.Where(p => p.CategoryName == CategoryFilter);

        if (StatusFilter == "Active")
            result = result.Where(p => p.IsActive);
        else if (StatusFilter == "Inactive")
            result = result.Where(p => !p.IsActive);

        FilteredProducts = new ObservableCollection<ProductDto>(result);
        OnPropertyChanged(nameof(FilteredCount));
    }

    public List<string> GetCategoryFilterOptions()
    {
        var opts = new List<string> { "Tất cả" };
        opts.AddRange(Categories.Select(c => c.Name));
        return opts;
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
            created.CategoryName = SelectedCategory.Name;
            _allProducts.Add(created);
            ApplyFilter();

            NewProductName = string.Empty;
            NewProductSku = string.Empty;
            NewProductBarcode = string.Empty;
            NewProductPrice = 0;
            IsAddProductVisible = false;
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void SelectProduct(ProductDto product)
    {
        SelectedProduct = product;
        UpdatedPrice = product.UnitPrice;
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

            var index = _allProducts.FindIndex(p => p.Id == updated.Id);
            if (index >= 0)
            {
                updated.CategoryName = _allProducts[index].CategoryName;
                updated.IsActive = _allProducts[index].IsActive;
                _allProducts[index] = updated;
            }
            ApplyFilter();
            SelectedProduct = null;
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeactivateProduct(ProductDto? product = null)
    {
        var target = product ?? SelectedProduct;
        if (target == null) return;

        ErrorMessage = string.Empty;
        try
        {
            var userId = _session.RequireUserId();
            await _catalogService.DeactivateProductAsync(target.Id, userId);
            var idx = _allProducts.FindIndex(p => p.Id == target.Id);
            if (idx >= 0) _allProducts[idx].IsActive = false;
            ApplyFilter();
            if (SelectedProduct?.Id == target.Id) SelectedProduct = null;
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}

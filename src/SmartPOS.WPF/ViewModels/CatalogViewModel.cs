using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Catalog;
using SmartPOS.Shared.DTOs.Product;
using SmartPOS.Shared.Exceptions;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using SmartPOS.Shared.DTOs.Inventory;
using SmartPOS.Data.Repositories.Interfaces;

namespace SmartPOS.WPF.ViewModels;

public partial class CatalogViewModel : ObservableObject
{
    private readonly ICatalogService _catalogService;
    private readonly CurrentSessionContext _session;
    private readonly NavigationService _navigationService;
    private readonly IInventorySyncService _inventorySyncService;

    private readonly IInventorySyncLogRepository _syncLogRepository;

    private List<ProductDto> _allProducts = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _successMessage = string.Empty;
    [ObservableProperty] private string _lastSyncedText = string.Empty;
    [ObservableProperty] private ObservableCollection<CategoryDto> _categories = new();
    [ObservableProperty] private ObservableCollection<ProductDto> _filteredProducts = new();
    [ObservableProperty] private CategoryDto? _selectedCategory;
    [ObservableProperty] private ProductDto? _selectedProduct;

    [ObservableProperty] private string _newCategoryName = string.Empty;
    [ObservableProperty] private string _newProductName = string.Empty;
    [ObservableProperty] private string _newProductSku = string.Empty;
    [ObservableProperty] private string _newProductBarcode = string.Empty;
    [ObservableProperty] private decimal _newProductPrice;
    [ObservableProperty] private int _newProductStock;
    [ObservableProperty] private string? _newProductImagePath;

    [ObservableProperty] private decimal _updatedPrice;
    [ObservableProperty] private int _updatedStock;

    [ObservableProperty] private bool _isAddProductVisible;
    [ObservableProperty] private bool _isSyncing;
    [ObservableProperty] private string _syncStatus = string.Empty;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private string _categoryFilter = "Tất cả";
    [ObservableProperty] private string _statusFilter = "Tất cả";
    [ObservableProperty] private bool _showNavigation = true;

    public int FilteredCount => FilteredProducts.Count;

    public List<string> StatusOptions { get; } = new() { "Tất cả", "Active", "Inactive" };

    [ObservableProperty] private List<string> _categoryFilterOptions = new() { "Tất cả" };

    public CatalogViewModel(ICatalogService catalogService, CurrentSessionContext session, NavigationService navigationService, IInventorySyncService inventorySyncService, IInventorySyncLogRepository syncLogRepository)
    {
        _catalogService = catalogService;
        _session = session;
        _navigationService = navigationService;
        _inventorySyncService = inventorySyncService;
        _syncLogRepository = syncLogRepository;
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilter();
    partial void OnCategoryFilterChanged(string value) => ApplyFilter();
    partial void OnStatusFilterChanged(string value) => ApplyFilter();

    [RelayCommand]
    private void NavigateToSales() => _navigationService.NavigateTo<SalesViewModel>();

    [RelayCommand]
    private void NavigateToCustomer() => _navigationService.NavigateTo<CustomerViewModel>();

    [RelayCommand]
    private void NavigateToSync() => _navigationService.NavigateTo<SyncViewModel>();

    [RelayCommand]
    private void NavigateToReport() => _navigationService.NavigateTo<ReportViewModel>();

    [RelayCommand]
    private void ToggleAddProduct() => IsAddProductVisible = !IsAddProductVisible;

    [RelayCommand]
    private void BrowseImage()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn ảnh sản phẩm",
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|All files|*.*",
            Multiselect = false
        };
        if (dialog.ShowDialog() == true)
            NewProductImagePath = dialog.FileName;
    }

    [RelayCommand]
    private void ClearImage() => NewProductImagePath = null;

    /// <summary>
    /// Fix TASK-0307: chạy Sync Catalog rồi Sync Stock tuần tự
    /// </summary>
    [RelayCommand]
    private async Task SyncFromInventory()
    {
        IsSyncing = true;
        SyncStatus = string.Empty;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        try
        {
            // Bước 1: Sync catalog
            var catalogResult = await _inventorySyncService.SyncCatalogAsync();
            if (catalogResult.Status == "FAILED")
            {
                ErrorMessage = "Sync Catalog thất bại: " + (catalogResult.Message ?? string.Empty);
                return;
            }

            // Bước 2: Sync stock
            var stockResult = await _inventorySyncService.SyncStockAsync();

            // Reload dữ liệu
            await LoadAsync();

            var stockMsg = stockResult.Status == "FAILED"
                ? $" | Sync tồn kho thất bại: {stockResult.Message}"
                : $" | {stockResult.Message}";

            SyncStatus = (catalogResult.Message ?? string.Empty) + stockMsg;
            SuccessMessage = "Đồng bộ hoàn tất!";
            LastSyncedText = $"Last synced: {DateTime.Now:dd/MM/yyyy HH:mm}";

            // Tự động ẩn thông báo sau 8 giây
            _ = Task.Run(async () =>
            {
                await Task.Delay(8000);
                App.Current.Dispatcher.Invoke(() =>
                {
                    SyncStatus = string.Empty;
                    SuccessMessage = string.Empty;
                });
            });
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
        if (IsLoading) return;
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

            var lastSync = await _syncLogRepository.GetLatestAsync("CATALOG");
            if (lastSync != null)
            {
                LastSyncedText = $"Last synced: {lastSync.SyncedAt.ToLocalTime():dd/MM/yyyy HH:mm}";
            }
            else
            {
                LastSyncedText = "Last synced: Chưa từng đồng bộ";
            }
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
        SuccessMessage = string.Empty;
        try
        {
            var userId = _session.RequireUserId();
            await _catalogService.CreateCategoryAsync(new CreateCategoryDto(NewCategoryName, null), userId);
            NewCategoryName = string.Empty;
            await LoadAsync();
            SuccessMessage = "Đã thêm danh mục thành công!";
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task AddProduct()
    {
        if (string.IsNullOrWhiteSpace(NewProductName))
        {
            ErrorMessage = "Tên sản phẩm là bắt buộc.";
            return;
        }

        if (SelectedCategory == null)
        {
            ErrorMessage = "Vui lòng chọn danh mục.";
            return;
        }

        if (NewProductName.Length > 255)
        {
            ErrorMessage = "Tên sản phẩm không được vượt quá 255 ký tự.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewProductSku))
        {
            ErrorMessage = "Mã SKU là bắt buộc.";
            return;
        }

        if (NewProductSku.Length > 50)
        {
            ErrorMessage = "Mã SKU không được vượt quá 50 ký tự.";
            return;
        }

        if (NewProductSku.Contains(" "))
        {
            ErrorMessage = "Mã SKU không được chứa khoảng trắng.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(NewProductBarcode))
        {
            if (NewProductBarcode.Length > 50)
            {
                ErrorMessage = "Barcode không được vượt quá 50 ký tự.";
                return;
            }

            if (!NewProductBarcode.All(char.IsDigit))
            {
                ErrorMessage = "Barcode chỉ được chứa chữ số.";
                return;
            }
        }

        if (NewProductPrice < 0)
        {
            ErrorMessage = "Giá sản phẩm không được âm.";
            return;
        }

        if (NewProductPrice > 1_000_000_000)
        {
            ErrorMessage = "Giá sản phẩm không hợp lý (vượt quá 1 tỷ).";
            return;
        }

        if (NewProductStock < 0)
        {
            ErrorMessage = "Số lượng tồn kho ban đầu không được âm.";
            return;
        }

        if (NewProductStock > 100_000)
        {
            ErrorMessage = "Số lượng tồn kho không được vượt quá 100,000.";
            return;
        }

        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        try
        {
            var dto = new CreateProductDto(
                CategoryId: SelectedCategory.Id,
                Name: NewProductName,
                Sku: NewProductSku,
                UnitPrice: NewProductPrice,
                Barcode: string.IsNullOrWhiteSpace(NewProductBarcode) ? null : NewProductBarcode,
                InitialStock: NewProductStock,
                ImagePath: NewProductImagePath
            );

            var userId = _session.RequireUserId();
            var created = await _catalogService.CreateProductAsync(dto, userId);
            created.CategoryName = SelectedCategory.Name;
            _allProducts.Add(created);
            ApplyFilter();

            NewProductName = string.Empty;
            NewProductSku = string.Empty;
            NewProductBarcode = string.Empty;
            NewProductPrice = 0;
            NewProductStock = 0;
            NewProductImagePath = null;
            IsAddProductVisible = false;
            SuccessMessage = $"Đã thêm sản phẩm '{created.Name}' thành công!";
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
        UpdatedStock = product.LocalStockQuantity;
    }

    [RelayCommand]
    private async Task UpdateProductInfo()
    {
        if (SelectedProduct == null) return;

        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        try
        {
            var userId = _session.RequireUserId();
            ProductDto updated = SelectedProduct;

            bool priceChanged = UpdatedPrice != SelectedProduct.UnitPrice;
            bool stockChanged = UpdatedStock != SelectedProduct.LocalStockQuantity;

            if (priceChanged)
            {
                var dto = new UpdatePriceDto(SelectedProduct.Id, UpdatedPrice);
                updated = await _catalogService.UpdatePriceAsync(dto, userId);
            }

            if (stockChanged)
            {
                updated = await _catalogService.UpdateStockAsync(SelectedProduct.Id, UpdatedStock, userId);
            }

            if (priceChanged || stockChanged)
            {
                var index = _allProducts.FindIndex(p => p.Id == updated.Id);
                if (index >= 0)
                {
                    updated.CategoryName = _allProducts[index].CategoryName;
                    updated.IsActive = _allProducts[index].IsActive;
                    _allProducts[index] = updated;
                }
                ApplyFilter();
                SuccessMessage = $"Đã cập nhật thông tin sản phẩm '{updated.Name}' thành công!";
                SelectedProduct = null;
            }
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
        SuccessMessage = string.Empty;
        try
        {
            var userId = _session.RequireUserId();
            await _catalogService.DeactivateProductAsync(target.Id, userId);
            var idx = _allProducts.FindIndex(p => p.Id == target.Id);
            if (idx >= 0) _allProducts[idx].IsActive = false;
            ApplyFilter();
            if (SelectedProduct?.Id == target.Id) SelectedProduct = null;
            SuccessMessage = $"Đã ngừng kinh doanh sản phẩm '{target.Name}'.";
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ReactivateProduct(ProductDto? product = null)
    {
        var target = product ?? SelectedProduct;
        if (target == null) return;

        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        try
        {
            var userId = _session.RequireUserId();
            await _catalogService.ReactivateProductAsync(target.Id, userId);
            var idx = _allProducts.FindIndex(p => p.Id == target.Id);
            if (idx >= 0) _allProducts[idx].IsActive = true;
            ApplyFilter();
            if (SelectedProduct?.Id == target.Id) SelectedProduct = null;
            SuccessMessage = $"Đã kinh doanh lại sản phẩm '{target.Name}'.";
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteProduct(ProductDto? product = null)
    {
        var target = product ?? SelectedProduct;
        if (target == null) return;

        var result = System.Windows.MessageBox.Show(
            $"Bạn có chắc chắn muốn xóa sản phẩm '{target.Name}' không?",
            "Xác nhận xóa",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        try
        {
            var userId = _session.RequireUserId();
            await _catalogService.DeleteProductAsync(target.Id, userId);
            _allProducts.RemoveAll(p => p.Id == target.Id);
            ApplyFilter();
            if (SelectedProduct?.Id == target.Id) SelectedProduct = null;
            SuccessMessage = $"Đã xóa sản phẩm '{target.Name}'.";
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task UpdateImage()
    {
        if (SelectedProduct == null) return;

        var dialog = new OpenFileDialog
        {
            Title = "Chọn ảnh sản phẩm mới",
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|All files|*.*",
            Multiselect = false
        };
        if (dialog.ShowDialog() == true)
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
            try
            {
                var userId = _session.RequireUserId();
                var updated = await _catalogService.UpdateProductImageAsync(SelectedProduct.Id, dialog.FileName, userId);
                
                var index = _allProducts.FindIndex(p => p.Id == updated.Id);
                if (index >= 0)
                {
                    updated.CategoryName = _allProducts[index].CategoryName;
                    _allProducts[index] = updated;
                }
                ApplyFilter();
                SuccessMessage = $"Đã cập nhật ảnh cho sản phẩm '{SelectedProduct.Name}'!";
                SelectedProduct = null;
            }
            catch (BusinessException ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }
}

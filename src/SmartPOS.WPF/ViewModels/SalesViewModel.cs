using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Cart;
using SmartPOS.Shared.DTOs.Product;
using SmartPOS.Shared.Exceptions;
using SmartPOS.WPF.Navigation;

namespace SmartPOS.WPF.ViewModels;

public partial class SalesViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly ICartService _cartService;
    private readonly NavigationService _navigationService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _barcodeQuery = string.Empty;

    [ObservableProperty]
    private CartSummaryDto _cart = new();

    public ObservableCollection<ProductDto> SearchResults { get; } = new();

    public SalesViewModel(
        IProductService productService,
        ICartService cartService,
        NavigationService navigationService)
    {
        _productService = productService;
        _cartService = cartService;
        _navigationService = navigationService;
        
        RecalculateCart();
    }

    [RelayCommand]
    private async Task SearchProductsAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            var result = await _productService.SearchAsync(SearchQuery).ConfigureAwait(true);
            SearchResults.Clear();
            foreach (var item in result.Products)
            {
                SearchResults.Add(item);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi tìm kiếm: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ScanBarcodeAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(BarcodeQuery))
            return;

        IsLoading = true;
        try
        {
            var product = await _productService.FindByBarcodeAsync(BarcodeQuery).ConfigureAwait(true);
            if (product == null)
            {
                ErrorMessage = $"Không tìm thấy sản phẩm có mã vạch: {BarcodeQuery}";
                return;
            }

            AddToCart(product.Id);
            BarcodeQuery = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void AddToCart(Guid productId)
    {
        ErrorMessage = string.Empty;
        try
        {
            Cart = _cartService.AddItem(productId, 1, Cart);
            OnPropertyChanged(nameof(Cart));
        }
        catch (StockInsufficientException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi: {ex.Message}";
        }
    }

    [RelayCommand]
    private void IncreaseQuantity(CartItemDto item)
    {
        if (item == null) return;
        ErrorMessage = string.Empty;
        try
        {
            Cart = _cartService.UpdateItem(item.ProductId, item.Quantity + 1, Cart);
            OnPropertyChanged(nameof(Cart));
        }
        catch (StockInsufficientException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DecreaseQuantity(CartItemDto item)
    {
        if (item == null) return;
        ErrorMessage = string.Empty;
        try
        {
            Cart = _cartService.UpdateItem(item.ProductId, item.Quantity - 1, Cart);
            OnPropertyChanged(nameof(Cart));
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RemoveItem(CartItemDto item)
    {
        if (item == null) return;
        ErrorMessage = string.Empty;
        try
        {
            Cart = _cartService.RemoveItem(item.ProductId, Cart);
            OnPropertyChanged(nameof(Cart));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearCart()
    {
        ErrorMessage = string.Empty;
        Cart = new CartSummaryDto();
        RecalculateCart();
        OnPropertyChanged(nameof(Cart));
    }

    [RelayCommand]
    private void ClearErrorMessage()
    {
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void Checkout()
    {
        ErrorMessage = string.Empty;
        if (Cart.Items.Count == 0)
        {
            ErrorMessage = "Giỏ hàng đang trống. Không thể thanh toán.";
            return;
        }
        
        _navigationService.NavigateTo<PaymentViewModel>();
    }

    private void RecalculateCart()
    {
        try
        {
            Cart = _cartService.Recalculate(Cart);
            OnPropertyChanged(nameof(Cart));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi tính toán giỏ hàng: {ex.Message}";
        }
    }
}


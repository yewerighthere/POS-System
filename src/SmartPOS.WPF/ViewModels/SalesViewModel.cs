using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Cart;
using SmartPOS.Shared.DTOs.Product;
using SmartPOS.Shared.Exceptions;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;

namespace SmartPOS.WPF.ViewModels;

public partial class SalesViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly ICartService _cartService;
    private readonly NavigationService _navigationService;
    private readonly ICustomerService _customerService;
    private readonly IPromotionService _promotionService;
    private readonly CurrentSessionContext _session;

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

    [ObservableProperty]
    private string _customerInfo = "Khách vãng lai";

    [ObservableProperty]
    private string _promotionInfo = "Không áp dụng";

    public ObservableCollection<ProductDto> SearchResults { get; } = new();

    public SalesViewModel(
        IProductService productService,
        ICartService cartService,
        NavigationService navigationService,
        ICustomerService customerService,
        IPromotionService promotionService,
        CurrentSessionContext session)
    {
        _productService = productService;
        _cartService = cartService;
        _navigationService = navigationService;
        _customerService = customerService;
        _promotionService = promotionService;
        _session = session;

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
            // 1. Kiểm tra xem có phải là mã sản phẩm không
            var product = await _productService.FindByBarcodeAsync(BarcodeQuery).ConfigureAwait(true);
            if (product != null)
            {
                AddToCart(product.Id);
                BarcodeQuery = string.Empty;
                return;
            }

            // 2. Kiểm tra nếu là số điện thoại (từ 9 đến 11 số)
            bool isPhone = System.Text.RegularExpressions.Regex.IsMatch(BarcodeQuery, @"^(0|\+84|84)\d{8,10}$");
            if (isPhone)
            {
                // MOCK DATA: Giả lập thông tin khách hàng để test (không sửa file của người khác)
                if (BarcodeQuery == "0987654321")
                {
                    CustomerInfo = "Nguyễn Văn A (0987654321)";
                    BarcodeQuery = string.Empty;
                    return;
                }
                else if (BarcodeQuery == "0912345678")
                {
                    CustomerInfo = "Trần Thị B (0912345678)";
                    BarcodeQuery = string.Empty;
                    return;
                }

                // Gọi service thật (an toàn nếu chưa cài đặt)
                try
                {
                    var customer = await _customerService.FindByPhoneAsync(BarcodeQuery).ConfigureAwait(true);
                    if (customer != null)
                    {
                        CustomerInfo = $"{customer.FullName} ({customer.Phone})";
                        BarcodeQuery = string.Empty;
                        return;
                    }
                }
                catch (NotImplementedException) { }

                ErrorMessage = $"Không tìm thấy khách hàng với SĐT: {BarcodeQuery}";
                return;
            }

            // 3. Kiểm tra xem có phải là mã khuyến mãi không
            // MOCK DATA: Giả lập mã khuyến mãi để test (không sửa file của người khác)
            if (BarcodeQuery.Equals("KM10", StringComparison.OrdinalIgnoreCase))
            {
                decimal discount = Math.Round(Cart.Subtotal * 0.1m);
                Cart.DiscountAmount = discount;
                UpdateCart(_cartService.Recalculate(Cart));
                PromotionInfo = $"KM10 (-{discount:N0} đ)";
                BarcodeQuery = string.Empty;
                return;
            }
            else if (BarcodeQuery.Equals("KM20", StringComparison.OrdinalIgnoreCase))
            {
                decimal discount = Math.Round(Cart.Subtotal * 0.2m);
                Cart.DiscountAmount = discount;
                UpdateCart(_cartService.Recalculate(Cart));
                PromotionInfo = $"KM20 (-{discount:N0} đ)";
                BarcodeQuery = string.Empty;
                return;
            }

            // Gọi service thật (an toàn nếu chưa cài đặt)
            try
            {
                var validation = await _promotionService.ValidateCodeAsync(BarcodeQuery, Cart).ConfigureAwait(true);
                if (validation.IsValid)
                {
                    UpdateCart(await _promotionService.ApplyPromotionAsync(BarcodeQuery, Cart).ConfigureAwait(true));
                    PromotionInfo = $"{BarcodeQuery} (-{validation.DiscountAmount:N0} đ)";
                    BarcodeQuery = string.Empty;
                    return;
                }
                else if (!string.IsNullOrEmpty(validation.Message) && validation.Message != "Promotion not found")
                {
                    ErrorMessage = $"Mã khuyến mãi không khả dụng: {validation.Message}";
                    return;
                }
            }
            catch (NotImplementedException) { }

            // 4. Nếu không khớp với bất kỳ trường hợp nào
            ErrorMessage = $"Không tìm thấy sản phẩm, số điện thoại hoặc mã khuyến mãi phù hợp cho: {BarcodeQuery}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi xử lý: {ex.Message}";
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
            UpdateCart(_cartService.AddItem(productId, 1, Cart));
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
            UpdateCart(_cartService.UpdateItem(item.ProductId, item.Quantity + 1, Cart));
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
            UpdateCart(_cartService.UpdateItem(item.ProductId, item.Quantity - 1, Cart));
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
            UpdateCart(_cartService.RemoveItem(item.ProductId, Cart));
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
    }

    [RelayCommand]
    private void CancelOrder()
    {
        ErrorMessage = string.Empty;
        Cart = new CartSummaryDto();
        RecalculateCart();
        CustomerInfo = "Khách vãng lai";
        PromotionInfo = "Không áp dụng";
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

        _session.CurrentCart = Cart;
        _navigationService.NavigateTo<PaymentViewModel>();
    }

    [RelayCommand]
    private void NavigateToShift()
    {
        _navigationService.NavigateTo<ShiftViewModel>();
    }

    private void UpdateCart(CartSummaryDto newCart)
    {
        if (newCart != null)
        {
            newCart.Items = new ObservableCollection<CartItemDto>(newCart.Items);
        }
        Cart = newCart ?? new CartSummaryDto();
        OnPropertyChanged(nameof(Cart));
    }

    private void RecalculateCart()
    {
        try
        {
            UpdateCart(_cartService.Recalculate(Cart));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi tính toán giỏ hàng: {ex.Message}";
        }
    }
}


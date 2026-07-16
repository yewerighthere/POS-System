using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Cart;
using SmartPOS.Shared.DTOs.Product;
using SmartPOS.Shared.Exceptions;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;
using SmartPOS.Shared.DTOs.Promotion;

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

    [ObservableProperty]
    private bool _isCustomerNotFoundPopupOpen;

    [ObservableProperty]
    private bool _isCreateCustomerPopupOpen;

    [ObservableProperty]
    private string _newCustomerPhone = string.Empty;

    [ObservableProperty]
    private string _newCustomerName = string.Empty;

    [ObservableProperty]
    private string _newCustomerEmail = string.Empty;

    [ObservableProperty]
    private bool _isPromoPopupOpen;

    [ObservableProperty]
    private string _promoCodeInput = string.Empty;

    public ObservableCollection<PromotionDto> AvailablePromotions { get; } = new();

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
                AddToCart(product);
                BarcodeQuery = string.Empty;
                return;
            }

            // 2. Kiểm tra nếu là số điện thoại (từ 9 đến 11 số)
            bool isPhone = System.Text.RegularExpressions.Regex.IsMatch(BarcodeQuery, @"^(0|\+84|84)\d{8,10}$");
            if (isPhone)
            {
                // Gọi service thật
                try
                {
                    var customer = await _customerService.FindByPhoneAsync(BarcodeQuery).ConfigureAwait(true);
                    if (customer != null)
                    {
                        CustomerInfo = $"{customer.FullName} ({customer.Phone})";
                        Cart.Customer = customer;
                        RecalculateCart();
                        BarcodeQuery = string.Empty;
                        return;
                    }
                    else
                    {
                        NewCustomerPhone = BarcodeQuery;
                        IsCustomerNotFoundPopupOpen = true;
                        BarcodeQuery = string.Empty;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Lỗi tìm khách hàng: {ex.Message}";
                    return;
                }
            }
            // Không xử lý Khuyến mãi ở đây nữa, sẽ dùng Popup riêng


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
    private void AddToCart(ProductDto product)
    {
        if (product == null) return;
        ErrorMessage = string.Empty;
        try
        {
            UpdateCart(_cartService.AddItem(product, 1, Cart));
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
    private async Task IncreaseQuantityAsync(CartItemDto item)
    {
        if (item == null) return;
        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            var product = await _productService.FindByIdAsync(item.ProductId).ConfigureAwait(true);
            if (product == null)
            {
                ErrorMessage = "Sản phẩm không tồn tại.";
                return;
            }

            UpdateCart(_cartService.UpdateItem(product, item.Quantity + 1, Cart));
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
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DecreaseQuantityAsync(CartItemDto item)
    {
        if (item == null) return;
        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            var product = await _productService.FindByIdAsync(item.ProductId).ConfigureAwait(true);
            if (product == null)
            {
                ErrorMessage = "Sản phẩm không tồn tại.";
                return;
            }

            UpdateCart(_cartService.UpdateItem(product, item.Quantity - 1, Cart));
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
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

    [RelayCommand]
    private void NavigateToCustomer()
    {
        _navigationService.NavigateTo<CustomerViewModel>();
    }

    private void UpdateCart(CartSummaryDto newCart)
    {
        if (newCart != null)
        {
            newCart.Items = new List<CartItemDto>(newCart.Items);
        }
        Cart = newCart ?? new CartSummaryDto();

        if (Cart.AppliedPromotion != null)
        {
            if (Cart.DiscountAmount == 0 && Cart.AppliedPromotion.MinOrderAmount.HasValue && Cart.Subtotal < Cart.AppliedPromotion.MinOrderAmount.Value)
            {
                Cart.AppliedPromotion = null;
                PromotionInfo = "Không áp dụng";
            }
            else
            {
                PromotionInfo = $"{Cart.AppliedPromotion.Code} (-{Cart.DiscountAmount:N0} đ)";
            }
        }

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

    [RelayCommand]
    private void ShowCreateCustomerForm()
    {
        IsCustomerNotFoundPopupOpen = false;
        NewCustomerName = string.Empty;
        NewCustomerEmail = string.Empty;
        IsCreateCustomerPopupOpen = true;
    }

    [RelayCommand]
    private void CloseCustomerPopups()
    {
        IsCustomerNotFoundPopupOpen = false;
        IsCreateCustomerPopupOpen = false;
    }

    [RelayCommand]
    private async Task ConfirmCreateCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCustomerName))
        {
            ErrorMessage = "Vui lòng nhập tên khách hàng";
            return;
        }

        IsLoading = true;
        try
        {
            var dto = new SmartPOS.Shared.DTOs.Customer.CreateCustomerDto(NewCustomerName, NewCustomerPhone, string.IsNullOrWhiteSpace(NewCustomerEmail) ? null : NewCustomerEmail);
            var customer = await _customerService.CreateAsync(dto).ConfigureAwait(true);
            CustomerInfo = $"{customer.FullName} ({customer.Phone})";
            Cart.Customer = customer;
            RecalculateCart();
            CloseCustomerPopups();
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
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
    private void RemoveCustomer()
    {
        CustomerInfo = "Khách vãng lai";
        Cart.Customer = null;
        Cart.PointsUsed = 0;
        Cart.PointsDiscountAmount = 0;
        RecalculateCart();
    }

    [RelayCommand]
    private void ToggleUsePoints()
    {
        if (Cart.Customer == null || Cart.Customer.LoyaltyPoints <= 0) return;
        
        if (Cart.PointsUsed > 0)
        {
            Cart.PointsUsed = 0;
            Cart.PointsDiscountAmount = 0;
        }
        else
        {
            var maxPoints = Cart.Customer.LoyaltyPoints;
            var currentSubtotalAfterPromo = Cart.Subtotal - Cart.DiscountAmount;
            
            if (maxPoints > currentSubtotalAfterPromo)
            {
                Cart.PointsUsed = (int)Math.Floor(currentSubtotalAfterPromo);
                Cart.PointsDiscountAmount = currentSubtotalAfterPromo;
            }
            else
            {
                Cart.PointsUsed = maxPoints;
                Cart.PointsDiscountAmount = maxPoints;
            }
        }
        
        RecalculateCart();
    }

    [RelayCommand]
    private async Task OpenPromoPopupAsync()
    {
        ErrorMessage = string.Empty;
        IsPromoPopupOpen = true;
        PromoCodeInput = string.Empty;

        try
        {
            var promotions = await _promotionService.GetActiveAsync(DateOnly.FromDateTime(DateTime.Today)).ConfigureAwait(true);
            AvailablePromotions.Clear();
            if (promotions != null)
            {
                foreach (var p in promotions)
                {
                    AvailablePromotions.Add(p);
                }
            }
        }
        catch (Exception ex)
        {
            // Bỏ qua lỗi nếu không load được gợi ý
        }
    }

    [RelayCommand]
    private void ClosePromoPopup()
    {
        IsPromoPopupOpen = false;
        PromoCodeInput = string.Empty;
    }

    [RelayCommand]
    private async Task ApplyPromoAsync(string code)
    {
        ErrorMessage = string.Empty;
        var codeToApply = string.IsNullOrWhiteSpace(code) ? PromoCodeInput : code;
        
        if (string.IsNullOrWhiteSpace(codeToApply))
        {
            ErrorMessage = "Vui lòng nhập mã khuyến mãi.";
            return;
        }

        IsLoading = true;
        try
        {
            var validation = await _promotionService.ValidateCodeAsync(codeToApply, Cart).ConfigureAwait(true);
            if (validation.IsValid)
            {
                var appliedCart = await _promotionService.ApplyPromotionAsync(codeToApply, Cart).ConfigureAwait(true);
                UpdateCart(_cartService.Recalculate(appliedCart));
                ClosePromoPopup();
            }
            else
            {
                ErrorMessage = $"Mã khuyến mãi không khả dụng: {validation.Message}";
            }
        }
        catch (NotImplementedException) 
        {
            ErrorMessage = "Dịch vụ khuyến mãi chưa được hỗ trợ hoàn toàn.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi áp dụng khuyến mãi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void RemovePromo()
    {
        ErrorMessage = string.Empty;
        Cart.DiscountAmount = 0;
        Cart.AppliedPromotion = null;
        UpdateCart(_cartService.Recalculate(Cart));
    }
}


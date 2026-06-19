using Microsoft.Extensions.Logging;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Cart;
using SmartPOS.Shared.Exceptions;

namespace SmartPOS.Services.Implementations;

public class CartService : ICartService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<CartService> _logger;

    public CartService(IProductRepository productRepository, ILogger<CartService> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public CartSummaryDto AddItem(Guid productId, int quantity, CartSummaryDto cart)
    {
        _logger.LogInformation("Adding product {ProductId} with quantity {Quantity} to cart", productId, quantity);
        
        if (quantity <= 0)
            throw new BusinessException("Số lượng sản phẩm thêm vào giỏ hàng phải lớn hơn 0.");

        var product = _productRepository.GetByIdAsync(productId).ConfigureAwait(false).GetAwaiter().GetResult();
        if (product == null)
            throw new BusinessException("Sản phẩm không tồn tại.");

        if (!product.IsActive)
            throw new BusinessException("Sản phẩm hiện đang ngưng hoạt động.");

        // Check if item already in cart
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        var currentQuantityInCart = existingItem?.Quantity ?? 0;
        var newQuantity = currentQuantityInCart + quantity;

        // Check stock
        if (newQuantity > product.LocalStockQuantity)
        {
            throw new StockInsufficientException($"Sản phẩm '{product.Name}' không đủ tồn kho. Tồn kho khả dụng: {product.LocalStockQuantity}, trong giỏ đã có: {currentQuantityInCart}, muốn thêm: {quantity}.");
        }

        if (existingItem != null)
        {
            existingItem.Quantity = newQuantity;
            existingItem.Subtotal = existingItem.Quantity * existingItem.UnitPrice;
        }
        else
        {
            cart.Items.Add(new CartItemDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.UnitPrice,
                Quantity = quantity,
                Subtotal = quantity * product.UnitPrice
            });
        }

        return Recalculate(cart);
    }

    public CartSummaryDto UpdateItem(Guid productId, int quantity, CartSummaryDto cart)
    {
        _logger.LogInformation("Updating product {ProductId} quantity to {Quantity} in cart", productId, quantity);
        
        if (quantity <= 0)
        {
            return RemoveItem(productId, cart);
        }

        var product = _productRepository.GetByIdAsync(productId).ConfigureAwait(false).GetAwaiter().GetResult();
        if (product == null)
            throw new BusinessException("Sản phẩm không tồn tại.");

        if (!product.IsActive)
            throw new BusinessException("Sản phẩm hiện đang ngưng hoạt động.");

        // Check stock
        if (quantity > product.LocalStockQuantity)
        {
            throw new StockInsufficientException($"Sản phẩm '{product.Name}' không đủ tồn kho. Tồn kho khả dụng: {product.LocalStockQuantity}, muốn cập nhật thành: {quantity}.");
        }

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem == null)
        {
            cart.Items.Add(new CartItemDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.UnitPrice,
                Quantity = quantity,
                Subtotal = quantity * product.UnitPrice
            });
        }
        else
        {
            existingItem.Quantity = quantity;
            existingItem.Subtotal = quantity * existingItem.UnitPrice;
        }

        return Recalculate(cart);
    }

    public CartSummaryDto RemoveItem(Guid productId, CartSummaryDto cart)
    {
        _logger.LogInformation("Removing product {ProductId} from cart", productId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            cart.Items.Remove(item);
        }
        return Recalculate(cart);
    }

    public CartSummaryDto Recalculate(CartSummaryDto cart)
    {
        _logger.LogInformation("Recalculating cart summary");
        
        decimal subtotal = 0;
        decimal taxAmount = 0;

        foreach (var item in cart.Items)
        {
            var product = _productRepository.GetByIdAsync(item.ProductId).ConfigureAwait(false).GetAwaiter().GetResult();
            if (product != null)
            {
                item.ProductName = product.Name;
                item.UnitPrice = product.UnitPrice;
                item.Subtotal = item.Quantity * item.UnitPrice;

                subtotal += item.Subtotal;
                taxAmount += item.Subtotal * product.TaxRate;
            }
            else
            {
                subtotal += item.Subtotal;
            }
        }

        cart.Subtotal = subtotal;
        cart.TaxAmount = taxAmount;
        cart.TotalAmount = subtotal + taxAmount - cart.DiscountAmount;
        
        if (cart.TotalAmount < 0)
        {
            cart.TotalAmount = 0;
        }

        return cart;
    }
}


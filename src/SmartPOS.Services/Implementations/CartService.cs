using Microsoft.Extensions.Logging;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Auth;
using SmartPOS.Shared.DTOs.Shift;
using SmartPOS.Shared.DTOs.Product;
using SmartPOS.Shared.DTOs.Cart;
using SmartPOS.Shared.DTOs.Order;
using SmartPOS.Shared.DTOs.Payment;
using SmartPOS.Shared.DTOs.Invoice;
using SmartPOS.Shared.DTOs.Customer;
using SmartPOS.Shared.DTOs.Return;
using SmartPOS.Shared.DTOs.Catalog;
using SmartPOS.Shared.DTOs.Inventory;
using SmartPOS.Shared.DTOs.Report;
using SmartPOS.Shared.DTOs.Promotion;
using SmartPOS.Shared.Enums;
using SmartPOS.Shared.Exceptions;

namespace SmartPOS.Services.Implementations;

public class CartService : ICartService
{
    public CartSummaryDto AddItem(ProductDto product, int quantity, CartSummaryDto cart)
    {
        var items = CloneItems(cart);
        var existing = items.FirstOrDefault(i => i.ProductId == product.Id);
        var currentQty = existing?.Quantity ?? 0;
        var newQty = currentQty + quantity;

        if (product.LocalStockQuantity > 0 && newQty > product.LocalStockQuantity)
            throw new StockInsufficientException($"Sản phẩm \"{product.Name}\" không đủ hàng. Tồn kho: {product.LocalStockQuantity}");

        if (existing != null)
        {
            existing.Quantity = newQty;
            existing.Subtotal = existing.UnitPrice * newQty;
        }
        else
        {
            items.Add(new CartItemDto
            {
                ProductId = product.Id,
                ProductSku = product.Sku,
                ProductName = product.Name,
                UnitPrice = product.UnitPrice,
                Quantity = quantity,
                Subtotal = product.UnitPrice * quantity,
                ProductImagePath = product.ImagePath
            });
        }

        return Recalculate(new CartSummaryDto { Items = items, DiscountAmount = cart.DiscountAmount });
    }

    public CartSummaryDto UpdateItem(Guid productId, int quantity, CartSummaryDto cart)
    {
        var items = CloneItems(cart);
        var item = items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return cart;

        if (quantity <= 0)
            items.Remove(item);
        else
        {
            item.Quantity = quantity;
            item.Subtotal = item.UnitPrice * quantity;
        }

        return Recalculate(new CartSummaryDto { Items = items, DiscountAmount = cart.DiscountAmount });
    }

    public CartSummaryDto RemoveItem(Guid productId, CartSummaryDto cart)
    {
        var items = CloneItems(cart).Where(i => i.ProductId != productId).ToList();
        return Recalculate(new CartSummaryDto { Items = items, DiscountAmount = cart.DiscountAmount });
    }

    public CartSummaryDto Recalculate(CartSummaryDto cart)
    {
        var subtotal = cart.Items.Sum(i => i.Subtotal);
        var discount = Math.Min(cart.DiscountAmount, subtotal);
        return new CartSummaryDto
        {
            Items = cart.Items,
            Subtotal = subtotal,
            DiscountAmount = discount,
            TaxAmount = 0,
            TotalAmount = Math.Max(0, subtotal - discount)
        };
    }

    private static List<CartItemDto> CloneItems(CartSummaryDto cart) =>
        cart.Items.Select(i => new CartItemDto
        {
            ProductId = i.ProductId,
            ProductSku = i.ProductSku,
            ProductName = i.ProductName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            Subtotal = i.Subtotal,
            ProductImagePath = i.ProductImagePath
        }).ToList();
}


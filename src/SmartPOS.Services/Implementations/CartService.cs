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
        if (!product.IsActive)
            throw new BusinessException("Sản phẩm đã ngừng kinh doanh");

        if (product.LocalStockQuantity <= 0)
            throw new StockInsufficientException("Sản phẩm đã hết hàng");

        var items = CloneItems(cart);
        var existing = items.FirstOrDefault(i => i.ProductId == product.Id);
        var currentQty = existing?.Quantity ?? 0;
        var newQty = currentQty + quantity;

        if (newQty > product.LocalStockQuantity)
            throw new StockInsufficientException("Số lượng vượt quá tồn kho hiện có");

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

        return Recalculate(new CartSummaryDto { 
            Items = items, 
            DiscountAmount = cart.DiscountAmount,
            Customer = cart.Customer,
            PointsUsed = cart.PointsUsed,
            PointsDiscountAmount = cart.PointsDiscountAmount,
            AppliedPromotion = cart.AppliedPromotion
        });
    }

    public CartSummaryDto UpdateItem(ProductDto product, int quantity, CartSummaryDto cart)
    {
        if (!product.IsActive)
            throw new BusinessException("Sản phẩm đã ngừng kinh doanh");

        if (quantity > 0)
        {
            if (product.LocalStockQuantity <= 0)
                throw new StockInsufficientException("Sản phẩm đã hết hàng");

            if (quantity > product.LocalStockQuantity)
                throw new StockInsufficientException("Số lượng vượt quá tồn kho hiện có");
        }

        var items = CloneItems(cart);
        var item = items.FirstOrDefault(i => i.ProductId == product.Id);
        if (item == null) return cart;

        if (quantity <= 0)
            items.Remove(item);
        else
        {
            item.Quantity = quantity;
            item.Subtotal = item.UnitPrice * quantity;
        }

        return Recalculate(new CartSummaryDto { 
            Items = items, 
            DiscountAmount = cart.DiscountAmount,
            Customer = cart.Customer,
            PointsUsed = cart.PointsUsed,
            PointsDiscountAmount = cart.PointsDiscountAmount,
            AppliedPromotion = cart.AppliedPromotion
        });
    }

    public CartSummaryDto RemoveItem(Guid productId, CartSummaryDto cart)
    {
        var items = CloneItems(cart).Where(i => i.ProductId != productId).ToList();
        return Recalculate(new CartSummaryDto { 
            Items = items, 
            DiscountAmount = cart.DiscountAmount,
            Customer = cart.Customer,
            PointsUsed = cart.PointsUsed,
            PointsDiscountAmount = cart.PointsDiscountAmount,
            AppliedPromotion = cart.AppliedPromotion
        });
    }

    public CartSummaryDto Recalculate(CartSummaryDto cart)
    {
        var subtotal = cart.Items.Sum(i => i.Subtotal);
        
        decimal discount = 0;
        if (cart.AppliedPromotion != null)
        {
            if (cart.AppliedPromotion.MinOrderAmount.HasValue && subtotal < cart.AppliedPromotion.MinOrderAmount.Value)
            {
                discount = 0;
            }
            else
            {
                if (cart.AppliedPromotion.Type.Contains("Percentage", StringComparison.OrdinalIgnoreCase))
                {
                    discount = subtotal * (cart.AppliedPromotion.DiscountValue / 100m);
                }
                else
                {
                    discount = cart.AppliedPromotion.DiscountValue;
                }
            }
        }
        else
        {
            discount = cart.DiscountAmount;
        }

        discount = Math.Min(discount, subtotal);
        var pointsDiscount = Math.Min(cart.PointsDiscountAmount, subtotal - discount);
        var pointsEarned = (int)Math.Floor(subtotal / 10000m);
        var taxableAmount = Math.Max(0, subtotal - discount);
        
        var taxAmount = Math.Round(taxableAmount * 0.03m, 2, MidpointRounding.AwayFromZero);
        var totalAmount = taxableAmount + taxAmount;
        return new CartSummaryDto
        {
            Items = cart.Items,
            Customer = cart.Customer,
            Subtotal = subtotal,
            DiscountAmount = discount,
            PointsDiscountAmount = pointsDiscount,
            PointsUsed = cart.PointsUsed,
            PointsEarned = pointsEarned,
            TaxAmount = taxAmount,
            TotalAmount = Math.Max(0, subtotal - discount - pointsDiscount + taxAmount),
            AppliedPromotion = cart.AppliedPromotion
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


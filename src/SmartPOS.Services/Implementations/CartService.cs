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

namespace SmartPOS.Services.Implementations;

public class CartService : ICartService
{
    private readonly ILogger<CartService> _logger;

    public CartService(ILogger<CartService> logger)
    {
        _logger = logger;
    }

    public CartSummaryDto AddItem(Guid productId, int quantity, CartSummaryDto cart)
    {
        throw new NotImplementedException();
    }

    public CartSummaryDto UpdateItem(Guid productId, int quantity, CartSummaryDto cart)
    {
        throw new NotImplementedException();
    }

    public CartSummaryDto RemoveItem(Guid productId, CartSummaryDto cart)
    {
        throw new NotImplementedException();
    }

    public CartSummaryDto Recalculate(CartSummaryDto cart)
    {
        throw new NotImplementedException();
    }
}


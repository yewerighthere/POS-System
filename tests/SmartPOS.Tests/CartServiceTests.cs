using Xunit;
using SmartPOS.Services.Implementations;
using SmartPOS.Shared.DTOs.Product;
using SmartPOS.Shared.DTOs.Cart;
using SmartPOS.Shared.Exceptions;
using System;
using System.Linq;

namespace SmartPOS.Tests;

public class CartServiceTests
{
    private readonly CartService _cartService;

    public CartServiceTests()
    {
        _cartService = new CartService();
    }

    [Fact]
    public void AddItem_Success_CalculatesTaxAndTotal()
    {
        // Arrange
        var product = new ProductDto
        {
            Id = Guid.NewGuid(),
            Name = "Coca Cola",
            Sku = "COCA",
            UnitPrice = 10000,
            LocalStockQuantity = 10,
            IsActive = true
        };
        var cart = new CartSummaryDto();

        // Act
        var result = _cartService.AddItem(product, 2, cart);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(20000, result.Subtotal);
        Assert.Equal(2000, result.TaxAmount); // 10% of 20000
        Assert.Equal(22000, result.TotalAmount); // 20000 + 2000
    }

    [Fact]
    public void AddItem_InactiveProduct_ThrowsBusinessException()
    {
        // Arrange
        var product = new ProductDto
        {
            Id = Guid.NewGuid(),
            Name = "Expired Drink",
            Sku = "EXP",
            UnitPrice = 10000,
            LocalStockQuantity = 10,
            IsActive = false
        };
        var cart = new CartSummaryDto();

        // Act & Assert
        var ex = Assert.Throws<BusinessException>(() => _cartService.AddItem(product, 1, cart));
        Assert.Equal("Sản phẩm đã ngừng kinh doanh", ex.Message);
    }

    [Fact]
    public void AddItem_OutOfStockProduct_ThrowsStockInsufficientException()
    {
        // Arrange
        var product = new ProductDto
        {
            Id = Guid.NewGuid(),
            Name = "Out of Stock Drink",
            Sku = "OOS",
            UnitPrice = 10000,
            LocalStockQuantity = 0,
            IsActive = true
        };
        var cart = new CartSummaryDto();

        // Act & Assert
        var ex = Assert.Throws<StockInsufficientException>(() => _cartService.AddItem(product, 1, cart));
        Assert.Equal("Sản phẩm đã hết hàng", ex.Message);
    }

    [Fact]
    public void AddItem_QuantityExceedsStock_ThrowsStockInsufficientException()
    {
        // Arrange
        var product = new ProductDto
        {
            Id = Guid.NewGuid(),
            Name = "Low Stock Drink",
            Sku = "LOW",
            UnitPrice = 10000,
            LocalStockQuantity = 3,
            IsActive = true
        };
        var cart = new CartSummaryDto();

        // Act & Assert
        var ex = Assert.Throws<StockInsufficientException>(() => _cartService.AddItem(product, 4, cart));
        Assert.Equal("Số lượng vượt quá tồn kho hiện có", ex.Message);
    }

    [Fact]
    public void UpdateItem_IncreaseQuantity_Success()
    {
        // Arrange
        var product = new ProductDto
        {
            Id = Guid.NewGuid(),
            Name = "Coca Cola",
            Sku = "COCA",
            UnitPrice = 10000,
            LocalStockQuantity = 10,
            IsActive = true
        };
        var cart = new CartSummaryDto();
        cart = _cartService.AddItem(product, 2, cart);

        // Act
        var result = _cartService.UpdateItem(product, 5, cart);

        // Assert
        Assert.Equal(5, result.Items.First().Quantity);
        Assert.Equal(50000, result.Subtotal);
        Assert.Equal(5000, result.TaxAmount);
        Assert.Equal(55000, result.TotalAmount);
    }

    [Fact]
    public void UpdateItem_ExceedsStock_ThrowsStockInsufficientException()
    {
        // Arrange
        var product = new ProductDto
        {
            Id = Guid.NewGuid(),
            Name = "Coca Cola",
            Sku = "COCA",
            UnitPrice = 10000,
            LocalStockQuantity = 5,
            IsActive = true
        };
        var cart = new CartSummaryDto();
        cart = _cartService.AddItem(product, 2, cart);

        // Act & Assert
        var ex = Assert.Throws<StockInsufficientException>(() => _cartService.UpdateItem(product, 6, cart));
        Assert.Equal("Số lượng vượt quá tồn kho hiện có", ex.Message);
    }

    [Fact]
    public void UpdateItem_ZeroQuantity_RemovesItem()
    {
        // Arrange
        var product = new ProductDto
        {
            Id = Guid.NewGuid(),
            Name = "Coca Cola",
            Sku = "COCA",
            UnitPrice = 10000,
            LocalStockQuantity = 10,
            IsActive = true
        };
        var cart = new CartSummaryDto();
        cart = _cartService.AddItem(product, 2, cart);

        // Act
        var result = _cartService.UpdateItem(product, 0, cart);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Subtotal);
        Assert.Equal(0, result.TaxAmount);
        Assert.Equal(0, result.TotalAmount);
    }

    [Fact]
    public void Recalculate_WithDiscount_CalculatesTaxOnDiscountedAmount()
    {
        // Arrange
        var product = new ProductDto
        {
            Id = Guid.NewGuid(),
            Name = "Coca Cola",
            Sku = "COCA",
            UnitPrice = 10000,
            LocalStockQuantity = 10,
            IsActive = true
        };
        var cart = new CartSummaryDto { DiscountAmount = 5000 };
        cart = _cartService.AddItem(product, 2, cart); // subtotal = 20000

        // Act
        var result = _cartService.Recalculate(cart);

        // Assert
        Assert.Equal(20000, result.Subtotal);
        Assert.Equal(5000, result.DiscountAmount);
        Assert.Equal(1500, result.TaxAmount); // (20000 - 5000) * 10%
        Assert.Equal(16500, result.TotalAmount); // (20000 - 5000) + 1500
    }
}

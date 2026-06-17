using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Implementations;
using SmartPOS.Shared.DTOs.Cart;
using SmartPOS.Shared.Exceptions;
using Xunit;

namespace SmartPOS.Tests;

public class CartServiceTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<ILogger<CartService>> _loggerMock;
    private readonly CartService _cartService;

    public CartServiceTests()
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _loggerMock = new Mock<ILogger<CartService>>();
        _cartService = new CartService(_productRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void AddItem_ShouldAddProductToCart_WhenStockIsSufficient()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Sku = "TEST-123",
            UnitPrice = 10000,
            LocalStockQuantity = 10,
            IsActive = true,
            TaxRate = 0.1m
        };

        _productRepositoryMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);

        var cart = new CartSummaryDto();

        // Act
        var result = _cartService.AddItem(productId, 2, cart);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].ProductId.Should().Be(productId);
        result.Items[0].Quantity.Should().Be(2);
        result.Items[0].Subtotal.Should().Be(20000);
        result.Subtotal.Should().Be(20000);
        result.TaxAmount.Should().Be(2000); // 10% of 20000
        result.TotalAmount.Should().Be(22000);
    }

    [Fact]
    public void AddItem_ShouldIncreaseQuantity_WhenProductAlreadyInCart()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Sku = "TEST-123",
            UnitPrice = 10000,
            LocalStockQuantity = 10,
            IsActive = true,
            TaxRate = 0.1m
        };

        _productRepositoryMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);

        var cart = new CartSummaryDto();
        cart.Items.Add(new CartItemDto
        {
            ProductId = productId,
            ProductName = "Test Product",
            UnitPrice = 10000,
            Quantity = 2,
            Subtotal = 20000
        });

        // Act
        var result = _cartService.AddItem(productId, 3, cart);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Quantity.Should().Be(5);
        result.Items[0].Subtotal.Should().Be(50000);
        result.Subtotal.Should().Be(50000);
        result.TaxAmount.Should().Be(5000);
        result.TotalAmount.Should().Be(55000);
    }

    [Fact]
    public void AddItem_ShouldThrowStockInsufficientException_WhenRequestedQuantityExceedsStock()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Sku = "TEST-123",
            UnitPrice = 10000,
            LocalStockQuantity = 4,
            IsActive = true,
            TaxRate = 0.1m
        };

        _productRepositoryMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);

        var cart = new CartSummaryDto();

        // Act
        Action act = () => _cartService.AddItem(productId, 5, cart);

        // Assert
        act.Should().Throw<StockInsufficientException>()
            .WithMessage("*không đủ tồn kho*");
    }

    [Fact]
    public void AddItem_ShouldThrowBusinessException_WhenProductIsInactive()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Inactive Product",
            Sku = "TEST-123",
            UnitPrice = 10000,
            LocalStockQuantity = 10,
            IsActive = false,
            TaxRate = 0.1m
        };

        _productRepositoryMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);

        var cart = new CartSummaryDto();

        // Act
        Action act = () => _cartService.AddItem(productId, 1, cart);

        // Assert
        act.Should().Throw<BusinessException>()
            .WithMessage("*ngưng hoạt động*");
    }

    [Fact]
    public void UpdateItem_ShouldUpdateQuantity_WhenStockIsSufficient()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Sku = "TEST-123",
            UnitPrice = 10000,
            LocalStockQuantity = 10,
            IsActive = true,
            TaxRate = 0.1m
        };

        _productRepositoryMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);

        var cart = new CartSummaryDto();
        cart.Items.Add(new CartItemDto
        {
            ProductId = productId,
            ProductName = "Test Product",
            UnitPrice = 10000,
            Quantity = 2,
            Subtotal = 20000
        });

        // Act
        var result = _cartService.UpdateItem(productId, 6, cart);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Quantity.Should().Be(6);
        result.Items[0].Subtotal.Should().Be(60000);
        result.TotalAmount.Should().Be(66000);
    }

    [Fact]
    public void UpdateItem_ShouldRemoveItem_WhenQuantityIsZeroOrNegative()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var cart = new CartSummaryDto();
        cart.Items.Add(new CartItemDto
        {
            ProductId = productId,
            ProductName = "Test Product",
            UnitPrice = 10000,
            Quantity = 2,
            Subtotal = 20000
        });

        // Act
        var result = _cartService.UpdateItem(productId, 0, cart);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalAmount.Should().Be(0);
    }

    [Fact]
    public void RemoveItem_ShouldRemoveProductFromCart()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var cart = new CartSummaryDto();
        cart.Items.Add(new CartItemDto
        {
            ProductId = productId,
            ProductName = "Test Product",
            UnitPrice = 10000,
            Quantity = 2,
            Subtotal = 20000
        });

        // Act
        var result = _cartService.RemoveItem(productId, cart);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalAmount.Should().Be(0);
    }

    [Fact]
    public void Recalculate_ShouldUpdateTotalsAndTaxCorrectly()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var product1 = new Product
        {
            Id = productId1,
            Name = "Product 1",
            UnitPrice = 10000,
            LocalStockQuantity = 10,
            IsActive = true,
            TaxRate = 0.1m
        };

        var productId2 = Guid.NewGuid();
        var product2 = new Product
        {
            Id = productId2,
            Name = "Product 2",
            UnitPrice = 20000,
            LocalStockQuantity = 10,
            IsActive = true,
            TaxRate = 0.05m
        };

        _productRepositoryMock.Setup(r => r.GetByIdAsync(productId1)).ReturnsAsync(product1);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(productId2)).ReturnsAsync(product2);

        var cart = new CartSummaryDto();
        cart.Items.Add(new CartItemDto { ProductId = productId1, Quantity = 2 });
        cart.Items.Add(new CartItemDto { ProductId = productId2, Quantity = 1 });
        cart.DiscountAmount = 5000;

        // Act
        var result = _cartService.Recalculate(cart);

        // Assert
        // Item 1 subtotal = 2 * 10000 = 20000, tax = 2000
        // Item 2 subtotal = 1 * 20000 = 20000, tax = 1000
        // Total subtotal = 40000
        // Total tax = 3000
        // Total amount = 40000 + 3000 - 5000 = 38000
        result.Subtotal.Should().Be(40000);
        result.TaxAmount.Should().Be(3000);
        result.TotalAmount.Should().Be(38000);
    }
}

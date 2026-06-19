using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Implementations;
using SmartPOS.Shared.DTOs.Product;
using Xunit;

namespace SmartPOS.Tests;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<ILogger<ProductService>> _loggerMock;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _loggerMock = new Mock<ILogger<ProductService>>();
        _productService = new ProductService(_productRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task FindByBarcodeAsync_ShouldReturnProduct_WhenProductExists()
    {
        // Arrange
        var barcode = "123456789";
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Barcode Product",
            Barcode = barcode,
            Sku = "BAR-1",
            UnitPrice = 15000,
            LocalStockQuantity = 100,
            IsActive = true
        };

        _productRepositoryMock.Setup(r => r.GetByBarcodeAsync(barcode))
            .ReturnsAsync(product);

        // Act
        var result = await _productService.FindByBarcodeAsync(barcode);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Barcode Product");
        result.Barcode.Should().Be(barcode);
    }

    [Fact]
    public async Task FindByBarcodeAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        // Arrange
        var barcode = "999999999";
        _productRepositoryMock.Setup(r => r.GetByBarcodeAsync(barcode))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _productService.FindByBarcodeAsync(barcode);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnMatchingProducts()
    {
        // Arrange
        var keyword = "Coca";
        var products = new List<Product>
        {
            new Product { Id = Guid.NewGuid(), Name = "Coca Cola 330ml", Sku = "COCA330", UnitPrice = 10000, LocalStockQuantity = 50, IsActive = true },
            new Product { Id = Guid.NewGuid(), Name = "Coca Light 330ml", Sku = "COCALIGHT", UnitPrice = 12000, LocalStockQuantity = 30, IsActive = true }
        };

        _productRepositoryMock.Setup(r => r.SearchAsync(keyword))
            .ReturnsAsync(products);

        // Act
        var result = await _productService.SearchAsync(keyword);

        // Assert
        result.Should().NotBeNull();
        result.Products.Should().HaveCount(2);
        result.Products[0].Name.Should().Contain(keyword);
        result.Products[1].Name.Should().Contain(keyword);
    }
}

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Implementations;
using SmartPOS.Shared.DTOs.Inventory;
using SmartPOS.Shared.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SmartPOS.Tests;

/// <summary>
/// Helper để mock HttpClient trả về response định sẵn.
/// </summary>
file class FakeHttpHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public FakeHttpHandler(HttpResponseMessage response)
        => _response = response;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_response);
}

public class InventorySyncServiceTests
{
    // ── Factory ───────────────────────────────────────────────────────────────

    private static InventorySyncService CreateService(
        HttpClient httpClient,
        IInventorySyncLogRepository? syncLogRepo = null,
        IProductRepository? productRepo = null,
        ICategoryRepository? categoryRepo = null)
    {
        var logRepo = syncLogRepo ?? Mock.Of<IInventorySyncLogRepository>();
        var prodRepo = productRepo ?? Mock.Of<IProductRepository>();
        var catRepo = categoryRepo ?? Mock.Of<ICategoryRepository>();
        return new InventorySyncService(
            NullLogger<InventorySyncService>.Instance,
            httpClient,
            logRepo,
            prodRepo,
            catRepo);
    }

    private static HttpClient BuildHttpClient<T>(T payload, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(payload);
        var responseMessage = new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        return new HttpClient(new FakeHttpHandler(responseMessage))
        {
            BaseAddress = new Uri("http://localhost")
        };
    }

    private static HttpClient BuildFailingHttpClient()
    {
        var handler = new FakeHttpHandler(
            new HttpResponseMessage(HttpStatusCode.InternalServerError));
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
    }

    // ── TASK-1110: Test sync thành công ───────────────────────────────────────

    [Fact]
    public async Task SyncCatalogAsync_ApiReturnsItems_UpsertsProductsAndReturnsSuccess()
    {
        // Arrange
        var catalogItems = new List<InventoryCatalogItemDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Sản phẩm mới", Sku = "SP001", UnitPrice = 50_000, IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Sản phẩm cũ",  Sku = "SP002", UnitPrice = 80_000, IsActive = true }
        };

        var existingProductId = catalogItems[1].Id; // SP002 đã tồn tại trong POS
        var existingProduct   = new Product { Id = Guid.NewGuid(), ExternalInventoryId = existingProductId.ToString() };

        var productRepoMock = new Mock<IProductRepository>();
        // SP001 → chưa có → tạo mới
        productRepoMock
            .Setup(r => r.GetByExternalIdAsync(catalogItems[0].Id.ToString()))
            .ReturnsAsync((Product?)null);
        // SP002 → đã có → update
        productRepoMock
            .Setup(r => r.GetByExternalIdAsync(catalogItems[1].Id.ToString()))
            .ReturnsAsync(existingProduct);
        productRepoMock.Setup(r => r.AddAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
        productRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

        var syncLogMock = new Mock<IInventorySyncLogRepository>();
        syncLogMock.Setup(r => r.AddAsync(It.IsAny<InventorySyncLog>())).Returns(Task.CompletedTask);

        // Setup category repo mock trả list rỗng (không cần map category trong test này)
        var categoryRepoMock = new Mock<ICategoryRepository>();
        categoryRepoMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SmartPOS.Data.Entities.Category>());

        var httpClient = BuildHttpClient(catalogItems);
        var service    = CreateService(httpClient, syncLogMock.Object, productRepoMock.Object, categoryRepoMock.Object);

        // Act
        var result = await service.SyncCatalogAsync();

        // Assert
        result.Status.Should().Be("SUCCESS");
        result.AffectedRows.Should().Be(2); // 1 tạo mới + 1 update

        productRepoMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
        productRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Once);
        syncLogMock.Verify(
            r => r.AddAsync(It.Is<InventorySyncLog>(l => l.Status == SyncStatus.Success && l.SyncType == "CATALOG")),
            Times.Once);
    }

    [Fact]
    public async Task SyncStockAsync_ApiReturnsItems_UpdatesLocalStockQuantity()
    {
        // Arrange
        var invProductId = Guid.NewGuid();
        var stockItems = new List<InventoryStockItemDto>
        {
            new() { InventoryProductId = invProductId, Quantity = 42, UpdatedAt = DateTime.UtcNow }
        };

        var matchedProduct = new Product
        {
            Id = Guid.NewGuid(),
            ExternalInventoryId = invProductId.ToString(),
            LocalStockQuantity = 0
        };

        var productRepoMock = new Mock<IProductRepository>();
        productRepoMock
            .Setup(r => r.GetByExternalIdAsync(invProductId.ToString()))
            .ReturnsAsync(matchedProduct);
        productRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

        var syncLogMock = new Mock<IInventorySyncLogRepository>();
        syncLogMock.Setup(r => r.AddAsync(It.IsAny<InventorySyncLog>())).Returns(Task.CompletedTask);

        var httpClient = BuildHttpClient(stockItems);
        var service    = CreateService(httpClient, syncLogMock.Object, productRepoMock.Object);

        // Act
        var result = await service.SyncStockAsync();

        // Assert
        result.Status.Should().Be("SUCCESS");
        result.AffectedRows.Should().Be(1);
        matchedProduct.LocalStockQuantity.Should().Be(42); // đã được cập nhật
        productRepoMock.Verify(r => r.UpdateAsync(It.Is<Product>(p => p.LocalStockQuantity == 42)), Times.Once);
        syncLogMock.Verify(
            r => r.AddAsync(It.Is<InventorySyncLog>(l => l.Status == SyncStatus.Success && l.SyncType == "STOCK")),
            Times.Once);
    }

    // ── TASK-1110: Test sync thất bại ─────────────────────────────────────────

    [Fact]
    public async Task SyncCatalogAsync_ApiDown_ReturnsFailedAndLogsError()
    {
        // Arrange
        var syncLogMock = new Mock<IInventorySyncLogRepository>();
        syncLogMock.Setup(r => r.AddAsync(It.IsAny<InventorySyncLog>())).Returns(Task.CompletedTask);

        var httpClient = BuildFailingHttpClient(); // API trả 500
        var service    = CreateService(httpClient, syncLogMock.Object);

        // Act
        var result = await service.SyncCatalogAsync();

        // Assert
        result.Status.Should().Be("FAILED");
        result.AffectedRows.Should().Be(0);
        result.Message.Should().Contain("Không thể kết nối Inventory API");
        syncLogMock.Verify(
            r => r.AddAsync(It.Is<InventorySyncLog>(l => l.Status == SyncStatus.Failed && l.SyncType == "CATALOG")),
            Times.Once);
    }

    [Fact]
    public async Task SyncStockAsync_ApiDown_ReturnsFailedAndLogsError()
    {
        // Arrange
        var syncLogMock = new Mock<IInventorySyncLogRepository>();
        syncLogMock.Setup(r => r.AddAsync(It.IsAny<InventorySyncLog>())).Returns(Task.CompletedTask);

        var httpClient = BuildFailingHttpClient();
        var service    = CreateService(httpClient, syncLogMock.Object);

        // Act
        var result = await service.SyncStockAsync();

        // Assert
        result.Status.Should().Be("FAILED");
        result.AffectedRows.Should().Be(0);
        syncLogMock.Verify(
            r => r.AddAsync(It.Is<InventorySyncLog>(l => l.Status == SyncStatus.Failed && l.SyncType == "STOCK")),
            Times.Once);
    }

    // ── TASK-1110: Test partial sync ──────────────────────────────────────────

    [Fact]
    public async Task SyncStockAsync_PartialMatch_SkipsUnknownProductsAndCountsMatched()
    {
        // Arrange — 2 items từ API, chỉ 1 có ExternalInventoryId khớp trong POS
        var knownId   = Guid.NewGuid();
        var unknownId = Guid.NewGuid();

        var stockItems = new List<InventoryStockItemDto>
        {
            new() { InventoryProductId = knownId,   Quantity = 10, UpdatedAt = DateTime.UtcNow },
            new() { InventoryProductId = unknownId,  Quantity = 5,  UpdatedAt = DateTime.UtcNow }
        };

        var knownProduct = new Product
        {
            Id = Guid.NewGuid(),
            ExternalInventoryId = knownId.ToString(),
            LocalStockQuantity = 0
        };

        var productRepoMock = new Mock<IProductRepository>();
        productRepoMock
            .Setup(r => r.GetByExternalIdAsync(knownId.ToString()))
            .ReturnsAsync(knownProduct);
        productRepoMock
            .Setup(r => r.GetByExternalIdAsync(unknownId.ToString()))
            .ReturnsAsync((Product?)null); // không tìm thấy → bỏ qua
        productRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

        var syncLogMock = new Mock<IInventorySyncLogRepository>();
        syncLogMock.Setup(r => r.AddAsync(It.IsAny<InventorySyncLog>())).Returns(Task.CompletedTask);

        var httpClient = BuildHttpClient(stockItems);
        var service    = CreateService(httpClient, syncLogMock.Object, productRepoMock.Object);

        // Act
        var result = await service.SyncStockAsync();

        // Assert
        result.Status.Should().Be("PARTIAL"); // bỏ qua 1 → trả PARTIAL theo fix TASK-1106
        result.AffectedRows.Should().Be(1); // chỉ 1 cái được update
        knownProduct.LocalStockQuantity.Should().Be(10);

        // unknownId không được gọi UpdateAsync
        productRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Once);

        // Log ghi PARTIAL (theo fix TASK-1106: skipped > 0 → Partial)
        syncLogMock.Verify(
            r => r.AddAsync(It.Is<InventorySyncLog>(
                l => l.Status == SyncStatus.Partial
                  && l.SyncType == "STOCK"
                  && l.Message!.Contains("bỏ qua 1"))),
            Times.Once);
    }
}

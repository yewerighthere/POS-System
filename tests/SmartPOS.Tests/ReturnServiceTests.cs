using Microsoft.Extensions.Logging;
using Moq;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Implementations;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Return;
using SmartPOS.Shared.Enums;
using SmartPOS.Shared.Exceptions;
using Xunit;

namespace SmartPOS.Tests;

public class ReturnServiceTests
{
    private readonly Mock<IReturnRepository> _mockReturnRepo = new();
    private readonly Mock<IOrderRepository> _mockOrderRepo = new();
    private readonly Mock<IProductRepository> _mockProductRepo = new();
    private readonly Mock<IInventorySyncService> _mockInventorySync = new();
    private readonly Mock<ICustomerService> _mockCustomerService = new();
    private readonly Mock<IAuditService> _mockAuditService = new();
    private readonly Mock<ILogger<ReturnService>> _mockLogger = new();

    private readonly ReturnService _returnService;

    public ReturnServiceTests()
    {
        _returnService = new ReturnService(
            _mockReturnRepo.Object,
            _mockOrderRepo.Object,
            _mockProductRepo.Object,
            _mockInventorySync.Object,
            _mockCustomerService.Object,
            _mockAuditService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateReturnAsync_ValidOrder_CreatesReturnSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();
        var requestedBy = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            PaymentStatus = PaymentStatus.Success,
            Status = OrderStatus.Confirmed,
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = orderItemId,
                    OrderId = orderId,
                    ProductId = Guid.NewGuid(),
                    ProductName = "Cà phê sữa",
                    UnitPrice = 35000,
                    Quantity = 2
                }
            }
        };

        _mockOrderRepo.Setup(r => r.GetByIdWithItemsAsync(orderId))
            .ReturnsAsync(order);
        _mockReturnRepo.Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(new List<Return>());

        var request = new ReturnRequestDto(
            orderId,
            requestedBy,
            "Hàng lỗi",
            new List<ReturnItemInputDto> { new ReturnItemInputDto(orderItemId, 1) });

        // Act
        var result = await _returnService.CreateReturnAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(orderId, result.OrderId);
        Assert.Equal("Requested", result.Status);
        Assert.Equal(35000, result.RefundAmount);
        _mockReturnRepo.Verify(r => r.AddAsync(It.IsAny<Return>()), Times.Once);
    }

    [Fact]
    public async Task CreateReturnAsync_UnpaidOrder_ThrowsBusinessException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            PaymentStatus = PaymentStatus.Pending,
            Status = OrderStatus.Draft
        };

        _mockOrderRepo.Setup(r => r.GetByIdWithItemsAsync(orderId)).ReturnsAsync(order);

        var request = new ReturnRequestDto(orderId, Guid.NewGuid(), "Đổi ý", new List<ReturnItemInputDto> { new ReturnItemInputDto(Guid.NewGuid(), 1) });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _returnService.CreateReturnAsync(request));
        Assert.Contains("thanh toán thành công", ex.Message);
    }

    [Fact]
    public async Task CreateReturnAsync_ExceedingQuantity_ThrowsBusinessException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            PaymentStatus = PaymentStatus.Success,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = orderItemId, Quantity = 2, UnitPrice = 10000 }
            }
        };

        _mockOrderRepo.Setup(r => r.GetByIdWithItemsAsync(orderId)).ReturnsAsync(order);
        _mockReturnRepo.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync(new List<Return>());

        var request = new ReturnRequestDto(orderId, Guid.NewGuid(), "Lý do", new List<ReturnItemInputDto> { new ReturnItemInputDto(orderItemId, 5) });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _returnService.CreateReturnAsync(request));
        Assert.Contains("vượt quá số lượng", ex.Message);
    }

    [Fact]
    public async Task ApproveAsync_ValidRequestedReturn_UpdatesStatusApproveAndIncrementsStock()
    {
        // Arrange
        var returnId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var product = new Product { Id = productId, LocalStockQuantity = 10, ExternalInventoryId = "11111111-0001-0001-0001-000000000001" };
        var orderItem = new OrderItem { Id = Guid.NewGuid(), ProductId = productId, Product = product, UnitPrice = 20000 };
        var returnEntity = new Return
        {
            Id = returnId,
            Status = ReturnStatus.Requested,
            RefundAmount = 20000,
            OrderId = Guid.NewGuid(),
            Items = new List<ReturnItem>
            {
                new ReturnItem { OrderItemId = orderItem.Id, OrderItem = orderItem, Quantity = 2 }
            }
        };

        _mockReturnRepo.Setup(r => r.GetByIdAsync(returnId)).ReturnsAsync(returnEntity);

        // Act
        var result = await _returnService.ApproveAsync(returnId, managerId);

        // Assert
        Assert.Equal("Approved", result.Status);
        Assert.Equal(12, product.LocalStockQuantity); // 10 + 2 = 12
        _mockProductRepo.Verify(r => r.UpdateAsync(product), Times.Once);
        _mockReturnRepo.Verify(r => r.UpdateAsync(returnEntity), Times.Once);
    }

    [Fact]
    public async Task RejectAsync_ValidRequestedReturn_UpdatesStatusRejected()
    {
        // Arrange
        var returnId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var returnEntity = new Return
        {
            Id = returnId,
            Status = ReturnStatus.Requested,
            OrderId = Guid.NewGuid(),
            Items = new List<ReturnItem>()
        };

        _mockReturnRepo.Setup(r => r.GetByIdAsync(returnId)).ReturnsAsync(returnEntity);

        // Act
        var result = await _returnService.RejectAsync(returnId, managerId);

        // Assert
        Assert.Equal("Rejected", result.Status);
        _mockReturnRepo.Verify(r => r.UpdateAsync(returnEntity), Times.Once);
    }
}

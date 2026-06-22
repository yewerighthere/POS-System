using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Implementations;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Order;
using SmartPOS.Shared.Enums;
using SmartPOS.Shared.Exceptions;
using Xunit;

namespace SmartPOS.Tests;

public class PaymentServiceTests
{
    private static PaymentService CreateService(
        IOrderRepository orderRepo,
        IInventorySyncService? inventorySync = null,
        IAuditService? auditService = null)
    {
        var syncMock = inventorySync ?? Mock.Of<IInventorySyncService>();
        var auditMock = auditService ?? Mock.Of<IAuditService>();
        return new PaymentService(orderRepo, syncMock, auditMock, NullLogger<PaymentService>.Instance);
    }

    private static Order BuildOrder(decimal total, bool isLocked = false) => new Order
    {
        Id = Guid.NewGuid(),
        ShiftId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        TotalAmount = total,
        IsLocked = isLocked,
        PaymentStatus = PaymentStatus.Pending,
        Status = OrderStatus.Draft,
        Items = new List<OrderItem>()
    };

    [Fact]
    public async Task RecordCashPaymentAsync_ValidPayment_ReturnsCorrectChange()
    {
        // Arrange
        var order = BuildOrder(150_000m);
        var orderId = order.Id;
        var userId = Guid.NewGuid();

        var repoMock = new Mock<IOrderRepository>();
        repoMock.Setup(r => r.GetByIdWithItemsAsync(orderId)).ReturnsAsync(order);
        repoMock.Setup(r => r.AddPaymentAsync(It.IsAny<Order>(), It.IsAny<Payment>())).Returns(Task.CompletedTask);

        var syncMock = new Mock<IInventorySyncService>();
        syncMock.Setup(s => s.SendStockDeductionAsync(It.IsAny<IEnumerable<OrderItemDto>>(), It.IsAny<Guid>()))
                .ThrowsAsync(new NotImplementedException());

        var auditMock = new Mock<IAuditService>();
        auditMock.Setup(a => a.LogAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<Guid>()))
                 .ThrowsAsync(new NotImplementedException());

        var service = CreateService(repoMock.Object, syncMock.Object, auditMock.Object);

        // Act
        var result = await service.RecordCashPaymentAsync(orderId, 200_000m, userId);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(orderId);
        result.AmountReceived.Should().Be(200_000m);
        result.ChangeAmount.Should().Be(50_000m);
        result.PaymentStatus.Should().Be(PaymentStatus.Success.ToString());

        repoMock.Verify(r => r.AddPaymentAsync(
            It.Is<Order>(o => o.PaymentStatus == PaymentStatus.Success && o.Status == OrderStatus.Confirmed),
            It.Is<Payment>(p => p.PaymentMethod == PaymentMethod.Cash && p.ChangeAmount == 50_000m)),
            Times.Once);
    }

    [Fact]
    public async Task RecordCashPaymentAsync_InsufficientAmount_ThrowsBusinessException()
    {
        // Arrange
        var order = BuildOrder(200_000m);
        var repoMock = new Mock<IOrderRepository>();
        repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var service = CreateService(repoMock.Object);

        // Act
        var act = async () => await service.RecordCashPaymentAsync(order.Id, 100_000m, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Số tiền khách đưa không đủ để thanh toán");
    }

    [Fact]
    public async Task RecordCashPaymentAsync_LockedOrder_ThrowsBusinessException()
    {
        // Arrange
        var order = BuildOrder(100_000m, isLocked: true);
        var repoMock = new Mock<IOrderRepository>();
        repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var service = CreateService(repoMock.Object);

        // Act
        var act = async () => await service.RecordCashPaymentAsync(order.Id, 100_000m, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Đơn hàng đang bị khóa bởi phiên thanh toán khác");
    }
}

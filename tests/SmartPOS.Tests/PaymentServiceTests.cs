using FluentAssertions;
using Microsoft.Extensions.Configuration;
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
        IInvoiceService? invoiceService = null,
        IConfiguration? configuration = null,
        ICustomerService? customerService = null)
    {
        var syncMock = inventorySync ?? Mock.Of<IInventorySyncService>();
        var customerMock = customerService ?? Mock.Of<ICustomerService>();
        return new PaymentService(orderRepo, syncMock, customerMock, NullLogger<PaymentService>.Instance, invoiceService, configuration);
    }

    private static IConfiguration BuildVNPayConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["VNPay:TmnCode"] = "WZ5YOPOO",
            ["VNPay:HashSecret"] = "U8MQJW9A1IJYP2G0L6JVWC405O9OWPWL",
            ["VNPay:PaymentUrl"] = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
            ["VNPay:ReturnUrl"] = "https://example.ngrok-free.app/api/vnpay/callback"
        })
        .Build();

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

        var service = CreateService(repoMock.Object, syncMock.Object);

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

    [Fact]
    public async Task CreateVNPayRequestAsync_ValidOrder_LocksOrderAndCreatesPendingPayment()
    {
        var order = BuildOrder(25_000m);
        var repoMock = new Mock<IOrderRepository>();
        repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);
        repoMock.Setup(r => r.AddPaymentAsync(It.IsAny<Order>(), It.IsAny<Payment>())).Returns(Task.CompletedTask);
        var service = CreateService(repoMock.Object, configuration: BuildVNPayConfiguration());

        var result = await service.CreateVNPayRequestAsync(new(order.Id, order.TotalAmount, "https://example.ngrok-free.app/api/vnpay/callback"));

        result.OrderId.Should().Be(order.Id);
        result.PaymentStatus.Should().Be(PaymentStatus.Pending.ToString());
        result.PaymentUrl.Should().Contain("vnp_Amount=2500000");
        result.PaymentUrl.Should().Contain($"vnp_TxnRef={order.Id:N}");
        result.PaymentUrl.Should().Contain("vnp_SecureHash=");
        repoMock.Verify(r => r.AddPaymentAsync(
            It.Is<Order>(o => o.IsLocked && o.PaymentMethod == PaymentMethod.VNPay && o.PaymentStatus == PaymentStatus.Pending),
            It.Is<Payment>(p => p.PaymentMethod == PaymentMethod.VNPay && p.PaymentStatus == PaymentStatus.Pending)),
            Times.Once);
    }

    [Fact]
    public async Task HandleVNPayCallbackAsync_ResponseCode00_MarksPaymentSuccessAndCreatesInvoice()
    {
        var order = BuildOrder(25_000m, isLocked: true);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PaymentMethod = PaymentMethod.VNPay,
            PaymentStatus = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        order.Payments.Add(payment);
        var repoMock = new Mock<IOrderRepository>();
        repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);
        repoMock.Setup(r => r.UpdateAsync(order)).Returns(Task.CompletedTask);
        var invoiceMock = new Mock<IInvoiceService>();
        invoiceMock.Setup(i => i.CreateInvoiceAsync(order.Id)).ReturnsAsync(new SmartPOS.Shared.DTOs.Invoice.InvoiceDto { Id = Guid.NewGuid(), OrderId = order.Id });
        var service = CreateService(repoMock.Object, invoiceService: invoiceMock.Object, configuration: BuildVNPayConfiguration());

        var result = await service.HandleVNPayCallbackAsync(new(order.Id, "VNP123", "00"));

        result.PaymentStatus.Should().Be(PaymentStatus.Success.ToString());
        order.PaymentStatus.Should().Be(PaymentStatus.Success);
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.IsLocked.Should().BeFalse();
        payment.TransactionId.Should().Be("VNP123");
        payment.VnpayResponse.Should().Contain("VNP123");
        invoiceMock.Verify(i => i.CreateInvoiceAsync(order.Id), Times.Once);
    }

    [Fact]
    public async Task HandleVNPayCallbackAsync_ResponseCodeFailed_MarksPaymentFailedAndUnlocksOrder()
    {
        var order = BuildOrder(25_000m, isLocked: true);
        order.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PaymentMethod = PaymentMethod.VNPay,
            PaymentStatus = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        var repoMock = new Mock<IOrderRepository>();
        repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);
        repoMock.Setup(r => r.UpdateAsync(order)).Returns(Task.CompletedTask);
        var invoiceMock = new Mock<IInvoiceService>();
        var service = CreateService(repoMock.Object, invoiceService: invoiceMock.Object, configuration: BuildVNPayConfiguration());

        var result = await service.HandleVNPayCallbackAsync(new(order.Id, "VNP999", "24"));

        result.PaymentStatus.Should().Be(PaymentStatus.Failed.ToString());
        order.PaymentStatus.Should().Be(PaymentStatus.Failed);
        order.Status.Should().Be(OrderStatus.Draft);
        order.IsLocked.Should().BeFalse();
        invoiceMock.Verify(i => i.CreateInvoiceAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CancelVNPayAsync_PendingPayment_MarksTimeoutAndUnlocksOrder()
    {
        var order = BuildOrder(25_000m, isLocked: true);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PaymentMethod = PaymentMethod.VNPay,
            PaymentStatus = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        order.Payments.Add(payment);
        var repoMock = new Mock<IOrderRepository>();
        repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);
        repoMock.Setup(r => r.UpdateAsync(order)).Returns(Task.CompletedTask);
        var service = CreateService(repoMock.Object);

        await service.CancelVNPayAsync(order.Id);

        order.IsLocked.Should().BeFalse();
        order.PaymentStatus.Should().Be(PaymentStatus.Timeout);
        order.Status.Should().Be(OrderStatus.Draft);
        payment.PaymentStatus.Should().Be(PaymentStatus.Timeout);
    }

    [Fact]
    public async Task GetOrderPaymentStatusAsync_ReturnsFreshRepositoryStatus()
    {
        var orderId = Guid.NewGuid();
        var repoMock = new Mock<IOrderRepository>();
        repoMock.Setup(r => r.GetPaymentStatusAsync(orderId)).ReturnsAsync(PaymentStatus.Success);
        var service = CreateService(repoMock.Object);

        var status = await service.GetOrderPaymentStatusAsync(orderId);

        status.Should().Be(PaymentStatus.Success);
    }
}

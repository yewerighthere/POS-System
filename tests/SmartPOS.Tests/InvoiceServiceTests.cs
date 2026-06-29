using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Implementations;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.Enums;
using SmartPOS.Shared.Exceptions;
using Xunit;

namespace SmartPOS.Tests;

public class InvoiceServiceTests
{
    private static Order BuildOrder(Guid orderId, PaymentStatus paymentStatus = PaymentStatus.Success) => new()
    {
        Id = orderId,
        ShiftId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        TotalAmount = 125_000m,
        PaymentStatus = paymentStatus,
        Status = paymentStatus == PaymentStatus.Success ? OrderStatus.Confirmed : OrderStatus.Draft,
        Items = new List<OrderItem>()
    };

    private static InvoiceService CreateService(
        IOrderRepository orderRepository,
        IInvoiceRepository invoiceRepository,
        IDeviceService? deviceService = null)
    {
        return new InvoiceService(
            orderRepository,
            invoiceRepository,
            deviceService ?? Mock.Of<IDeviceService>(),
            NullLogger<InvoiceService>.Instance);
    }

    [Fact]
    public async Task CreateInvoiceAsync_SuccessfulPayment_CreatesInvoiceWithDailySequence()
    {
        var orderId = Guid.NewGuid();
        var order = BuildOrder(orderId);
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetPaymentStatusAsync(orderId)).ReturnsAsync(PaymentStatus.Success);
        orderRepo.Setup(r => r.GetByIdWithItemsAsync(orderId)).ReturnsAsync(order);

        var invoiceRepo = new Mock<IInvoiceRepository>();
        invoiceRepo.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync((Invoice?)null);
        invoiceRepo.Setup(r => r.GetDailySequenceAsync(
                It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc),
                It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc)))
            .ReturnsAsync(3);
        invoiceRepo.Setup(r => r.AddAsync(It.IsAny<Invoice>())).Returns(Task.CompletedTask);
        var service = CreateService(orderRepo.Object, invoiceRepo.Object);

        var invoice = await service.CreateInvoiceAsync(orderId);

        invoice.OrderId.Should().Be(orderId);
        invoice.InvoiceNumber.Should().MatchRegex(@"^INV-\d{8}-0004$");
        invoice.TotalAmount.Should().Be(order.TotalAmount);
        invoiceRepo.Verify(r => r.AddAsync(It.Is<Invoice>(i => i.OrderId == orderId && i.TotalAmount == order.TotalAmount)), Times.Once);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Failed)]
    public async Task CreateInvoiceAsync_NonSuccessfulPayment_ThrowsBusinessException(PaymentStatus status)
    {
        var orderId = Guid.NewGuid();
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetPaymentStatusAsync(orderId)).ReturnsAsync(status);
        var invoiceRepo = new Mock<IInvoiceRepository>();
        var service = CreateService(orderRepo.Object, invoiceRepo.Object);

        var act = async () => await service.CreateInvoiceAsync(orderId);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Chỉ tạo hóa đơn khi thanh toán thành công");
        invoiceRepo.Verify(r => r.AddAsync(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task CreateInvoiceAsync_ExistingInvoice_ReturnsExistingInvoiceWithoutAdding()
    {
        var orderId = Guid.NewGuid();
        var existing = new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            InvoiceNumber = "INV-20260629-0001",
            TotalAmount = 50_000m,
            IssuedAt = DateTime.UtcNow
        };
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetPaymentStatusAsync(orderId)).ReturnsAsync(PaymentStatus.Success);
        orderRepo.Setup(r => r.GetByIdWithItemsAsync(orderId)).ReturnsAsync(BuildOrder(orderId));
        var invoiceRepo = new Mock<IInvoiceRepository>();
        invoiceRepo.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync(existing);
        var service = CreateService(orderRepo.Object, invoiceRepo.Object);

        var invoice = await service.CreateInvoiceAsync(orderId);

        invoice.Id.Should().Be(existing.Id);
        invoice.InvoiceNumber.Should().Be(existing.InvoiceNumber);
        invoiceRepo.Verify(r => r.AddAsync(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task PrintPreviewAsync_ExistingInvoice_LogsDevicePreviewEvent()
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            InvoiceNumber = "INV-20260629-0001",
            TotalAmount = 50_000m,
            IssuedAt = DateTime.UtcNow
        };
        var orderRepo = new Mock<IOrderRepository>();
        var invoiceRepo = new Mock<IInvoiceRepository>();
        invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
        var deviceService = new Mock<IDeviceService>();
        deviceService.Setup(d => d.LogDeviceEventAsync(null, "INVOICE_PREVIEW", It.IsAny<string>())).Returns(Task.CompletedTask);
        var service = CreateService(orderRepo.Object, invoiceRepo.Object, deviceService.Object);

        await service.PrintPreviewAsync(invoice.Id);

        deviceService.Verify(d => d.LogDeviceEventAsync(null, "INVOICE_PREVIEW", It.Is<string>(message => message.Contains(invoice.InvoiceNumber))), Times.Once);
    }
}

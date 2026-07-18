using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Implementations;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Report;
using SmartPOS.Shared.Enums;
using SmartPOS.Shared.Exceptions;
using Xunit;

namespace SmartPOS.Tests;

public class ReportServiceTests
{
    private static ReportService CreateService(IShiftRepository shiftRepo, IOrderRepository orderRepo)
        => new(NullLogger<ReportService>.Instance, shiftRepo, orderRepo, Mock.Of<IAuditService>());

    // ── GetShiftReportAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetShiftReportAsync_ValidShift_MapsAllFields()
    {
        // Arrange
        var shiftId = Guid.NewGuid();
        var shift = new Shift
        {
            Id          = shiftId,
            Status      = ShiftStatus.Closed,
            OpeningCash = 500_000m,
            ClosingCash = 800_000m,
            ExpectedCash   = 790_000m,
            CashDifference = 10_000m,
            OpenedAt    = new DateTime(2024, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            ClosedAt    = new DateTime(2024, 1, 1, 16, 0, 0, DateTimeKind.Utc)
        };

        var shiftMock = new Mock<IShiftRepository>();
        shiftMock.Setup(r => r.GetByIdAsync(shiftId)).ReturnsAsync(shift);
        shiftMock.Setup(r => r.GetTotalSalesAsync(shiftId)).ReturnsAsync(1_200_000m);
        shiftMock.Setup(r => r.GetCashRevenueAsync(shiftId)).ReturnsAsync(800_000m);
        shiftMock.Setup(r => r.GetVNPayRevenueAsync(shiftId)).ReturnsAsync(400_000m);

        var orderMock = new Mock<IOrderRepository>();
        orderMock.Setup(r => r.GetOrderCountByShiftAsync(shiftId)).ReturnsAsync(10);
        orderMock.Setup(r => r.GetOrdersByShiftAsync(shiftId)).ReturnsAsync(new List<Order>());
        orderMock.Setup(r => r.GetTopProductsByShiftAsync(shiftId, 5)).ReturnsAsync(new List<TopProductDto>());

        var service = CreateService(shiftMock.Object, orderMock.Object);

        // Act
        var result = await service.GetShiftReportAsync(shiftId);

        // Assert
        result.ShiftId.Should().Be(shiftId);
        result.Status.Should().Be(ShiftStatus.Closed.ToString());
        result.TotalSales.Should().Be(1_200_000m);
        result.CashRevenue.Should().Be(800_000m);
        result.VNPayRevenue.Should().Be(400_000m);
        result.TotalOrders.Should().Be(10);
    }

    [Fact]
    public async Task GetShiftReportAsync_ShiftNotFound_ThrowsBusinessException()
    {
        // Arrange
        var shiftId = Guid.NewGuid();
        var shiftMock = new Mock<IShiftRepository>();
        shiftMock.Setup(r => r.GetByIdAsync(shiftId)).ReturnsAsync((Shift?)null);

        var service = CreateService(shiftMock.Object, new Mock<IOrderRepository>().Object);

        // Act
        var act = async () => await service.GetShiftReportAsync(shiftId);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Không tìm thấy ca làm việc*");
    }

    [Fact]
    public async Task GetShiftReportAsync_MapsOrderLogFromOrders()
    {
        // Arrange
        var shiftId = Guid.NewGuid();
        var shift = new Shift { Id = shiftId, Status = ShiftStatus.Closed, OpenedAt = DateTime.UtcNow };

        var orders = new List<Order>
        {
            new()
            {
                Id            = Guid.NewGuid(),
                CreatedAt     = DateTime.UtcNow,
                User          = new User { FullName = "Nguyen Van A" },
                Items         = new List<OrderItem> { new() { Quantity = 2, ProductName = "Cà phê" } },
                PaymentMethod = PaymentMethod.Cash,
                TotalAmount   = 60_000m,
                PaymentStatus = PaymentStatus.Success
            },
            new()
            {
                Id            = Guid.NewGuid(),
                CreatedAt     = DateTime.UtcNow,
                User          = new User { FullName = "Tran Thi B" },
                Items         = new List<OrderItem> { new() { Quantity = 1, ProductName = "Trà sữa" } },
                PaymentMethod = PaymentMethod.VNPay,
                TotalAmount   = 45_000m,
                PaymentStatus = PaymentStatus.Success
            }
        };

        var shiftMock = new Mock<IShiftRepository>();
        shiftMock.Setup(r => r.GetByIdAsync(shiftId)).ReturnsAsync(shift);
        shiftMock.Setup(r => r.GetTotalSalesAsync(shiftId)).ReturnsAsync(105_000m);
        shiftMock.Setup(r => r.GetCashRevenueAsync(shiftId)).ReturnsAsync(60_000m);
        shiftMock.Setup(r => r.GetVNPayRevenueAsync(shiftId)).ReturnsAsync(45_000m);

        var orderMock = new Mock<IOrderRepository>();
        orderMock.Setup(r => r.GetOrderCountByShiftAsync(shiftId)).ReturnsAsync(2);
        orderMock.Setup(r => r.GetOrdersByShiftAsync(shiftId)).ReturnsAsync(orders);
        orderMock.Setup(r => r.GetTopProductsByShiftAsync(shiftId, 5)).ReturnsAsync(new List<TopProductDto>());

        var service = CreateService(shiftMock.Object, orderMock.Object);

        // Act
        var result = await service.GetShiftReportAsync(shiftId);

        // Assert
        result.OrderLog.Should().HaveCount(2);
        result.OrderLog[0].StaffName.Should().Be("Nguyen Van A");
        result.OrderLog[0].TotalAmount.Should().Be(60_000m);
        result.OrderLog[1].StaffName.Should().Be("Tran Thi B");
        result.OrderLog[1].TotalAmount.Should().Be(45_000m);
    }

    [Fact]
    public async Task GetShiftReportAsync_TopProducts_LimitedToFive()
    {
        // Arrange
        var shiftId = Guid.NewGuid();
        var shift = new Shift { Id = shiftId, Status = ShiftStatus.Open, OpenedAt = DateTime.UtcNow };

        var shiftMock = new Mock<IShiftRepository>();
        shiftMock.Setup(r => r.GetByIdAsync(shiftId)).ReturnsAsync(shift);
        shiftMock.Setup(r => r.GetTotalSalesAsync(shiftId)).ReturnsAsync(0m);
        shiftMock.Setup(r => r.GetCashRevenueAsync(shiftId)).ReturnsAsync(0m);
        shiftMock.Setup(r => r.GetVNPayRevenueAsync(shiftId)).ReturnsAsync(0m);

        var orderMock = new Mock<IOrderRepository>();
        orderMock.Setup(r => r.GetOrderCountByShiftAsync(shiftId)).ReturnsAsync(0);
        orderMock.Setup(r => r.GetOrdersByShiftAsync(shiftId)).ReturnsAsync(new List<Order>());
        orderMock.Setup(r => r.GetTopProductsByShiftAsync(shiftId, 5)).ReturnsAsync(new List<TopProductDto>());

        var service = CreateService(shiftMock.Object, orderMock.Object);

        // Act
        await service.GetShiftReportAsync(shiftId);

        // Assert
        orderMock.Verify(r => r.GetTopProductsByShiftAsync(shiftId, 5), Times.Once);
    }

    // ── GetRecentShiftSummariesAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetRecentShiftSummariesAsync_ReturnsSummaryPerShift()
    {
        // Arrange
        var shifts = new List<Shift>
        {
            new() { Id = Guid.NewGuid(), Status = ShiftStatus.Closed, OpenedAt = DateTime.UtcNow, User = new User { FullName = "A" } },
            new() { Id = Guid.NewGuid(), Status = ShiftStatus.Closed, OpenedAt = DateTime.UtcNow, User = new User { FullName = "B" } },
            new() { Id = Guid.NewGuid(), Status = ShiftStatus.Open,   OpenedAt = DateTime.UtcNow, User = new User { FullName = "C" } }
        };

        var shiftMock = new Mock<IShiftRepository>();
        shiftMock.Setup(r => r.GetRecentShiftsAsync(3)).ReturnsAsync(shifts);
        foreach (var s in shifts)
            shiftMock.Setup(r => r.GetTotalSalesAsync(s.Id)).ReturnsAsync(100_000m);

        var orderMock = new Mock<IOrderRepository>();
        foreach (var s in shifts)
            orderMock.Setup(r => r.GetOrderCountByShiftAsync(s.Id)).ReturnsAsync(5);

        var service = CreateService(shiftMock.Object, orderMock.Object);

        // Act
        var result = await service.GetRecentShiftSummariesAsync(3);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetRecentShiftSummariesAsync_CallsRevenueAndOrderCountPerShift()
    {
        // Arrange
        var shifts = new List<Shift>
        {
            new() { Id = Guid.NewGuid(), Status = ShiftStatus.Closed, OpenedAt = DateTime.UtcNow, User = new User { FullName = "A" } },
            new() { Id = Guid.NewGuid(), Status = ShiftStatus.Closed, OpenedAt = DateTime.UtcNow, User = new User { FullName = "B" } },
            new() { Id = Guid.NewGuid(), Status = ShiftStatus.Closed, OpenedAt = DateTime.UtcNow, User = new User { FullName = "C" } }
        };

        var shiftMock = new Mock<IShiftRepository>();
        shiftMock.Setup(r => r.GetRecentShiftsAsync(It.IsAny<int>())).ReturnsAsync(shifts);
        foreach (var s in shifts)
            shiftMock.Setup(r => r.GetTotalSalesAsync(s.Id)).ReturnsAsync(200_000m);

        var orderMock = new Mock<IOrderRepository>();
        foreach (var s in shifts)
            orderMock.Setup(r => r.GetOrderCountByShiftAsync(s.Id)).ReturnsAsync(7);

        var service = CreateService(shiftMock.Object, orderMock.Object);

        // Act
        await service.GetRecentShiftSummariesAsync(3);

        // Assert
        shiftMock.Verify(r => r.GetTotalSalesAsync(It.IsAny<Guid>()), Times.Exactly(3));
        orderMock.Verify(r => r.GetOrderCountByShiftAsync(It.IsAny<Guid>()), Times.Exactly(3));
    }

    [Fact]
    public async Task GetRecentShiftSummariesAsync_EmptyList_ReturnsEmpty()
    {
        // Arrange
        var shiftMock = new Mock<IShiftRepository>();
        shiftMock.Setup(r => r.GetRecentShiftsAsync(It.IsAny<int>())).ReturnsAsync(new List<Shift>());

        var orderMock = new Mock<IOrderRepository>();

        var service = CreateService(shiftMock.Object, orderMock.Object);

        // Act
        var result = await service.GetRecentShiftSummariesAsync(10);

        // Assert
        result.Should().BeEmpty();
        shiftMock.Verify(r => r.GetTotalSalesAsync(It.IsAny<Guid>()), Times.Never);
        orderMock.Verify(r => r.GetOrderCountByShiftAsync(It.IsAny<Guid>()), Times.Never);
    }

    // ── GetSalesReportAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetSalesReportAsync_NoOrdersInRange_ReturnsZeroTotals()
    {
        // Arrange
        var filter = new SalesReportFilterDto(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));

        var orderMock = new Mock<IOrderRepository>();
        orderMock.Setup(r => r.GetOrdersByDateRangeAsync(filter.FromDate, filter.ToDate, It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<PaymentMethod?>()))
                 .ReturnsAsync(new List<Order>());
        orderMock.Setup(r => r.GetTopProductsByDateRangeAsync(filter.FromDate, filter.ToDate, 5, It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<PaymentMethod?>()))
                 .ReturnsAsync(new List<TopProductDto>());

        var service = CreateService(new Mock<IShiftRepository>().Object, orderMock.Object);

        // Act
        var result = await service.GetSalesReportAsync(filter);

        // Assert
        result.TotalRevenue.Should().Be(0);
        result.TotalOrders.Should().Be(0);
        result.TotalShifts.Should().Be(0);
        result.OrderLog.Should().BeEmpty();
        result.TopProducts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSalesReportAsync_ReturnsCorrectAggregates()
    {
        // Arrange
        var filter  = new SalesReportFilterDto(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));
        var shiftId = Guid.NewGuid();

        var orders = new List<Order>
        {
            new()
            {
                Id            = Guid.NewGuid(),
                ShiftId       = shiftId,
                Status        = OrderStatus.Confirmed,
                PaymentMethod = PaymentMethod.Cash,
                TotalAmount   = 100_000m,
                DiscountAmount = 0m,
                PointsDiscountAmount = 0m,
                TaxAmount     = 10_000m,
                PaymentStatus = PaymentStatus.Success,
                CreatedAt     = new DateTime(2024, 1, 10, 9, 0, 0, DateTimeKind.Utc),
                User          = new User { FullName = "Nguyen Van A" },
                Items         = new List<OrderItem> { new() { Quantity = 2, ProductName = "Cà phê" } }
            },
            new()
            {
                Id            = Guid.NewGuid(),
                ShiftId       = shiftId,
                Status        = OrderStatus.Confirmed,
                PaymentMethod = PaymentMethod.VNPay,
                TotalAmount   = 200_000m,
                DiscountAmount = 0m,
                PointsDiscountAmount = 0m,
                TaxAmount     = 20_000m,
                PaymentStatus = PaymentStatus.Success,
                CreatedAt     = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                User          = new User { FullName = "Tran Thi B" },
                Items         = new List<OrderItem> { new() { Quantity = 1, ProductName = "Trà sữa" } }
            }
        };

        var orderMock = new Mock<IOrderRepository>();
        orderMock.Setup(r => r.GetOrdersByDateRangeAsync(filter.FromDate, filter.ToDate, It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<PaymentMethod?>()))
                 .ReturnsAsync(orders);
        orderMock.Setup(r => r.GetTopProductsByDateRangeAsync(filter.FromDate, filter.ToDate, 5, It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<PaymentMethod?>()))
                 .ReturnsAsync(new List<TopProductDto>());

        var service = CreateService(new Mock<IShiftRepository>().Object, orderMock.Object);

        // Act
        var result = await service.GetSalesReportAsync(filter);

        // Assert
        result.TotalRevenue.Should().Be(300_000m);
        result.TotalOrders.Should().Be(2);
        result.TotalShifts.Should().Be(1);
        result.TotalTax.Should().Be(30_000m);
        result.OrderLog.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSalesReportAsync_ExcludesCancelledOrdersFromAggregates()
    {
        // Arrange
        var filter  = new SalesReportFilterDto(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));
        var shiftId = Guid.NewGuid();

        var orders = new List<Order>
        {
            new()
            {
                Id            = Guid.NewGuid(),
                ShiftId       = shiftId,
                Status        = OrderStatus.Confirmed,
                PaymentMethod = PaymentMethod.Cash,
                TotalAmount   = 150_000m,
                DiscountAmount = 0m,
                PointsDiscountAmount = 0m,
                TaxAmount     = 0m,
                PaymentStatus = PaymentStatus.Success,
                CreatedAt     = new DateTime(2024, 1, 5, 8, 0, 0, DateTimeKind.Utc),
                User          = new User { FullName = "A" },
                Items         = new List<OrderItem>()
            },
            new()
            {
                Id            = Guid.NewGuid(),
                ShiftId       = shiftId,
                Status        = OrderStatus.Cancelled,
                PaymentMethod = PaymentMethod.Cash,
                TotalAmount   = 999_000m,
                DiscountAmount = 0m,
                PointsDiscountAmount = 0m,
                TaxAmount     = 0m,
                PaymentStatus = PaymentStatus.Success,
                CreatedAt     = new DateTime(2024, 1, 6, 8, 0, 0, DateTimeKind.Utc),
                User          = new User { FullName = "B" },
                Items         = new List<OrderItem>()
            }
        };

        var orderMock = new Mock<IOrderRepository>();
        orderMock.Setup(r => r.GetOrdersByDateRangeAsync(filter.FromDate, filter.ToDate, It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<PaymentMethod?>()))
                 .ReturnsAsync(orders);
        orderMock.Setup(r => r.GetTopProductsByDateRangeAsync(filter.FromDate, filter.ToDate, 5, It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<PaymentMethod?>()))
                 .ReturnsAsync(new List<TopProductDto>());

        var service = CreateService(new Mock<IShiftRepository>().Object, orderMock.Object);

        // Act
        var result = await service.GetSalesReportAsync(filter);

        // Assert
        result.TotalRevenue.Should().Be(150_000m);
        result.TotalOrders.Should().Be(1);
        result.OrderLog.Should().HaveCount(2);  // all orders in log regardless of status
    }

    [Fact]
    public async Task GetSalesReportAsync_SeparatesCashAndVNPayRevenue()
    {
        // Arrange
        var filter = new SalesReportFilterDto(new DateTime(2024, 2, 1), new DateTime(2024, 2, 28));

        var orders = new List<Order>
        {
            new()
            {
                Id            = Guid.NewGuid(),
                ShiftId       = Guid.NewGuid(),
                Status        = OrderStatus.Confirmed,
                PaymentMethod = PaymentMethod.Cash,
                TotalAmount   = 80_000m,
                DiscountAmount = 0m,
                PointsDiscountAmount = 0m,
                TaxAmount     = 0m,
                PaymentStatus = PaymentStatus.Success,
                CreatedAt     = new DateTime(2024, 2, 5, 9, 0, 0, DateTimeKind.Utc),
                User          = new User { FullName = "A" },
                Items         = new List<OrderItem>()
            },
            new()
            {
                Id            = Guid.NewGuid(),
                ShiftId       = Guid.NewGuid(),
                Status        = OrderStatus.Confirmed,
                PaymentMethod = PaymentMethod.VNPay,
                TotalAmount   = 120_000m,
                DiscountAmount = 0m,
                PointsDiscountAmount = 0m,
                TaxAmount     = 0m,
                PaymentStatus = PaymentStatus.Success,
                CreatedAt     = new DateTime(2024, 2, 10, 10, 0, 0, DateTimeKind.Utc),
                User          = new User { FullName = "B" },
                Items         = new List<OrderItem>()
            }
        };

        var orderMock = new Mock<IOrderRepository>();
        orderMock.Setup(r => r.GetOrdersByDateRangeAsync(filter.FromDate, filter.ToDate, It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<PaymentMethod?>()))
                 .ReturnsAsync(orders);
        orderMock.Setup(r => r.GetTopProductsByDateRangeAsync(filter.FromDate, filter.ToDate, 5, It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<PaymentMethod?>()))
                 .ReturnsAsync(new List<TopProductDto>());

        var service = CreateService(new Mock<IShiftRepository>().Object, orderMock.Object);

        // Act
        var result = await service.GetSalesReportAsync(filter);

        // Assert
        result.CashRevenue.Should().Be(80_000m);
        result.VNPayRevenue.Should().Be(120_000m);
        result.TotalRevenue.Should().Be(200_000m);
    }

    [Fact]
    public async Task GetSalesReportAsync_InvalidDateRange_ThrowsBusinessException()
    {
        // Arrange
        var filter = new SalesReportFilterDto(
            new DateTime(2024, 1, 31),
            new DateTime(2024, 1, 1));  // from > to

        var service = CreateService(new Mock<IShiftRepository>().Object, new Mock<IOrderRepository>().Object);

        // Act
        var act = async () => await service.GetSalesReportAsync(filter);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Ngày bắt đầu không được lớn hơn ngày kết thúc*");
    }
}

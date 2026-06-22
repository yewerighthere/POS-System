using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Implementations;
using SmartPOS.Shared.DTOs.Shift;
using SmartPOS.Shared.Enums;
using SmartPOS.Shared.Exceptions;
using Xunit;

namespace SmartPOS.Tests;

public class ShiftServiceTests
{
    private static ShiftService CreateService(IShiftRepository repo)
        => new(repo, NullLogger<ShiftService>.Instance);

    [Fact]
    public async Task OpenShiftAsync_ValidInput_ReturnsShiftDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var repoMock = new Mock<IShiftRepository>();
        repoMock.Setup(r => r.GetOpenShiftAsync(userId)).ReturnsAsync((Shift?)null);
        repoMock.Setup(r => r.AddAsync(It.IsAny<Shift>())).Returns(Task.CompletedTask);

        var service = CreateService(repoMock.Object);
        var dto = new OpenShiftDto(userId, 500_000m);

        // Act
        var result = await service.OpenShiftAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.OpeningCash.Should().Be(500_000m);
        result.Status.Should().Be(ShiftStatus.Open.ToString());
        repoMock.Verify(r => r.AddAsync(It.Is<Shift>(s =>
            s.UserId == userId &&
            s.OpeningCash == 500_000m &&
            s.Status == ShiftStatus.Open)), Times.Once);
    }

    [Fact]
    public async Task OpenShiftAsync_UserAlreadyHasOpenShift_ThrowsBusinessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingShift = new Shift { Id = Guid.NewGuid(), UserId = userId, Status = ShiftStatus.Open };
        var repoMock = new Mock<IShiftRepository>();
        repoMock.Setup(r => r.GetOpenShiftAsync(userId)).ReturnsAsync(existingShift);

        var service = CreateService(repoMock.Object);
        var dto = new OpenShiftDto(userId, 100_000m);

        // Act
        var act = async () => await service.OpenShiftAsync(dto);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đang có ca*");
    }

    [Fact]
    public async Task OpenShiftAsync_NegativeOpeningCash_ThrowsBusinessException()
    {
        // Arrange
        var repoMock = new Mock<IShiftRepository>();
        var service = CreateService(repoMock.Object);
        var dto = new OpenShiftDto(Guid.NewGuid(), -1m);

        // Act
        var act = async () => await service.OpenShiftAsync(dto);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*không được âm*");
    }

    [Fact]
    public async Task CloseShiftAsync_CalculatesCashDifferenceCorrectly()
    {
        // Arrange
        var shiftId = Guid.NewGuid();
        var shift = new Shift
        {
            Id = shiftId,
            UserId = Guid.NewGuid(),
            Status = ShiftStatus.Open,
            OpeningCash = 500_000m,
            OpenedAt = DateTime.UtcNow
        };

        var repoMock = new Mock<IShiftRepository>();
        repoMock.Setup(r => r.GetByIdAsync(shiftId)).ReturnsAsync(shift);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<Shift>())).Returns(Task.CompletedTask);
        repoMock.Setup(r => r.GetCashRevenueAsync(shiftId)).ReturnsAsync(200_000m);

        var service = CreateService(repoMock.Object);
        var dto = new CloseShiftDto(shiftId, 650_000m);

        // Act
        await service.CloseShiftAsync(dto);

        // Assert
        // ExpectedCash = OpeningCash (500k) + CashRevenue (200k) = 700k
        // CashDifference = ClosingCash (650k) - ExpectedCash (700k) = -50k
        repoMock.Verify(r => r.UpdateAsync(It.Is<Shift>(s =>
            s.Status == ShiftStatus.Closed &&
            s.ExpectedCash == 700_000m &&
            s.CashDifference == -50_000m &&
            s.ClosingCash == 650_000m)), Times.Once);
    }

    [Fact]
    public async Task CloseShiftAsync_AlreadyClosedShift_ThrowsBusinessException()
    {
        // Arrange
        var shiftId = Guid.NewGuid();
        var shift = new Shift
        {
            Id = shiftId,
            Status = ShiftStatus.Closed,
            OpenedAt = DateTime.UtcNow
        };

        var repoMock = new Mock<IShiftRepository>();
        repoMock.Setup(r => r.GetByIdAsync(shiftId)).ReturnsAsync(shift);

        var service = CreateService(repoMock.Object);
        var dto = new CloseShiftDto(shiftId, 500_000m);

        // Act
        var act = async () => await service.CloseShiftAsync(dto);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã được đóng*");
    }
}

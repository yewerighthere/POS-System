using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Implementations;
using Xunit;

namespace SmartPOS.Tests;

public class DeviceServiceTests
{
    [Fact]
    public async Task LogDeviceEventAsync_StoresDeviceLogWithEventAndMessage()
    {
        var deviceId = Guid.NewGuid();
        var deviceRepo = new Mock<IDeviceRepository>();
        var logRepo = new Mock<IDeviceLogRepository>();
        logRepo.Setup(r => r.AddAsync(It.IsAny<DeviceLog>())).Returns(Task.CompletedTask);
        var service = new DeviceService(deviceRepo.Object, logRepo.Object, NullLogger<DeviceService>.Instance);

        await service.LogDeviceEventAsync(deviceId, "PRINT", "In hóa đơn thành công");

        logRepo.Verify(r => r.AddAsync(It.Is<DeviceLog>(log =>
            log.DeviceId == deviceId &&
            log.EventType == "PRINT" &&
            log.Message == "In hóa đơn thành công" &&
            log.CreatedAt.Kind == DateTimeKind.Utc)), Times.Once);
    }

    [Fact]
    public async Task PrintAsync_LogsPrintEvent()
    {
        var deviceId = Guid.NewGuid();
        var deviceRepo = new Mock<IDeviceRepository>();
        var logRepo = new Mock<IDeviceLogRepository>();
        logRepo.Setup(r => r.AddAsync(It.IsAny<DeviceLog>())).Returns(Task.CompletedTask);
        var service = new DeviceService(deviceRepo.Object, logRepo.Object, NullLogger<DeviceService>.Instance);

        await service.PrintAsync(deviceId, "receipt payload");

        logRepo.Verify(r => r.AddAsync(It.Is<DeviceLog>(log =>
            log.DeviceId == deviceId &&
            log.EventType == "PRINT" &&
            log.Message == "receipt payload")), Times.Once);
    }

    [Fact]
    public async Task GetSimulatedDevicesAsync_ReturnsActiveDeviceLabels()
    {
        var deviceRepo = new Mock<IDeviceRepository>();
        deviceRepo.Setup(r => r.GetActiveSimulatedDevicesAsync()).ReturnsAsync(new List<Device>
        {
            new() { Id = Guid.NewGuid(), Name = "Máy in giả lập", DeviceType = "Printer", IsActive = true }
        });
        var logRepo = new Mock<IDeviceLogRepository>();
        var service = new DeviceService(deviceRepo.Object, logRepo.Object, NullLogger<DeviceService>.Instance);

        var devices = await service.GetSimulatedDevicesAsync();

        devices.Should().ContainSingle().Which.Should().Be("Máy in giả lập (Printer)");
    }
}

using Microsoft.Extensions.Logging;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;

namespace SmartPOS.Services.Implementations;

public class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IDeviceLogRepository _deviceLogRepository;
    private readonly ILogger<DeviceService> _logger;

    public DeviceService(
        IDeviceRepository deviceRepository,
        IDeviceLogRepository deviceLogRepository,
        ILogger<DeviceService> logger)
    {
        _deviceRepository = deviceRepository;
        _deviceLogRepository = deviceLogRepository;
        _logger = logger;
    }

    public async Task PrintAsync(Guid deviceId, string payload)
    {
        await LogDeviceEventAsync(deviceId, "PRINT", payload).ConfigureAwait(false);
        _logger.LogInformation("Giả lập in trên thiết bị {DeviceId}: {Payload}", deviceId, payload);
    }

    public async Task LogDeviceEventAsync(Guid? deviceId, string eventType, string message)
    {
        await _deviceLogRepository.AddAsync(new DeviceLog
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            EventType = eventType,
            Message = message,
            CreatedAt = DateTime.UtcNow
        }).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> GetSimulatedDevicesAsync()
    {
        var devices = await _deviceRepository.GetActiveSimulatedDevicesAsync().ConfigureAwait(false);
        return devices.Select(device => $"{device.Name} ({device.DeviceType})").ToList();
    }
}

using SmartPOS.Data.Entities;

namespace SmartPOS.Data.Repositories.Interfaces;

public interface IDeviceLogRepository
{
    Task AddAsync(DeviceLog log);
}


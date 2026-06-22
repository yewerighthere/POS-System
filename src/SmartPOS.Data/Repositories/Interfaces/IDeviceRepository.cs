using SmartPOS.Data.Entities;

namespace SmartPOS.Data.Repositories.Interfaces;

public interface IDeviceRepository
{
    Task<IReadOnlyList<Device>> GetActiveSimulatedDevicesAsync(); Task AddAsync(Device device); Task UpdateAsync(Device device);
}


using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;

namespace SmartPOS.Data.Repositories.Implementations;

public class DeviceRepository : IDeviceRepository
{
    private readonly AppDbContext _context;

    public DeviceRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<IReadOnlyList<Device>> GetActiveSimulatedDevicesAsync()
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Device device)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Device device)
    {
        throw new NotImplementedException();
    }
}


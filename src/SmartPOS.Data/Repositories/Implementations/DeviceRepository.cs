using Microsoft.EntityFrameworkCore;
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

    public async Task<IReadOnlyList<Device>> GetActiveSimulatedDevicesAsync()
    {
        return await _context.Devices
            .Where(device => device.IsActive)
            .OrderBy(device => device.Name)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Device device)
    {
        await _context.Devices.AddAsync(device).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(Device device)
    {
        _context.Devices.Update(device);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}

using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;

namespace SmartPOS.Data.Repositories.Implementations;

public class DeviceLogRepository : IDeviceLogRepository
{
    private readonly AppDbContext _context;

    public DeviceLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(DeviceLog log)
    {
        throw new NotImplementedException();
    }
}


using System.Threading.Tasks;
using SmartPOS.Shared.DTOs.Dashboard;

namespace SmartPOS.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync();
}

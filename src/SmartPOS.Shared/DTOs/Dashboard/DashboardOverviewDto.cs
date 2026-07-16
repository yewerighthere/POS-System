using System.Collections.Generic;

namespace SmartPOS.Shared.DTOs.Dashboard;

public class DashboardOverviewDto
{
    public decimal TodayRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int NewCustomers { get; set; }
    public int InventoryAlerts { get; set; }

    public List<RevenueDataPointDto> Revenue7Days { get; set; } = new();
    public List<CategoryShareDto> TopCategories { get; set; } = new();

    public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
    public List<ActivePromotionDto> ActivePromotions { get; set; } = new();
}

public class RevenueDataPointDto
{
    public string Date { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class CategoryShareDto
{
    public string CategoryName { get; set; } = string.Empty;
    public double Percentage { get; set; }
}

public class RecentTransactionDto
{
    public string OrderId { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ActivePromotionDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ValidUntil { get; set; } = string.Empty;
}

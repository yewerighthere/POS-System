using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Dashboard;

namespace SmartPOS.Services.Implementations;

public class DashboardService : IDashboardService
{
    public async Task<DashboardOverviewDto> GetOverviewAsync()
    {
        // Simulate network delay
        await Task.Delay(500);

        return new DashboardOverviewDto
        {
            TodayRevenue = 15240.50m,
            TotalOrders = 128,
            NewCustomers = 15,
            InventoryAlerts = 3,
            
            Revenue7Days = new List<RevenueDataPointDto>
            {
                new() { Date = "Mon", Value = 12000m },
                new() { Date = "Tue", Value = 13500m },
                new() { Date = "Wed", Value = 11000m },
                new() { Date = "Thu", Value = 16000m },
                new() { Date = "Fri", Value = 18500m },
                new() { Date = "Sat", Value = 22000m },
                new() { Date = "Sun", Value = 19000m },
            },
            
            TopCategories = new List<CategoryShareDto>
            {
                new() { CategoryName = "Beverages", Percentage = 45 },
                new() { CategoryName = "Food", Percentage = 30 },
                new() { CategoryName = "Snacks", Percentage = 15 },
                new() { CategoryName = "Others", Percentage = 10 },
            },

            RecentTransactions = new List<RecentTransactionDto>
            {
                new() { OrderId = "ORD-1001", Time = "10:15 AM", Amount = 150.00m, Status = "Completed" },
                new() { OrderId = "ORD-1002", Time = "10:22 AM", Amount = 45.50m, Status = "Completed" },
                new() { OrderId = "ORD-1003", Time = "10:30 AM", Amount = 210.00m, Status = "Completed" },
                new() { OrderId = "ORD-1004", Time = "10:45 AM", Amount = 85.00m, Status = "Completed" },
                new() { OrderId = "ORD-1005", Time = "11:00 AM", Amount = 320.00m, Status = "Completed" }
            },

            ActivePromotions = new List<ActivePromotionDto>
            {
                new() { Name = "Summer Sale", Description = "10% off on all cold beverages", ValidUntil = "31 Jul 2026" },
                new() { Name = "Weekend Combo", Description = "Buy 1 get 1 free on selected foods", ValidUntil = "15 Aug 2026" }
            }
        };
    }
}

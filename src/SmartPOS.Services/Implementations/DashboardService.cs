using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Data;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Dashboard;
using SmartPOS.Shared.Enums;

namespace SmartPOS.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync()
    {
        var localToday = DateTime.Today; // local mid-night
        var utcTodayStart = localToday.ToUniversalTime();
        var utcTodayEnd = localToday.AddDays(1).ToUniversalTime();

        // 1. Today Revenue (completed/confirmed orders with successful payment)
        var todayOrdersQuery = _context.Orders
            .Where(o => o.CreatedAt >= utcTodayStart && o.CreatedAt < utcTodayEnd);

        var todayRevenue = await todayOrdersQuery
            .Where(o => o.PaymentStatus == PaymentStatus.Success)
            .SumAsync(o => (decimal?)o.TotalAmount)
            .ConfigureAwait(false) ?? 0m;

        // 2. Total Orders today
        var totalOrders = await todayOrdersQuery
            .CountAsync(o => o.Status == OrderStatus.Confirmed)
            .ConfigureAwait(false);

        // 3. New Customers today
        var newCustomers = await _context.Customers
            .CountAsync(c => c.CreatedAt >= utcTodayStart && c.CreatedAt < utcTodayEnd)
            .ConfigureAwait(false);

        // 4. Inventory Alerts (local stock < 10)
        var inventoryAlerts = await _context.Products
            .CountAsync(p => p.IsActive && p.LocalStockQuantity < 10)
            .ConfigureAwait(false);

        // 5. Revenue 7 Days (including today)
        var last7Days = Enumerable.Range(0, 7)
            .Select(i => localToday.AddDays(-i))
            .Reverse()
            .ToList();

        var revenue7Days = new List<RevenueDataPointDto>();
        foreach (var day in last7Days)
        {
            var dayStart = day.ToUniversalTime();
            var dayEnd = day.AddDays(1).ToUniversalTime();
            var dayRev = await _context.Orders
                .Where(o => o.CreatedAt >= dayStart && o.CreatedAt < dayEnd && o.PaymentStatus == PaymentStatus.Success)
                .SumAsync(o => (decimal?)o.TotalAmount)
                .ConfigureAwait(false) ?? 0m;

            revenue7Days.Add(new RevenueDataPointDto
            {
                Date = day.ToString("dd/MM"),
                Value = dayRev
            });
        }

        // 6. Top Categories
        // We'll calculate the share based on order items quantity
        var totalQuantitySold = await _context.OrderItems
            .Where(oi => oi.Order.Status == OrderStatus.Confirmed)
            .SumAsync(oi => (double?)oi.Quantity)
            .ConfigureAwait(false) ?? 0.0;

        var topCategories = new List<CategoryShareDto>();
        if (totalQuantitySold > 0)
        {
            var categoryStats = await _context.OrderItems
                .Where(oi => oi.Order.Status == OrderStatus.Confirmed && oi.Product.Category != null)
                .GroupBy(oi => oi.Product.Category.Name)
                .Select(g => new
                {
                    CategoryName = g.Key,
                    Quantity = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .ToListAsync()
                .ConfigureAwait(false);

            double accountedPercentage = 0;
            foreach (var cat in categoryStats.Take(3))
            {
                var percentage = Math.Round((cat.Quantity / totalQuantitySold) * 100, 1);
                accountedPercentage += percentage;
                topCategories.Add(new CategoryShareDto
                {
                    CategoryName = cat.CategoryName,
                    Percentage = percentage
                });
            }

            if (accountedPercentage < 100 && totalQuantitySold > 0)
            {
                topCategories.Add(new CategoryShareDto
                {
                    CategoryName = "Khác",
                    Percentage = Math.Round(100 - accountedPercentage, 1)
                });
            }
        }
        else
        {
            topCategories = new List<CategoryShareDto>
            {
                new() { CategoryName = "Đồ uống", Percentage = 40 },
                new() { CategoryName = "Thức ăn", Percentage = 40 },
                new() { CategoryName = "Khác", Percentage = 20 }
            };
        }

        // 7. Recent Transactions (latest 5 orders)
        var recentOrders = await _context.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new
            {
                o.Id,
                o.CreatedAt,
                o.TotalAmount,
                o.PaymentStatus,
                o.PaymentMethod
            })
            .ToListAsync()
            .ConfigureAwait(false);

        var recentTransactions = recentOrders.Select(o => new RecentTransactionDto
        {
            OrderId = o.Id.ToString().Substring(0, 8).ToUpper(),
            Time = o.CreatedAt.ToLocalTime().ToString("HH:mm"),
            Amount = o.TotalAmount,
            Status = o.PaymentStatus == PaymentStatus.Success ? "Thành công" 
                   : o.PaymentStatus == PaymentStatus.Pending ? "Chờ xử lý" 
                   : "Thất bại"
        }).ToList();

        // 8. Active Promotions (currently valid promotions)
        var todayDate = DateOnly.FromDateTime(DateTime.Today);
        var promotions = await _context.Promotions
            .Where(p => p.IsActive && p.StartDate <= todayDate && p.EndDate >= todayDate)
            .Take(3)
            .ToListAsync()
            .ConfigureAwait(false);

        var activePromotions = promotions.Select(p => new ActivePromotionDto
        {
            Name = p.Name,
            Description = p.Description ?? $"Giảm {p.DiscountValue:N0} " + (p.Type == "Percentage" ? "%" : "₫"),
            ValidUntil = p.EndDate.ToString("dd/MM/yyyy")
        }).ToList();

        return new DashboardOverviewDto
        {
            TodayRevenue = todayRevenue,
            TotalOrders = totalOrders,
            NewCustomers = newCustomers,
            InventoryAlerts = inventoryAlerts,
            Revenue7Days = revenue7Days,
            TopCategories = topCategories,
            RecentTransactions = recentTransactions,
            ActivePromotions = activePromotions
        };
    }
}

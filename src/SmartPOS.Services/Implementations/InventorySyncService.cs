using Microsoft.Extensions.Logging;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Auth;
using SmartPOS.Shared.DTOs.Cart;
using SmartPOS.Shared.DTOs.Catalog;
using SmartPOS.Shared.DTOs.Customer;
using SmartPOS.Shared.DTOs.Inventory;
using SmartPOS.Shared.DTOs.Invoice;
using SmartPOS.Shared.DTOs.Order;
using SmartPOS.Shared.DTOs.Payment;
using SmartPOS.Shared.DTOs.Product;
using SmartPOS.Shared.DTOs.Promotion;
using SmartPOS.Shared.DTOs.Report;
using SmartPOS.Shared.DTOs.Return;
using SmartPOS.Shared.DTOs.Shift;
using SmartPOS.Shared.Enums;
using System.Net.Http.Json;

namespace SmartPOS.Services.Implementations;

public class InventorySyncService : IInventorySyncService
{
    private readonly ILogger<InventorySyncService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IInventorySyncLogRepository _syncLogRepository;

    public InventorySyncService(ILogger<InventorySyncService> logger, HttpClient httpClient, IInventorySyncLogRepository syncLogRepository)
    {
        _logger = logger;
        _httpClient = httpClient;
        _syncLogRepository = syncLogRepository;
    }

    public async Task<SyncResultDto> SyncCatalogAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/sync/catalog");
            response.EnsureSuccessStatusCode();

            var items = await response.Content
                .ReadFromJsonAsync<List<InventoryCatalogItemDto>>() ?? new();

            _logger.LogInformation("SyncCatalog: {Count} items", items.Count);

            await _syncLogRepository.AddAsync(new InventorySyncLog
            {
                Id = Guid.NewGuid(),
                SyncType = "CATALOG",
                Status = SyncStatus.Success,
                Message = $"Đồng bộ catalog: {items.Count} sản phẩm",
                SyncedAt = DateTime.UtcNow
            });

            return new SyncResultDto
            {
                Status = "SUCCESS",
                Message = $"Đồng bộ catalog thành công: {items.Count} sản phẩm",
                AffectedRows = items.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncCatalog failed");

            await _syncLogRepository.AddAsync(new InventorySyncLog
            {
                Id = Guid.NewGuid(),
                SyncType = "CATALOG",
                Status = SyncStatus.Failed,
                Message = ex.Message,
                SyncedAt = DateTime.UtcNow
            });

            return new SyncResultDto
            {
                Status = "FAILED",
                Message = "Không thể kết nối Inventory API: " + ex.Message,
                AffectedRows = 0
            };
        }
    }

    public async Task<SyncResultDto> SyncStockAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/sync/stock");
            response.EnsureSuccessStatusCode();

            var items = await response.Content
                .ReadFromJsonAsync<List<InventoryStockItemDto>>() ?? new();

            _logger.LogInformation("SyncStock: {Count} items", items.Count);

            await _syncLogRepository.AddAsync(new InventorySyncLog
            {
                Id = Guid.NewGuid(),
                SyncType = "STOCK",
                Status = SyncStatus.Success,
                Message = $"Đồng bộ tồn kho: {items.Count} mặt hàng",
                SyncedAt = DateTime.UtcNow
            });

            return new SyncResultDto
            {
                Status = "SUCCESS",
                Message = $"Đồng bộ tồn kho thành công: {items.Count} mặt hàng",
                AffectedRows = items.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncStock failed");

            await _syncLogRepository.AddAsync(new InventorySyncLog
            {
                Id = Guid.NewGuid(),
                SyncType = "STOCK",
                Status = SyncStatus.Failed,
                Message = ex.Message,
                SyncedAt = DateTime.UtcNow
            });
            return new SyncResultDto
            {
                Status = "FAILED",
                Message = "Không thể kết nối Inventory API: " + ex.Message,
                AffectedRows = 0
            };
        }
    }

    public async Task SendStockDeductionAsync(IEnumerable<OrderItemDto> items, Guid orderId)
    {
        foreach (var item in items)
        {
            try
            {
                var dto = new StockDeductionEventDto(orderId, item.ProductId, item.Quantity);
                var response = await _httpClient.PostAsJsonAsync("/api/stock/deduct", dto);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Deducted stock for product {ProductId}", item.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deduct stock for product {ProductId}", item.ProductId);
            }
        }
    }

    public async Task RestockAsync(RestockEventDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/stock/restock", dto);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Restocked product {ProductId}", dto.ProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restock product {ProductId}", dto.ProductId);
        }
    }
}


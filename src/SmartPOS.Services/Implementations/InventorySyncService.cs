using Microsoft.Extensions.Logging;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Inventory;
using SmartPOS.Shared.DTOs.Order;
using SmartPOS.Shared.Enums;
using System.Net.Http.Json;

namespace SmartPOS.Services.Implementations;

public class InventorySyncService : IInventorySyncService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventorySyncService> _logger;
    private readonly IInventorySyncLogRepository _syncLogRepository;
    private readonly IProductRepository _productRepository;

    public InventorySyncService(
        ILogger<InventorySyncService> logger,
        HttpClient httpClient,
        IInventorySyncLogRepository syncLogRepository,
        IProductRepository productRepository)
    {
        _httpClient = httpClient;
        _logger = logger;
        _syncLogRepository = syncLogRepository;
        _productRepository = productRepository;
    }

    // ── TASK-1105: SyncCatalogAsync ───────────────────────────────────────────

    public async Task<SyncResultDto> SyncCatalogAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/sync/catalog");
            response.EnsureSuccessStatusCode();

            var items = await response.Content
                .ReadFromJsonAsync<List<InventoryCatalogItemDto>>() ?? new();

            _logger.LogInformation("SyncCatalog: nhận {Count} sản phẩm từ Inventory API", items.Count);

            int created = 0, updated = 0;

            foreach (var item in items)
            {
                var externalId = item.Id.ToString();
                var existing = await _productRepository.GetByExternalIdAsync(externalId);

                if (existing is null)
                {
                    // Tạo mới sản phẩm trong POS từ dữ liệu Inventory Manager
                    var newProduct = new Product
                    {
                        Id = Guid.NewGuid(),
                        ExternalInventoryId = externalId,
                        Name = item.Name,
                        Sku = item.Sku,
                        Barcode = item.Barcode,
                        UnitPrice = item.UnitPrice,
                        TaxRate = item.TaxRate,
                        IsActive = item.IsActive,
                        LocalStockQuantity = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        LastSyncedAt = DateTime.UtcNow
                    };
                    await _productRepository.AddAsync(newProduct);
                    created++;
                    _logger.LogInformation("SyncCatalog: tạo mới sản phẩm {Name} (ExternalId: {Id})", item.Name, externalId);
                }
                else
                {
                    // Cập nhật sản phẩm đã có trong POS
                    existing.Name = item.Name;
                    existing.Sku = item.Sku;
                    existing.Barcode = item.Barcode;
                    existing.UnitPrice = item.UnitPrice;
                    existing.TaxRate = item.TaxRate;
                    existing.IsActive = item.IsActive;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.LastSyncedAt = DateTime.UtcNow;
                    await _productRepository.UpdateAsync(existing);
                    updated++;
                }
            }

            var message = $"Đồng bộ catalog: tạo {created}, cập nhật {updated} sản phẩm";
            _logger.LogInformation("SyncCatalog hoàn tất: {Message}", message);

            await _syncLogRepository.AddAsync(new InventorySyncLog
            {
                Id = Guid.NewGuid(),
                SyncType = "CATALOG",
                Status = SyncStatus.Success,
                Message = message,
                SyncedAt = DateTime.UtcNow
            });

            return new SyncResultDto
            {
                Status = "SUCCESS",
                Message = message,
                AffectedRows = created + updated
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncCatalog thất bại");
            try { await _syncLogRepository.AddAsync(new InventorySyncLog { Id = Guid.NewGuid(), SyncType = "CATALOG", Status = SyncStatus.Failed, Message = ex.Message, SyncedAt = DateTime.UtcNow }); }
            catch (Exception logEx) { _logger.LogWarning(logEx, "Không ghi được sync log"); }

            return new SyncResultDto
            {
                Status = "FAILED",
                Message = "Không thể kết nối Inventory API: " + ex.Message,
                AffectedRows = 0
            };
        }
    }

    // ── TASK-1106: SyncStockAsync ─────────────────────────────────────────────

    public async Task<SyncResultDto> SyncStockAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/sync/stock");
            response.EnsureSuccessStatusCode();

            var items = await response.Content
                .ReadFromJsonAsync<List<InventoryStockItemDto>>() ?? new();

            _logger.LogInformation("SyncStock: nhận {Count} mặt hàng từ Inventory API", items.Count);

            int matched = 0, skipped = 0;

            foreach (var item in items)
            {
                var externalId = item.InventoryProductId.ToString();
                var product = await _productRepository.GetByExternalIdAsync(externalId);

                if (product is null)
                {
                    // Chưa sync catalog — bỏ qua, ghi warning
                    _logger.LogWarning("SyncStock: không tìm thấy sản phẩm POS cho ExternalId {Id} — hãy chạy Sync Catalog trước", externalId);
                    skipped++;
                    continue;
                }

                product.LocalStockQuantity = item.Quantity;
                product.LastSyncedAt = DateTime.UtcNow;
                product.UpdatedAt = DateTime.UtcNow;
                await _productRepository.UpdateAsync(product);
                matched++;
            }

            var message = $"Đồng bộ tồn kho: cập nhật {matched} sản phẩm, bỏ qua {skipped} (chưa sync catalog)";
            _logger.LogInformation("SyncStock hoàn tất: {Message}", message);

            await _syncLogRepository.AddAsync(new InventorySyncLog
            {
                Id = Guid.NewGuid(),
                SyncType = "STOCK",
                Status = SyncStatus.Success,
                Message = message,
                SyncedAt = DateTime.UtcNow
            });

            return new SyncResultDto
            {
                Status = "SUCCESS",
                Message = message,
                AffectedRows = matched
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncStock thất bại");
            try { await _syncLogRepository.AddAsync(new InventorySyncLog { Id = Guid.NewGuid(), SyncType = "STOCK", Status = SyncStatus.Failed, Message = ex.Message, SyncedAt = DateTime.UtcNow }); }
            catch (Exception logEx) { _logger.LogWarning(logEx, "Không ghi được sync log"); }

            return new SyncResultDto
            {
                Status = "FAILED",
                Message = "Không thể kết nối Inventory API: " + ex.Message,
                AffectedRows = 0
            };
        }
    }

    // ── SendStockDeductionAsync / RestockAsync (không đổi) ────────────────────

    public async Task SendStockDeductionAsync(IEnumerable<OrderItemDto> items, Guid orderId)
    {
        foreach (var item in items)
        {
            try
            {
                var dto = new StockDeductionEventDto(orderId, item.ProductId, item.Quantity);
                var response = await _httpClient.PostAsJsonAsync("/api/stock/deduct", dto);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Đã trừ kho sản phẩm {ProductId}", item.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Trừ kho thất bại cho sản phẩm {ProductId}", item.ProductId);
            }
        }
    }

    public async Task RestockAsync(RestockEventDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/stock/restock", dto);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Đã nhập lại hàng sản phẩm {ProductId}", dto.ProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nhập lại hàng thất bại cho sản phẩm {ProductId}", dto.ProductId);
        }
    }
}

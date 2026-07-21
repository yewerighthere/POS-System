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
    private readonly ICategoryRepository _categoryRepository;

    public InventorySyncService(
        ILogger<InventorySyncService> logger,
        HttpClient httpClient,
        IInventorySyncLogRepository syncLogRepository,
        IProductRepository productRepository,
        ICategoryRepository categoryRepository)
    {
        _httpClient = httpClient;
        _logger = logger;
        _syncLogRepository = syncLogRepository;
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
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

            // Nạp trước tất cả categories để map theo tên
            var allCategories = await _categoryRepository.GetAllAsync();
            var catByName = allCategories
                .GroupBy(c => c.Name.ToLower())
                .ToDictionary(g => g.Key, g => g.First());

            int created = 0, updated = 0;

            foreach (var item in items)
            {
                var externalId = item.Id.ToString();
                var existing = await _productRepository.GetByExternalIdAsync(externalId);

                if (existing is null)
                {
                    // Tạo mới sản phẩm trong POS từ dữ liệu Inventory Manager
                    Guid? categoryId = null;
                    if (!string.IsNullOrWhiteSpace(item.CategoryName) &&
                        catByName.TryGetValue(item.CategoryName.ToLower(), out var cat))
                        categoryId = cat.Id;

                    var newProduct = new Product
                    {
                        Id = Guid.NewGuid(),
                        ExternalInventoryId = externalId,
                        CategoryId = categoryId,
                        Name = item.Name,
                        Sku = item.Sku,
                        Barcode = item.Barcode,
                        QrCode = item.QrCode,
                        Description = item.Description,
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
                    // Cập nhật sản phẩm đã có trong POS (cập nhật cả CategoryId nếu chưa có)
                    if (!existing.CategoryId.HasValue && !string.IsNullOrWhiteSpace(item.CategoryName) &&
                        catByName.TryGetValue(item.CategoryName.ToLower(), out var cat))
                        existing.CategoryId = cat.Id;

                    existing.Name = item.Name;
                    existing.Sku = item.Sku;
                    existing.Barcode = item.Barcode;
                    existing.QrCode = item.QrCode;
                    existing.Description = item.Description;
                    existing.UnitPrice = item.UnitPrice;
                    existing.TaxRate = item.TaxRate;
                    
                    // Nếu trên Inventory Manager bị ngừng kinh doanh, thì ép POS ngừng.
                    // Ngược lại, tôn trọng trạng thái ngừng kinh doanh cục bộ của POS.
                    if (!item.IsActive) 
                    {
                        existing.IsActive = false;
                    }
                    
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
                AffectedRows = created + updated,
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
            try
            {
                await _syncLogRepository.AddAsync(new InventorySyncLog
                {
                    Id = Guid.NewGuid(),
                    SyncType = "CATALOG",
                    Status = SyncStatus.Failed,
                    Message = ex.Message,
                    AffectedRows = 0,
                    SyncedAt = DateTime.UtcNow
                });
            }
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

            var isPartial = skipped > 0;
            var message = $"Đồng bộ tồn kho: cập nhật {matched} sản phẩm, bỏ qua {skipped} (chưa sync catalog)";
            var status = isPartial ? SyncStatus.Partial : SyncStatus.Success;
            var statusStr = isPartial ? "PARTIAL" : "SUCCESS";

            _logger.LogInformation("SyncStock hoàn tất: {Message}", message);

            await _syncLogRepository.AddAsync(new InventorySyncLog
            {
                Id = Guid.NewGuid(),
                SyncType = "STOCK",
                Status = status,
                Message = message,
                AffectedRows = matched,
                SyncedAt = DateTime.UtcNow
            });

            return new SyncResultDto
            {
                Status = statusStr,
                Message = message,
                AffectedRows = matched
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncStock thất bại");
            try
            {
                await _syncLogRepository.AddAsync(new InventorySyncLog
                {
                    Id = Guid.NewGuid(),
                    SyncType = "STOCK",
                    Status = SyncStatus.Failed,
                    Message = ex.Message,
                    AffectedRows = 0,
                    SyncedAt = DateTime.UtcNow
                });
            }
            catch (Exception logEx) { _logger.LogWarning(logEx, "Không ghi được sync log"); }

            return new SyncResultDto
            {
                Status = "FAILED",
                Message = "Không thể kết nối Inventory API: " + ex.Message,
                AffectedRows = 0
            };
        }
    }

    // ── TASK-1103: SendStockDeductionAsync ────────────────────────────────────
    // Fix: lookup ExternalInventoryId trước, gửi đúng Inventory Product ID

    public async Task SendStockDeductionAsync(IEnumerable<OrderItemDto> items, Guid orderId)
    {
        foreach (var item in items)
        {
            try
            {
                // Lấy sản phẩm từ POS DB
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null)
                {
                    _logger.LogWarning("SendStockDeduction: không tìm thấy sản phẩm {ProductId} trong POS DB.", item.ProductId);
                    continue;
                }

                bool sentToInventory = false;

                if (!string.IsNullOrEmpty(product.ExternalInventoryId) &&
                    Guid.TryParse(product.ExternalInventoryId, out var inventoryProductId))
                {
                    // Sản phẩm liên kết Inventory API → gửi trừ kho về Inventory
                    try
                    {
                        var dto = new StockDeductionEventDto(orderId, inventoryProductId, item.Quantity);
                        var response = await _httpClient.PostAsJsonAsync("/api/stock/deduct", dto);
                        response.EnsureSuccessStatusCode();
                        sentToInventory = true;
                        _logger.LogInformation("Đã trừ kho Inventory API sản phẩm {ProductId} (InventoryId: {InvId})", item.ProductId, inventoryProductId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Trừ kho Inventory API thất bại cho sản phẩm {ProductId}", item.ProductId);
                    }
                }
                else
                {
                    _logger.LogWarning("SendStockDeduction: sản phẩm {ProductId} không có ExternalInventoryId, chỉ trừ LocalStock.", item.ProductId);
                }

                // Luôn trừ LocalStockQuantity trong POS DB sau khi bán
                // (dù có hay không liên kết Inventory API)
                var deduct = item.Quantity;
                if (product.LocalStockQuantity >= deduct)
                    product.LocalStockQuantity -= deduct;
                else
                    product.LocalStockQuantity = 0; // tránh âm kho

                // Tự động ngừng kinh doanh khi hết hàng
                if (product.LocalStockQuantity <= 0 && product.IsActive)
                {
                    product.IsActive = false;
                    _logger.LogInformation(
                        "Sản phẩm {ProductId} ({Name}) hết hàng sau khi bán — tự động chuyển sang Inactive.",
                        product.Id, product.Name);
                }

                product.UpdatedAt = DateTime.UtcNow;
                await _productRepository.UpdateAsync(product);
                _logger.LogInformation("Đã cập nhật LocalStockQuantity sản phẩm {ProductId}: -{Qty} (sentToInventory={Sent})",
                    item.ProductId, deduct, sentToInventory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Trừ kho thất bại cho sản phẩm {ProductId}", item.ProductId);
            }
        }
    }

    // ── RegisterProductToInventoryAsync ──────────────────────────────────────
    // Khi POS tạo sản phẩm mới, gọi method này để đồng ký lên Inventory API
    // và lấy về ExternalInventoryId để lưu vào Product.ExternalInventoryId.

    public async Task<RegisterInventoryProductResultDto?> RegisterProductToInventoryAsync(RegisterInventoryProductDto dto)
    {
        try
        {
            // Gọi POST /api/products trên Inventory API
            var response = await _httpClient.PostAsJsonAsync("/api/products", new
            {
                dto.Name,
                dto.Sku,
                dto.Barcode,
                dto.QrCode,
                dto.Description,
                dto.UnitPrice,
                dto.TaxRate,
                dto.CategoryId,
                dto.InitialStock
            });

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "RegisterProductToInventory: Inventory API trả về {Status} cho SKU '{Sku}'. Body: {Body}",
                    (int)response.StatusCode, dto.Sku, body);
                return null;
            }

            var result = await response.Content
                .ReadFromJsonAsync<RegisterInventoryProductResultDto>();

            if (result != null)
                _logger.LogInformation(
                    "RegisterProductToInventory: đã đăng ký sản phẩm '{Name}' lên Inventory, ExternalId = {Id}",
                    result.Name, result.Id);

            return result;
        }
        catch (Exception ex)
        {
            // Inventory API offline hoặc lỗi mạng → vẫn cho tạo trong POS,
            // nhưng ExternalInventoryId sẽ null (cần sync lại sau)
            _logger.LogError(ex,
                "RegisterProductToInventory: không thể kết nối Inventory API cho SKU '{Sku}'", dto.Sku);
            return null;
        }
    }

    // ── TASK-1104: RestockAsync ───────────────────────────────────────────────

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

    public async Task AdjustStockAsync(Guid inventoryProductId, int difference, string reason)
    {
        if (difference == 0) return;

        try
        {
            if (difference > 0)
            {
                var dto = new RestockEventDto(inventoryProductId, difference, reason);
                var response = await _httpClient.PostAsJsonAsync("/api/stock/restock", dto);
                response.EnsureSuccessStatusCode();
            }
            else
            {
                var dto = new StockDeductionEventDto(Guid.Empty, inventoryProductId, Math.Abs(difference));
                var response = await _httpClient.PostAsJsonAsync("/api/stock/deduct", dto);
                response.EnsureSuccessStatusCode();
            }
            _logger.LogInformation("Đã điều chỉnh kho (+{Diff}) cho InventoryId {InvId}", difference, inventoryProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Điều chỉnh kho thất bại cho InventoryId {InvId}", inventoryProductId);
        }
    }

    public async Task DeleteProductFromInventoryAsync(Guid inventoryProductId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/products/{inventoryProductId}");
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Đã xóa sản phẩm {InvId} khỏi Inventory Manager API", inventoryProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Xóa sản phẩm {InvId} khỏi Inventory Manager API thất bại", inventoryProductId);
        }
    }

    public async Task UpdateProductInInventoryAsync(Guid inventoryProductId, UpdateInventoryProductDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/products/{inventoryProductId}", dto);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Đã cập nhật sản phẩm {InvId} trên Inventory Manager API", inventoryProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cập nhật sản phẩm {InvId} trên Inventory Manager API thất bại", inventoryProductId);
        }
    }
}

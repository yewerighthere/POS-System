using SmartPOS.Shared.DTOs.Auth;
using SmartPOS.Shared.DTOs.Shift;
using SmartPOS.Shared.DTOs.Product;
using SmartPOS.Shared.DTOs.Cart;
using SmartPOS.Shared.DTOs.Order;
using SmartPOS.Shared.DTOs.Payment;
using SmartPOS.Shared.DTOs.Invoice;
using SmartPOS.Shared.DTOs.Customer;
using SmartPOS.Shared.DTOs.Return;
using SmartPOS.Shared.DTOs.Catalog;
using SmartPOS.Shared.DTOs.Inventory;
using SmartPOS.Shared.DTOs.Report;
using SmartPOS.Shared.DTOs.Promotion;
using SmartPOS.Shared.Enums;

namespace SmartPOS.Services.Interfaces;

public interface IInventorySyncService
{
    Task<SyncResultDto> SyncCatalogAsync();
    Task<SyncResultDto> SyncStockAsync();
    Task SendStockDeductionAsync(IEnumerable<OrderItemDto> items, Guid orderId);
    Task RestockAsync(RestockEventDto dto);

    /// <summary>
    /// Đăng ký sản phẩm mới lên Inventory API khi POS tạo sản phẩm.
    /// Trả về RegisterInventoryProductResultDto chứa InventoryProduct.Id
    /// để lưu vào Product.ExternalInventoryId.
    /// Trả về null nếu Inventory API không khả dụng (offline/lỗi).
    /// </summary>
    Task<RegisterInventoryProductResultDto?> RegisterProductToInventoryAsync(RegisterInventoryProductDto dto);
}



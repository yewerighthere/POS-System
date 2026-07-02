using InventoryManager.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Shared.DTOs.Inventory;

namespace InventoryManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly InventoryDbContext _context;

    public SyncController(InventoryDbContext context)
    {
        _context = context;
    }

    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog()
    {
        // Trả tất cả sản phẩm (kể cả inactive) để POS có thể cập nhật trạng thái
        var items = await _context.InventoryProducts
            .Include(p => p.Category)
            .Select(p => new InventoryCatalogItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                Barcode = p.Barcode,
                QrCode = p.QrCode,
                Description = p.Description,
                UnitPrice = p.UnitPrice,
                TaxRate = p.TaxRate,
                IsActive = p.IsActive,
                CategoryName = p.Category != null ? p.Category.Name : null
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("stock")]
    public async Task<IActionResult> GetStock()
    {
        var items = await _context.StockItems
            .Select(s => new InventoryStockItemDto
            {
                InventoryProductId = s.InventoryProductId,
                Quantity = s.Quantity,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync();

        return Ok(items);
    }
}

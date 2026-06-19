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
        var items = await _context.InventoryProducts
            .Where(p => p.IsActive)
            .Select(p => new InventoryCatalogItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                Barcode = p.Barcode,
                UnitPrice = p.UnitPrice,
                TaxRate = p.TaxRate,
                IsActive = p.IsActive
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

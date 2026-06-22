using InventoryManager.Api.Data;
using InventoryManager.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Shared.DTOs.Inventory;

namespace InventoryManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly InventoryDbContext _context;
    public StockController(InventoryDbContext context)
    {
        _context = context;
    }

    [HttpPost("deduct")]
    public async Task<IActionResult> Deduct([FromBody] StockDeductionEventDto dto)
    {
        var stock = await _context.StockItems
            .FirstOrDefaultAsync(s => s.InventoryProductId == dto.ProductId);
        if (stock == null)
            return NotFound(new { message = "Không tìm thấy sản phẩm trong kho." });
        if (stock.Quantity < dto.Quantity)
            return BadRequest(new { message = "Số lượng trong kho không đủ." });
        stock.Quantity -= dto.Quantity;
        stock.UpdatedAt = DateTime.UtcNow;
        _context.StockTransactions.Add(new StockTransaction
        {
            Id = Guid.NewGuid(),
            InventoryProductId = dto.ProductId,
            TransactionType = "DEDUCT",
            Quantity = dto.Quantity,
            ReferenceId = dto.OrderId.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return Ok(new { message = "Trừ kho thành công." });
    }

    [HttpPost("restock")]
    public async Task<IActionResult> Restock([FromBody] RestockEventDto dto)
    {
        var stock = await _context.StockItems
            .FirstOrDefaultAsync(s => s.InventoryProductId == dto.ProductId);
        if (stock == null)
            return NotFound(new { message = "Không tìm thấy sản phẩm trong kho." });
        stock.Quantity += dto.Quantity;
        stock.UpdatedAt = DateTime.UtcNow;
        _context.StockTransactions.Add(new StockTransaction
        {
            Id = Guid.NewGuid(),
            InventoryProductId = dto.ProductId,
            TransactionType = "RESTOCK",
            Quantity = dto.Quantity,
            ReferenceId = dto.Reason,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return Ok(new { message = "Nhập hàng thành công." });
    }
}

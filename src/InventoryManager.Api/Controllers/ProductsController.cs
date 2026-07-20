using InventoryManager.Api.Data;
using InventoryManager.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Api.Controllers;

/// <summary>
/// Quản lý sản phẩm trong Inventory Manager.
/// Tạo sản phẩm ở đây trước, rồi Sync Catalog từ POS để kéo về.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly InventoryDbContext _context;

    public ProductsController(InventoryDbContext context)
    {
        _context = context;
    }

    /// <summary>Lấy danh sách tất cả sản phẩm kèm tồn kho</summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _context.InventoryProducts
            .Include(p => p.Category)
            .Include(p => p.StockItem)
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Sku,
                p.Barcode,
                p.QrCode,
                p.Description,
                p.UnitPrice,
                p.TaxRate,
                p.IsActive,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : null,
                Stock = p.StockItem != null ? p.StockItem.Quantity : 0
            })
            .ToListAsync();

        return Ok(products);
    }

    /// <summary>
    /// Tạo sản phẩm mới trong Inventory Manager (kèm số lượng tồn kho ban đầu).
    /// Sau khi tạo xong, bấm Sync Catalog + Sync Tồn Kho trong POS để đồng bộ.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateInventoryProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Tên sản phẩm không được để trống." });

        if (string.IsNullOrWhiteSpace(request.Sku))
            return BadRequest(new { message = "SKU không được để trống." });

        // Kiểm tra SKU trùng
        if (await _context.InventoryProducts.AnyAsync(p => p.Sku == request.Sku))
            return Conflict(new { message = $"SKU '{request.Sku}' đã tồn tại." });

        // Kiểm tra Barcode trùng
        if (!string.IsNullOrWhiteSpace(request.Barcode) &&
            await _context.InventoryProducts.AnyAsync(p => p.Barcode == request.Barcode))
            return Conflict(new { message = $"Barcode '{request.Barcode}' đã tồn tại." });

        // Kiểm tra CategoryId nếu có
        if (request.CategoryId.HasValue &&
            !await _context.InventoryCategories.AnyAsync(c => c.Id == request.CategoryId.Value))
            return BadRequest(new { message = $"Danh mục không tồn tại." });

        var product = new InventoryProduct
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Sku = request.Sku.Trim(),
            Barcode = request.Barcode,
            QrCode = request.QrCode,
            Description = request.Description,
            UnitPrice = request.UnitPrice,
            TaxRate = request.TaxRate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.InventoryProducts.AddAsync(product);

        // Tạo StockItem ngay khi tạo sản phẩm
        var stockItem = new StockItem
        {
            Id = Guid.NewGuid(),
            InventoryProductId = product.Id,
            Quantity = request.InitialStock,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.StockItems.AddAsync(stockItem);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, new
        {
            product.Id,
            product.Name,
            product.Sku,
            product.Barcode,
            product.UnitPrice,
            product.IsActive,
            InitialStock = stockItem.Quantity,
            message = $"Đã tạo sản phẩm '{product.Name}' với tồn kho ban đầu {stockItem.Quantity}. Hãy bấm Sync Catalog rồi Sync Tồn Kho trong POS."
        });
    }

    /// <summary>
    /// Lấy danh sách danh mục để biết CategoryId khi tạo sản phẩm
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.InventoryCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();
        return Ok(categories);
    }
    /// <summary>
    /// Xóa sản phẩm khỏi Inventory Manager
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var product = await _context.InventoryProducts
            .Include(p => p.StockItem)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound(new { message = "Không tìm thấy sản phẩm." });

        if (product.StockItem != null)
        {
            _context.StockItems.Remove(product.StockItem);
        }

        _context.InventoryProducts.Remove(product);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã xóa sản phẩm thành công khỏi Inventory Manager." });
    }

    /// <summary>
    /// Cập nhật thông tin sản phẩm trong Inventory Manager
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] SmartPOS.Shared.DTOs.Inventory.UpdateInventoryProductDto request)
    {
        var product = await _context.InventoryProducts.FindAsync(id);
        if (product == null)
            return NotFound(new { message = "Không tìm thấy sản phẩm." });

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Tên sản phẩm không được để trống." });

        if (string.IsNullOrWhiteSpace(request.Sku))
            return BadRequest(new { message = "SKU không được để trống." });

        // Kiểm tra SKU trùng
        if (await _context.InventoryProducts.AnyAsync(p => p.Sku == request.Sku && p.Id != id))
            return Conflict(new { message = $"SKU '{request.Sku}' đã được sử dụng bởi sản phẩm khác." });

        // Kiểm tra Barcode trùng
        if (!string.IsNullOrWhiteSpace(request.Barcode) &&
            await _context.InventoryProducts.AnyAsync(p => p.Barcode == request.Barcode && p.Id != id))
            return Conflict(new { message = $"Barcode '{request.Barcode}' đã được sử dụng." });

        product.Name = request.Name.Trim();
        product.Sku = request.Sku.Trim();
        product.Barcode = request.Barcode;
        product.QrCode = request.QrCode;
        product.Description = request.Description;
        product.UnitPrice = request.UnitPrice;
        product.TaxRate = request.TaxRate;
        // product.CategoryId = request.CategoryId; // Lỗi khoá ngoại vì CategoryId của POS khác Inventory
        product.UpdatedAt = DateTime.UtcNow;

        _context.InventoryProducts.Update(product);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã cập nhật sản phẩm thành công trên Inventory Manager." });
    }
}

/// <summary>Request body để tạo sản phẩm mới trong Inventory Manager</summary>
public class CreateInventoryProductRequest
{
    /// <summary>Tên sản phẩm (bắt buộc)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mã SKU duy nhất (bắt buộc)</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Barcode (tuỳ chọn)</summary>
    public string? Barcode { get; set; }

    /// <summary>QR Code (tuỳ chọn)</summary>
    public string? QrCode { get; set; }

    /// <summary>Mô tả sản phẩm (tuỳ chọn)</summary>
    public string? Description { get; set; }

    /// <summary>Giá bán (VNĐ)</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Thuế suất (0 = không có thuế, 0.1 = 10%)</summary>
    public decimal TaxRate { get; set; } = 0;

    /// <summary>ID danh mục (lấy từ GET /api/products/categories)</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Số lượng tồn kho ban đầu (mặc định = 0)</summary>
    public int InitialStock { get; set; } = 0;
}

namespace SmartPOS.Shared.DTOs.Inventory;

/// <summary>
/// Request gửi lên Inventory API để đăng ký sản phẩm mới.
/// Dùng khi POS tạo sản phẩm mới → phải register lên Inventory để lấy ExternalInventoryId.
/// </summary>
public class RegisterInventoryProductDto
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? QrCode { get; set; }
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; } = 0;
    /// <summary>CategoryId bên Inventory (có thể null nếu chưa mapping)</summary>
    public Guid? CategoryId { get; set; }
    public int InitialStock { get; set; } = 0;
}

/// <summary>
/// Response từ Inventory API sau khi tạo sản phẩm thành công.
/// Id ở đây là InventoryProduct.Id → lưu vào Product.ExternalInventoryId bên POS.
/// </summary>
public class RegisterInventoryProductResultDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int InitialStock { get; set; }
    public string? Message { get; set; }
}

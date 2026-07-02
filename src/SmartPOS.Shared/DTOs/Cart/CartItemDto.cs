namespace SmartPOS.Shared.DTOs.Cart;

public class CartItemDto 
{ 
    public Guid ProductId { get; set; } 
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty; 
    public decimal UnitPrice { get; set; } 
    public int Quantity { get; set; } 
    public decimal Subtotal { get; set; } 
    public string? ProductImagePath { get; set; }
}


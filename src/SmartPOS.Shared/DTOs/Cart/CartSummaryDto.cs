namespace SmartPOS.Shared.DTOs.Cart;

public class CartSummaryDto { 
    public List<CartItemDto> Items { get; set; } = new(); 
    public SmartPOS.Shared.DTOs.Customer.CustomerDto? Customer { get; set; }
    public decimal Subtotal { get; set; } 
    public decimal DiscountAmount { get; set; } 
    public decimal PointsDiscountAmount { get; set; }
    public int PointsEarned { get; set; }
    public int PointsUsed { get; set; }
    public decimal TaxAmount { get; set; } 
    public decimal TotalAmount { get; set; } 
}


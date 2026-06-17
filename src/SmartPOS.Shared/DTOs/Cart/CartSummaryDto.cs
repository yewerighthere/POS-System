using System.Collections.ObjectModel;

namespace SmartPOS.Shared.DTOs.Cart;

public class CartSummaryDto
{
    public ObservableCollection<CartItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
}


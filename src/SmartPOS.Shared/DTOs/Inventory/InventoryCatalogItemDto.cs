using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPOS.Shared.DTOs.Inventory
{
    public class InventoryCatalogItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public bool IsActive { get; set; }
    }
}

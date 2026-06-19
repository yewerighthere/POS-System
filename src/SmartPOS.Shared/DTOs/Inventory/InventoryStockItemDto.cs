using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPOS.Shared.DTOs.Inventory
{
    public class InventoryStockItemDto
    {
        public Guid InventoryProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

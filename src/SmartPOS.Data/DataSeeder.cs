using Microsoft.EntityFrameworkCore;
using SmartPOS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPOS.Data
{
    public static class DataSeeder
    {
        // Các Guid cố định để map đúng với InventoryDataSeeder bên Inventory Manager
        public static readonly Guid InvId_CaPheSua          = new("11111111-0001-0001-0001-000000000001");
        public static readonly Guid InvId_TraSua             = new("11111111-0001-0001-0001-000000000002");
        public static readonly Guid InvId_NuocSuoi           = new("11111111-0001-0001-0001-000000000003");
        public static readonly Guid InvId_Coca               = new("11111111-0001-0001-0001-000000000004");
        public static readonly Guid InvId_ComGa              = new("11111111-0001-0001-0001-000000000005");
        public static readonly Guid InvId_BanhMi             = new("11111111-0001-0001-0001-000000000006");
        public static readonly Guid InvId_PhoBO              = new("11111111-0001-0001-0001-000000000007");
        public static readonly Guid InvId_BunBo              = new("11111111-0001-0001-0001-000000000008");
        public static readonly Guid InvId_Oreo               = new("11111111-0001-0001-0001-000000000009");
        public static readonly Guid InvId_Pringles           = new("11111111-0001-0001-0001-000000000010");

        public static async Task SeedAsync(AppDbContext context)
        {
            if (await context.Categorys.AnyAsync()) return;

            // Tạo danh mục
            var drinks = new Category { Id = Guid.NewGuid(), Name = "Đồ uống", Description = "Các loại đồ uống", IsActive = true };
            var food   = new Category { Id = Guid.NewGuid(), Name = "Thức ăn",  Description = "Các loại thức ăn",  IsActive = true };
            var snacks = new Category { Id = Guid.NewGuid(), Name = "Bánh snack", Description = "Bánh kẹo snack", IsActive = true };

            await context.Categorys.AddRangeAsync(drinks, food, snacks);
            await context.SaveChangesAsync();

            var products = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_CaPheSua.ToString(),  Name = "Cà phê sữa",              CategoryId = drinks.Id, Sku = "CF001", Barcode = "8934563140016", UnitPrice = 35000, TaxRate = 0, IsActive = true, LocalStockQuantity = 100 },
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_TraSua.ToString(),    Name = "Trà sữa trân châu",        CategoryId = drinks.Id, Sku = "TS001", Barcode = "8934563140017", UnitPrice = 45000, TaxRate = 0, IsActive = true, LocalStockQuantity = 80  },
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_NuocSuoi.ToString(),  Name = "Nước suối",                CategoryId = drinks.Id, Sku = "NS001", Barcode = "8934563140018", UnitPrice = 10000, TaxRate = 0, IsActive = true, LocalStockQuantity = 200 },
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_Coca.ToString(),      Name = "Nước ngọt Coca",           CategoryId = drinks.Id, Sku = "CC001", Barcode = "8934563140019", UnitPrice = 15000, TaxRate = 0, IsActive = true, LocalStockQuantity = 150 },
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_ComGa.ToString(),     Name = "Cơm gà xối mỡ",           CategoryId = food.Id,   Sku = "CG001", Barcode = "8934563140020", UnitPrice = 55000, TaxRate = 0, IsActive = true, LocalStockQuantity = 50  },
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_BanhMi.ToString(),    Name = "Bánh mì thịt",             CategoryId = food.Id,   Sku = "BM001", Barcode = "8934563140021", UnitPrice = 25000, TaxRate = 0, IsActive = true, LocalStockQuantity = 60  },
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_PhoBO.ToString(),     Name = "Phở bò",                   CategoryId = food.Id,   Sku = "PB001", Barcode = "8934563140022", UnitPrice = 65000, TaxRate = 0, IsActive = true, LocalStockQuantity = 40  },
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_BunBo.ToString(),     Name = "Bún bò Huế",               CategoryId = food.Id,   Sku = "BB001", Barcode = "8934563140023", UnitPrice = 60000, TaxRate = 0, IsActive = true, LocalStockQuantity = 40  },
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_Oreo.ToString(),      Name = "Bánh Oreo",                CategoryId = snacks.Id, Sku = "BO001", Barcode = "8934563140024", UnitPrice = 20000, TaxRate = 0, IsActive = true, LocalStockQuantity = 120 },
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_Pringles.ToString(),  Name = "Khoai tây chiên Pringles", CategoryId = snacks.Id, Sku = "KT001", Barcode = "8934563140025", UnitPrice = 55000, TaxRate = 0, IsActive = true, LocalStockQuantity = 90  },
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }
    }
}

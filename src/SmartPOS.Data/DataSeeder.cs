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
            await ForceUpdateProductImagesAsync(context);

            if (!await context.Customers.AnyAsync())
            {
                var customers = new List<Customer>
                {
                    new Customer { Id = Guid.NewGuid(), FullName = "Nguyễn Văn A", Phone = "0987654321", Email = "vana@gmail.com", MemberCode = "MEM001", LoyaltyPoints = 120, CreatedAt = DateTime.UtcNow, IsActive = true },
                    new Customer { Id = Guid.NewGuid(), FullName = "Trần Thị B", Phone = "0912345678", Email = "thib@gmail.com", MemberCode = "MEM002", LoyaltyPoints = 350, CreatedAt = DateTime.UtcNow, IsActive = true },
                    new Customer { Id = Guid.NewGuid(), FullName = "Lê Văn C", Phone = "0909090909", Email = "vanc@gmail.com", MemberCode = "MEM003", LoyaltyPoints = 50, CreatedAt = DateTime.UtcNow, IsActive = true }
                };
                await context.Customers.AddRangeAsync(customers);
                await context.SaveChangesAsync();
            }

            if (await context.Categorys.AnyAsync()) 
            {
                // Vẫn seed promotions nếu chưa có
                await SeedPromotionsAsync(context);
                return;
            }

            // Tạo danh mục
            var drinks = new Category { Id = Guid.NewGuid(), Name = "Đồ uống", Description = "Các loại đồ uống", IsActive = true };
            var food   = new Category { Id = Guid.NewGuid(), Name = "Thức ăn",  Description = "Các loại thức ăn",  IsActive = true };
            var snacks = new Category { Id = Guid.NewGuid(), Name = "Bánh snack", Description = "Bánh kẹo snack", IsActive = true };

            await context.Categorys.AddRangeAsync(drinks, food, snacks);
            await context.SaveChangesAsync();

            var products = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_CaPheSua.ToString(),  Name = "Cà phê sữa",              CategoryId = drinks.Id, Sku = "CF001", Barcode = "8934563140016", UnitPrice = 35000, TaxRate = 0, IsActive = true, LocalStockQuantity = 100, ImagePath = "Assets/Images/caphesua.jpg" },
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_TraSua.ToString(),    Name = "Trà sữa trân châu",        CategoryId = drinks.Id, Sku = "TS001", Barcode = "8934563140017", UnitPrice = 45000, TaxRate = 0, IsActive = true, LocalStockQuantity = 80,  ImagePath = "Assets/Images/trasua.jpg" },
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_NuocSuoi.ToString(),  Name = "Nước suối",                CategoryId = drinks.Id, Sku = "NS001", Barcode = "8934563140018", UnitPrice = 10000, TaxRate = 0, IsActive = true, LocalStockQuantity = 200, ImagePath = "Assets/Images/nuocsuoi.jpg" },
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_Coca.ToString(),      Name = "Nước ngọt Coca",           CategoryId = drinks.Id, Sku = "CC001", Barcode = "8934563140019", UnitPrice = 15000, TaxRate = 0, IsActive = true, LocalStockQuantity = 150, ImagePath = "Assets/Images/coca.jpg" },
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_ComGa.ToString(),     Name = "Cơm gà xối mỡ",           CategoryId = food.Id,   Sku = "CG001", Barcode = "8934563140020", UnitPrice = 55000, TaxRate = 0, IsActive = true, LocalStockQuantity = 50,  ImagePath = "Assets/Images/comga.jpg" },
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_BanhMi.ToString(),    Name = "Bánh mì thịt",             CategoryId = food.Id,   Sku = "BM001", Barcode = "8934563140021", UnitPrice = 25000, TaxRate = 0, IsActive = true, LocalStockQuantity = 60,  ImagePath = "Assets/Images/banhmi.jpg" },
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_PhoBO.ToString(),     Name = "Phở bò",                   CategoryId = food.Id,   Sku = "PB001", Barcode = "8934563140022", UnitPrice = 65000, TaxRate = 0, IsActive = true, LocalStockQuantity = 40,  ImagePath = "Assets/Images/phobo.jpg" },
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_BunBo.ToString(),     Name = "Bún bò Huế",               CategoryId = food.Id,   Sku = "BB001", Barcode = "8934563140023", UnitPrice = 60000, TaxRate = 0, IsActive = true, LocalStockQuantity = 40,  ImagePath = "Assets/Images/bunbo.jpg" },
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_Oreo.ToString(),      Name = "Bánh Oreo",                CategoryId = snacks.Id, Sku = "BO001", Barcode = "8934563140024", UnitPrice = 20000, TaxRate = 0, IsActive = true, LocalStockQuantity = 120, ImagePath = "Assets/Images/oreo.jpg" },
                new Product { Id = Guid.NewGuid(), ExternalInventoryId = InvId_Pringles.ToString(),  Name = "Khoai tây chiên Pringles", CategoryId = snacks.Id, Sku = "KT001", Barcode = "8934563140025", UnitPrice = 55000, TaxRate = 0, IsActive = true, LocalStockQuantity = 90,  ImagePath = "Assets/Images/pringles.jpg" },
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();

            await SeedPromotionsAsync(context);
        }

        private static async Task SeedPromotionsAsync(AppDbContext context)
        {
            if (!await context.Promotions.AnyAsync(p => p.Code == "GIAM10"))
            {
                await context.Promotions.AddAsync(new Promotion
                {
                    Id = Guid.NewGuid(),
                    Code = "GIAM10",
                    Name = "Giảm giá 10% đơn hàng",
                    Description = "Áp dụng cho đơn hàng từ 50k trở lên",
                    Type = "PERCENTAGE",
                    DiscountValue = 10,
                    MinOrderAmount = 50000,
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = new DateOnly(2026, 12, 31),
                    IsActive = true
                });
            }

            if (!await context.Promotions.AnyAsync(p => p.Code == "SALE20K"))
            {
                await context.Promotions.AddAsync(new Promotion
                {
                    Id = Guid.NewGuid(),
                    Code = "SALE20K",
                    Name = "Giảm giá 20,000đ",
                    Description = "Áp dụng cho đơn hàng từ 100k trở lên",
                    Type = "FLAT",
                    DiscountValue = 20000,
                    MinOrderAmount = 100000,
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = new DateOnly(2026, 12, 31),
                    IsActive = true
                });
            }

            await context.SaveChangesAsync();
        }

        private static async Task ForceUpdateProductImagesAsync(AppDbContext context)
        {
            var skuImageMapping = new Dictionary<string, string>
            {
                { "CF001", "Assets/Images/caphesua.jpg" },
                { "TS001", "Assets/Images/trasua.jpg" },
                { "NS001", "Assets/Images/nuocsuoi.jpg" },
                { "CC001", "Assets/Images/coca.jpg" },
                { "CG001", "Assets/Images/comga.jpg" },
                { "BM001", "Assets/Images/banhmi.jpg" },
                { "PB001", "Assets/Images/phobo.jpg" },
                { "BB001", "Assets/Images/bunbo.jpg" },
                { "BO001", "Assets/Images/oreo.jpg" },
                { "KT001", "Assets/Images/pringles.jpg" }
            };

            var dbProducts = await context.Products.ToListAsync();
            bool changed = false;

            foreach (var p in dbProducts)
            {
                if (skuImageMapping.TryGetValue(p.Sku, out var localPath))
                {
                    if (p.ImagePath != localPath)
                    {
                        p.ImagePath = localPath;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                await context.SaveChangesAsync();
            }
        }
    }
}

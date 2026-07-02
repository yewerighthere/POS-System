using InventoryManager.Api.Data;
using InventoryManager.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Api;

/// <summary>
/// Seed 3 categories và 10 products vào Inventory Manager,
/// dùng Guid cố định để map đúng với ExternalInventoryId trong POS DataSeeder.
/// </summary>
public static class InventoryDataSeeder
{
    // Các Guid khớp với DataSeeder.InvId_* bên POS
    private static readonly Guid InvId_CaPheSua  = new("11111111-0001-0001-0001-000000000001");
    private static readonly Guid InvId_TraSua    = new("11111111-0001-0001-0001-000000000002");
    private static readonly Guid InvId_NuocSuoi  = new("11111111-0001-0001-0001-000000000003");
    private static readonly Guid InvId_Coca      = new("11111111-0001-0001-0001-000000000004");
    private static readonly Guid InvId_ComGa     = new("11111111-0001-0001-0001-000000000005");
    private static readonly Guid InvId_BanhMi    = new("11111111-0001-0001-0001-000000000006");
    private static readonly Guid InvId_PhoBO     = new("11111111-0001-0001-0001-000000000007");
    private static readonly Guid InvId_BunBo     = new("11111111-0001-0001-0001-000000000008");
    private static readonly Guid InvId_Oreo      = new("11111111-0001-0001-0001-000000000009");
    private static readonly Guid InvId_Pringles  = new("11111111-0001-0001-0001-000000000010");

    public static async Task SeedAsync(InventoryDbContext context)
    {
        if (await context.InventoryProducts.AnyAsync()) return;

        // Tạo danh mục
        var catDrinks = new InventoryCategory { Id = Guid.NewGuid(), Name = "Đồ uống",   IsActive = true };
        var catFood   = new InventoryCategory { Id = Guid.NewGuid(), Name = "Thức ăn",    IsActive = true };
        var catSnacks = new InventoryCategory { Id = Guid.NewGuid(), Name = "Bánh snack", IsActive = true };

        await context.InventoryCategories.AddRangeAsync(catDrinks, catFood, catSnacks);
        await context.SaveChangesAsync();

        // Tạo sản phẩm với Guid cố định khớp với POS seed
        var products = new List<InventoryProduct>
        {
            new() { Id = InvId_CaPheSua, CategoryId = catDrinks.Id, Name = "Cà phê sữa",              Sku = "CF001", Barcode = "8934563140016", UnitPrice = 35000, TaxRate = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = InvId_TraSua,   CategoryId = catDrinks.Id, Name = "Trà sữa trân châu",        Sku = "TS001", Barcode = "8934563140017", UnitPrice = 45000, TaxRate = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = InvId_NuocSuoi, CategoryId = catDrinks.Id, Name = "Nước suối",                Sku = "NS001", Barcode = "8934563140018", UnitPrice = 10000, TaxRate = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = InvId_Coca,     CategoryId = catDrinks.Id, Name = "Nước ngọt Coca",           Sku = "CC001", Barcode = "8934563140019", UnitPrice = 15000, TaxRate = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = InvId_ComGa,    CategoryId = catFood.Id,   Name = "Cơm gà xối mỡ",           Sku = "CG001", Barcode = "8934563140020", UnitPrice = 55000, TaxRate = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = InvId_BanhMi,   CategoryId = catFood.Id,   Name = "Bánh mì thịt",             Sku = "BM001", Barcode = "8934563140021", UnitPrice = 25000, TaxRate = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = InvId_PhoBO,    CategoryId = catFood.Id,   Name = "Phở bò",                   Sku = "PB001", Barcode = "8934563140022", UnitPrice = 65000, TaxRate = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = InvId_BunBo,    CategoryId = catFood.Id,   Name = "Bún bò Huế",               Sku = "BB001", Barcode = "8934563140023", UnitPrice = 60000, TaxRate = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = InvId_Oreo,     CategoryId = catSnacks.Id, Name = "Bánh Oreo",                Sku = "BO001", Barcode = "8934563140024", UnitPrice = 20000, TaxRate = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = InvId_Pringles, CategoryId = catSnacks.Id, Name = "Khoai tây chiên Pringles", Sku = "KT001", Barcode = "8934563140025", UnitPrice = 55000, TaxRate = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
        };

        await context.InventoryProducts.AddRangeAsync(products);
        await context.SaveChangesAsync();

        // Tạo StockItem cho mỗi sản phẩm
        var stockItems = new List<StockItem>
        {
            new() { Id = Guid.NewGuid(), InventoryProductId = InvId_CaPheSua, Quantity = 100, UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), InventoryProductId = InvId_TraSua,   Quantity = 80,  UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), InventoryProductId = InvId_NuocSuoi, Quantity = 200, UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), InventoryProductId = InvId_Coca,     Quantity = 150, UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), InventoryProductId = InvId_ComGa,    Quantity = 50,  UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), InventoryProductId = InvId_BanhMi,   Quantity = 60,  UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), InventoryProductId = InvId_PhoBO,    Quantity = 40,  UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), InventoryProductId = InvId_BunBo,    Quantity = 40,  UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), InventoryProductId = InvId_Oreo,     Quantity = 120, UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), InventoryProductId = InvId_Pringles, Quantity = 90,  UpdatedAt = DateTime.UtcNow },
        };

        await context.StockItems.AddRangeAsync(stockItems);
        await context.SaveChangesAsync();
    }
}

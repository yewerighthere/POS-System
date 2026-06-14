using InventoryManager.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Api.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<InventoryCategory> InventoryCategories => Set<InventoryCategory>();
    public DbSet<InventoryProduct> InventoryProducts => Set<InventoryProduct>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InventoryCategory>(entity =>
        {
            entity.ToTable("inventory_categories");
            entity.HasKey(category => category.Id);
            entity.HasIndex(category => category.Name).IsUnique();
            entity.Property(category => category.Name).HasMaxLength(200).IsRequired();
            entity.Property(category => category.IsActive).IsRequired();
        });

        modelBuilder.Entity<InventoryProduct>(entity =>
        {
            entity.ToTable("inventory_products");
            entity.HasKey(product => product.Id);
            entity.HasIndex(product => product.Sku).IsUnique();
            entity.HasIndex(product => product.Barcode).IsUnique();
            entity.HasIndex(product => product.QrCode).IsUnique();
            entity.Property(product => product.Name).HasMaxLength(300).IsRequired();
            entity.Property(product => product.Sku).HasMaxLength(100).IsRequired();
            entity.Property(product => product.Barcode).HasMaxLength(100);
            entity.Property(product => product.QrCode).HasMaxLength(200);
            entity.Property(product => product.UnitPrice).HasColumnType("numeric(18,2)");
            entity.Property(product => product.TaxRate).HasColumnType("numeric(5,4)");
            entity.Property(product => product.IsActive).IsRequired();
            entity.Property(product => product.CreatedAt).IsRequired();

            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StockItem>(entity =>
        {
            entity.ToTable("stock_items");
            entity.HasKey(stockItem => stockItem.Id);
            entity.HasIndex(stockItem => stockItem.InventoryProductId).IsUnique();
            entity.Property(stockItem => stockItem.Quantity).IsRequired();
            entity.Property(stockItem => stockItem.UpdatedAt).IsRequired();

            entity.HasOne(stockItem => stockItem.InventoryProduct)
                .WithOne(product => product.StockItem)
                .HasForeignKey<StockItem>(stockItem => stockItem.InventoryProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.ToTable("stock_transactions");
            entity.HasKey(transaction => transaction.Id);
            entity.Property(transaction => transaction.TransactionType).HasMaxLength(50).IsRequired();
            entity.Property(transaction => transaction.ReferenceId).HasMaxLength(100);
            entity.Property(transaction => transaction.CreatedAt).IsRequired();

            entity.HasOne(transaction => transaction.InventoryProduct)
                .WithMany(product => product.StockTransactions)
                .HasForeignKey(transaction => transaction.InventoryProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

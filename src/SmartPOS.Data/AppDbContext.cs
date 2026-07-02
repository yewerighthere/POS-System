using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartPOS.Data.Entities;

namespace SmartPOS.Data;

public class AppDbContext : DbContext
{
    public AppDbContext() { }
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Category> Categorys => Set<Category>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceLog> DeviceLogs => Set<DeviceLog>();
    public DbSet<InventorySyncLog> InventorySyncLogs => Set<InventorySyncLog>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<Return> Returns => Set<Return>();
    public DbSet<ReturnItem> ReturnItems => Set<ReturnItem>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    private static string GetConnectionString()
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        return config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection in appsettings.json.");
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql(GetConnectionString());
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique indexes cho Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.Sku)
                  .IsUnique()
                  .HasFilter("\"Sku\" IS NOT NULL AND \"Sku\" != ''");
            entity.HasIndex(p => p.Barcode)
                  .IsUnique()
                  .HasFilter("\"Barcode\" IS NOT NULL");
            entity.HasIndex(p => p.QrCode)
                  .IsUnique()
                  .HasFilter("\"QrCode\" IS NOT NULL");
            entity.HasIndex(p => p.ExternalInventoryId)
                  .IsUnique()
                  .HasFilter("\"ExternalInventoryId\" IS NOT NULL");
        });

        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties()).Where(property => property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("numeric(18,2)");
        }
    }
}


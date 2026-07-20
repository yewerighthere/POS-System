using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartPOS.Data.Entities;

namespace SmartPOS.Data;

public class AppDbContext : DbContext
{
    private readonly SmartPOS.Shared.Interfaces.ICurrentUserService? _currentUserService;

    public AppDbContext() { }
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public AppDbContext(DbContextOptions<AppDbContext> options, SmartPOS.Shared.Interfaces.ICurrentUserService currentUserService) : base(options) 
    {
        _currentUserService = currentUserService;
    }

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

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_currentUserService != null)
        {
            ChangeTracker.DetectChanges();
            var entries = ChangeTracker.Entries().ToList();
            var auditLogs = new List<AuditLog>();
            
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (currentUserId != null && currentUserId != Guid.Empty)
            {
                var userId = currentUserId.Value;
                var now = DateTime.UtcNow;

                foreach (var entry in entries)
                {
                    if (entry.Entity is AuditLog || entry.Entity is DeviceLog || entry.Entity is InventorySyncLog || entry.Entity is UserSession)
                        continue;

                    if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                        continue;

                    var auditLog = new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Entity = entry.Entity.GetType().Name,
                        CreatedAt = now,
                        Action = entry.State.ToString()
                    };

                    var primaryKey = entry.Metadata.FindPrimaryKey();
                    var idProp = primaryKey?.Properties.FirstOrDefault(p => p.Name == "Id");
                    if (idProp != null && entry.State != EntityState.Added)
                    {
                        var val = entry.Property("Id").CurrentValue;
                        if (val != null)
                        {
                            auditLog.EntityId = Guid.Parse(val.ToString()!);
                        }
                    }

                    var oldValues = new Dictionary<string, object?>();
                    var newValues = new Dictionary<string, object?>();

                    foreach (var property in entry.Properties)
                    {
                        string propertyName = property.Metadata.Name;
                        
                        if (property.IsTemporary) continue;

                        switch (entry.State)
                        {
                            case EntityState.Added:
                                newValues[propertyName] = property.CurrentValue;
                                break;

                            case EntityState.Deleted:
                                oldValues[propertyName] = property.OriginalValue;
                                break;

                            case EntityState.Modified:
                                if (property.IsModified)
                                {
                                    oldValues[propertyName] = property.OriginalValue;
                                    newValues[propertyName] = property.CurrentValue;
                                }
                                break;
                        }
                    }

                    auditLog.OldValue = oldValues.Count == 0 ? null : System.Text.Json.JsonSerializer.Serialize(oldValues);
                    auditLog.NewValue = newValues.Count == 0 ? null : System.Text.Json.JsonSerializer.Serialize(newValues);

                    auditLogs.Add(auditLog);
                }

                if (auditLogs.Count > 0)
                {
                    AuditLogs.AddRange(auditLogs);
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}


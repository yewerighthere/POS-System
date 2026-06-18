using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartPOS.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5433;Database=smartpos;Username=postgres;Password=1",
            builder => builder.MigrationsAssembly("SmartPOS.Data"));

        return new AppDbContext(optionsBuilder.Options);
    }
}

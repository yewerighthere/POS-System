using Microsoft.EntityFrameworkCore;
using InventoryManager.Api.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<InventoryDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();

using Microsoft.EntityFrameworkCore;
using SmartPOS.Data;
using SmartPOS.Data.Repositories.Implementations;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Implementations;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Payment;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Primitives;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();
builder.Services.AddScoped<IShiftRepository, ShiftRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IReturnRepository, ReturnRepository>();
builder.Services.AddScoped<IInventorySyncLogRepository, InventorySyncLogRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IDeviceLogRepository, DeviceLogRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IReturnService, ReturnService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IInventorySyncService, InventorySyncService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddHttpClient<IInventorySyncService, InventorySyncService>(client => client.BaseAddress = new Uri(builder.Configuration["InventoryManager:BaseUrl"] ?? "http://localhost:5145"));
builder.Services.AddControllers();

var app = builder.Build();

app.MapPost("/api/vnpay/callback", async (HttpContext context, IPaymentService paymentService, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("VNPayCallback");

    IFormCollection form;
    if (context.Request.HasFormContentType)
    {
        form = await context.Request.ReadFormAsync().ConfigureAwait(false);
    }
    else
    {
        var data = await context.Request.ReadFromJsonAsync<Dictionary<string, string>>().ConfigureAwait(false) ?? new();
        form = new FormCollection(data.ToDictionary(item => item.Key, item => new StringValues(item.Value)));
    }

    var formValues = form.ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.OrdinalIgnoreCase);
    var secureHash = formValues.TryGetValue("vnp_SecureHash", out var hash) ? hash : string.Empty;
    var signData = BuildSignatureData(formValues);
    var expectedHash = CreateHmacSha512(builder.Configuration["VNPay:HashSecret"] ?? "YOUR_HASH_SECRET", signData);

    if (!string.Equals(secureHash, expectedHash, StringComparison.OrdinalIgnoreCase))
    {
        logger.LogWarning("Chữ ký VNPay không hợp lệ");
        return Results.BadRequest(new { message = "Chữ ký VNPay không hợp lệ" });
    }

    if (!TryParseCallback(formValues, out var callback))
    {
        return Results.BadRequest(new { message = "Dữ liệu callback không hợp lệ" });
    }

    var result = await paymentService.HandleVNPayCallbackAsync(callback).ConfigureAwait(false);
    return Results.Ok(result);
});

app.Run();

static bool TryParseCallback(IReadOnlyDictionary<string, string> formValues, out VNPayCallbackDto callback)
{
    callback = default!;

    if (!formValues.TryGetValue("vnp_TxnRef", out var txnRef) || !Guid.TryParse(txnRef, out var orderId))
        return false;

    if (!formValues.TryGetValue("vnp_TransactionNo", out var transactionNo))
        transactionNo = string.Empty;

    if (!formValues.TryGetValue("vnp_ResponseCode", out var responseCode))
        return false;

    callback = new VNPayCallbackDto(orderId, transactionNo, responseCode);
    return true;
}

static string BuildSignatureData(IReadOnlyDictionary<string, string> values)
{
    var filtered = values
        .Where(item => !string.IsNullOrWhiteSpace(item.Value))
        .Where(item => !string.Equals(item.Key, "vnp_SecureHash", StringComparison.OrdinalIgnoreCase))
        .OrderBy(item => item.Key, StringComparer.Ordinal);

    return string.Join("&", filtered.Select(item => $"{item.Key}={item.Value}"));
}

static string CreateHmacSha512(string key, string data)
{
    var keyBytes = Encoding.UTF8.GetBytes(key);
    using var hmac = new HMACSHA512(keyBytes);
    var dataBytes = Encoding.UTF8.GetBytes(data);
    return Convert.ToHexString(hmac.ComputeHash(dataBytes)).ToLowerInvariant();
}

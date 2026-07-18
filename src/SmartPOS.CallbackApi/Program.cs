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
using System.Net;

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
builder.Services.AddHttpClient<IInventorySyncService, InventorySyncService>(client => client.BaseAddress = new Uri(builder.Configuration["InventoryManager:BaseUrl"] ?? "http://localhost:5145"))
    .ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });
builder.Services.AddControllers();

var app = builder.Build();

app.MapMethods("/api/vnpay/callback", new[] { "GET", "POST" }, async (HttpContext context, IPaymentService paymentService, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("VNPayCallback");
    var values = await ReadVNPayValuesAsync(context).ConfigureAwait(false);
    var secureHash = values.TryGetValue("vnp_SecureHash", out var hash) ? hash : string.Empty;
    var signData = BuildSignatureData(values);
    var expectedHash = CreateHmacSha512(builder.Configuration["VNPay:HashSecret"] ?? "YOUR_HASH_SECRET", signData);

    if (!string.Equals(secureHash, expectedHash, StringComparison.OrdinalIgnoreCase))
    {
        logger.LogWarning("Chữ ký VNPay không hợp lệ");
        return Results.BadRequest(new { message = "Chữ ký VNPay không hợp lệ" });
    }

    if (!TryParseCallback(values, out var callback))
    {
        return Results.BadRequest(new { message = "Dữ liệu callback không hợp lệ" });
    }

    var result = await paymentService.HandleVNPayCallbackAsync(callback).ConfigureAwait(false);
    return Results.Content(BuildReturnPage(result.PaymentStatus), "text/html; charset=utf-8");
});

app.Run();

static async Task<Dictionary<string, string>> ReadVNPayValuesAsync(HttpContext context)
{
    if (context.Request.Query.Count > 0)
    {
        return context.Request.Query.ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    if (context.Request.HasFormContentType)
    {
        var form = await context.Request.ReadFormAsync().ConfigureAwait(false);
        return form.ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    return await context.Request.ReadFromJsonAsync<Dictionary<string, string>>().ConfigureAwait(false)
        ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

static string BuildReturnPage(string paymentStatus)
{
    var isSuccess = string.Equals(paymentStatus, "Success", StringComparison.OrdinalIgnoreCase);
    var title = isSuccess ? "Thanh toán VNPay thành công" : "Thanh toán VNPay thất bại";
    var message = isSuccess
        ? "Bạn có thể quay lại màn hình POS để xem hóa đơn."
        : "Vui lòng quay lại quầy thu ngân để thử lại hoặc chọn phương thức khác.";
    var color = isSuccess ? "#16a34a" : "#dc2626";

    return $$"""
        <!doctype html>
        <html lang="vi">
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{{title}}</title>
            <style>
                body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; margin: 0; background: #f8fafc; color: #0f172a; }
                .card { margin: 64px 20px; padding: 28px 22px; border-radius: 18px; background: white; box-shadow: 0 12px 32px rgba(15, 23, 42, .12); text-align: center; }
                .icon { width: 72px; height: 72px; border-radius: 999px; margin: 0 auto 18px; display: grid; place-items: center; background: {{color}}22; color: {{color}}; font-size: 42px; font-weight: 800; }
                h1 { font-size: 24px; margin: 0 0 12px; color: {{color}}; }
                p { font-size: 16px; line-height: 1.5; margin: 0; color: #475569; }
            </style>
        </head>
        <body>
            <main class="card">
                <div class="icon">{{(isSuccess ? "✓" : "!")}}</div>
                <h1>{{title}}</h1>
                <p>{{message}}</p>
            </main>
        </body>
        </html>
        """;
}

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

    return string.Join("&", filtered.Select(item => $"{item.Key}={WebUtility.UrlEncode(item.Value)}"));
}

static string CreateHmacSha512(string key, string data)
{
    var keyBytes = Encoding.UTF8.GetBytes(key);
    using var hmac = new HMACSHA512(keyBytes);
    var dataBytes = Encoding.UTF8.GetBytes(data);
    return Convert.ToHexString(hmac.ComputeHash(dataBytes)).ToLowerInvariant();
}

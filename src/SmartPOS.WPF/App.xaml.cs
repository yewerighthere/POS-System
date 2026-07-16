using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SmartPOS.Data;
using SmartPOS.Data.Repositories.Implementations;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Implementations;
using SmartPOS.Services.Interfaces;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;
using SmartPOS.WPF.ViewModels;
using SmartPOS.WPF.Views;
using System.IO;
using System.Windows;

namespace SmartPOS.WPF;

public partial class App : Application
{
    private ServiceProvider _serviceProvider = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, ex) =>
        {
            Log.Fatal(ex.Exception, "Unhandled dispatcher exception");
            Log.CloseAndFlush();
            ex.Handled = true;
            MessageBox.Show($"Lỗi không xử lý được:\n{ex.Exception.GetType().Name}: {ex.Exception.Message}\n\nXem logs/smartpos-*.log để biết chi tiết.", "Lỗi ứng dụng", MessageBoxButton.OK, MessageBoxImage.Error);
        };
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        var logPath = Path.Combine(ResolveLogRoot(), "logs", "smartpos-.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();
        services.AddLogging(builder => builder.AddSerilog(Log.Logger, dispose: true));
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, b => b.MigrationsAssembly("SmartPOS.Data")));
        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IUserSessionRepository, UserSessionRepository>();
        services.AddTransient<IShiftRepository, ShiftRepository>();
        services.AddTransient<IProductRepository, ProductRepository>();
        services.AddTransient<ICategoryRepository, CategoryRepository>();
        services.AddTransient<IPromotionRepository, PromotionRepository>();
        services.AddTransient<ICustomerRepository, CustomerRepository>();
        services.AddTransient<IOrderRepository, OrderRepository>();
        services.AddTransient<IInvoiceRepository, InvoiceRepository>();
        services.AddTransient<IReturnRepository, ReturnRepository>();
        services.AddTransient<IInventorySyncLogRepository, InventorySyncLogRepository>();
        services.AddTransient<IDeviceRepository, DeviceRepository>();
        services.AddTransient<IDeviceLogRepository, DeviceLogRepository>();
        services.AddTransient<IAuditLogRepository, AuditLogRepository>();
        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<IShiftService, ShiftService>();
        services.AddTransient<IProductService, ProductService>();
        services.AddTransient<ICartService, CartService>();
        services.AddTransient<IPromotionService, PromotionService>();
        services.AddTransient<IPaymentService, PaymentService>();
        services.AddTransient<IInvoiceService, InvoiceService>();
        services.AddTransient<IDeviceService, DeviceService>();
        services.AddTransient<ICustomerService, CustomerService>();
        services.AddTransient<IReturnService, ReturnService>();
        services.AddTransient<ICatalogService, CatalogService>();
        services.AddTransient<IReportService, ReportService>();
        services.AddTransient<IAuditService, AuditService>();
        services.AddTransient<IDashboardService, DashboardService>();
        services.AddHttpClient<IInventorySyncService, InventorySyncService>(client =>
        {
            client.BaseAddress = new Uri(configuration["InventoryManager:BaseUrl"] ?? "http://localhost:5145");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddSingleton<CurrentSessionContext>();
        services.AddSingleton<NavigationService>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ShiftViewModel>();
        services.AddTransient<SalesViewModel>();
        services.AddTransient<PaymentViewModel>();
        services.AddTransient<InvoiceViewModel>();
        services.AddTransient<CustomerViewModel>();
        services.AddTransient<ReturnViewModel>();
        services.AddTransient<CatalogViewModel>();
        services.AddTransient<PromotionViewModel>();
        services.AddTransient<ReportViewModel>();
        services.AddTransient<AuditLogViewModel>();
        services.AddTransient<SyncViewModel>();
        services.AddTransient<UserManagementViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<DashboardView>();
        services.AddTransient<DashboardCatalogPromoViewModel>();
        services.AddTransient<DashboardCatalogPromoView>();
        services.AddTransient<DashboardUserStaffViewModel>();
        services.AddTransient<DashboardUserStaffView>();
        services.AddTransient<DashboardInventoryViewModel>();
        services.AddTransient<DashboardInventoryView>();
        services.AddTransient<DashboardReportViewModel>();
        services.AddTransient<DashboardReportView>();
        services.AddTransient<ShiftView>();
        services.AddTransient<SalesView>();
        services.AddTransient<PaymentView>();
        services.AddTransient<InvoiceView>();
        services.AddTransient<CustomerView>();
        services.AddTransient<ReturnView>();
        services.AddTransient<CatalogView>();
        services.AddTransient<PromotionView>();
        services.AddTransient<ReportView>();
        services.AddTransient<AuditLogView>();
        services.AddTransient<SyncView>();
        services.AddTransient<UserManagementView>();
        services.AddTransient<LoginView>();
        services.AddTransient<MainWindow>();
        _serviceProvider = services.BuildServiceProvider();
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
            await DataSeeder.SeedAsync(db);
        }
        _serviceProvider.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static string ResolveLogRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SmartPOS.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        return AppContext.BaseDirectory;
    }
}


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
using System.Windows;

namespace SmartPOS.WPF;

public partial class App : Application
{
    private ServiceProvider _serviceProvider = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.AddSerilog(new LoggerConfiguration().WriteTo.File("logs/smartpos-.log", rollingInterval: RollingInterval.Day).CreateLogger()));
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")), ServiceLifetime.Transient);
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
        services.AddHttpClient<IInventorySyncService, InventorySyncService>(client => client.BaseAddress = new Uri("http://localhost:5001"));
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
        services.AddTransient<LoginView>();
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
        services.AddTransient<MainWindow>();
        _serviceProvider = services.BuildServiceProvider();
        _serviceProvider.GetRequiredService<MainWindow>().Show();
    }
}


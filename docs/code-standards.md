# Quy chuẩn viết mã

File này quy định cách viết mã cho toàn bộ dự án. Mục tiêu là thống nhất giữa các thành
viên và giúp tác nhân AI tạo mã đúng khuôn mẫu.

## Tên project và namespace

```text
SmartPOS.WPF
SmartPOS.Services
SmartPOS.Data
SmartPOS.Shared
SmartPOS.CallbackApi
InventoryManager.Api
SmartPOS.Tests
```

Tên file phải trùng với tên class chính trong file.

## Đặt tên

- Class và interface dùng PascalCase.
- Interface bắt đầu bằng `I`.
- Method dùng PascalCase.
- Method bất đồng bộ kết thúc bằng `Async`.
- Private field dùng `_camelCase`.
- Biến cục bộ và tham số dùng `camelCase`.
- DTO kết thúc bằng `Dto`.
- ViewModel kết thúc bằng `ViewModel`.
- Entity không thêm hậu tố `Entity` hay `Model`.

Ví dụ đúng:

```csharp
public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(CreateOrderDto dto);
}

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;

    public OrderService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }
}
```

## Kiến trúc bắt buộc

Luồng gọi chuẩn:

```text
View -> ViewModel -> Service -> Repository -> DbContext
```

Quy tắc:

- View chỉ hiển thị và nhận tương tác.
- ViewModel chỉ quản lý trạng thái giao diện và gọi Service.
- Service chứa quy tắc nghiệp vụ.
- Repository chứa truy vấn cơ sở dữ liệu.
- DbContext chỉ nằm trong Data.
- Service không gọi trực tiếp DbContext.
- ViewModel không gọi trực tiếp Repository.

## Dependency injection

Dùng `Microsoft.Extensions.DependencyInjection`.

Trong WPF:

- `CurrentSessionContext` là singleton.
- `NavigationService` là singleton.
- Service là transient.
- Repository là transient.
- ViewModel là transient.
- DbContext là transient vì WPF không có request scope.

Trong API:

- Service là scoped.
- Repository là scoped.
- DbContext là scoped.

Không dùng service locator trong ViewModel. Chỉ `NavigationService` được phép resolve
ViewModel từ container.

## Mẫu ViewModel

ViewModel dùng `CommunityToolkit.Mvvm`.

```csharp
public partial class SalesViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly ICartService _cartService;
    private readonly ILogger<SalesViewModel> _logger;

    [ObservableProperty]
    private string _barcodeInput = string.Empty;

    [ObservableProperty]
    private CartSummaryDto _cart = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public ObservableCollection<CartItemDto> CartItems { get; } = new();

    public SalesViewModel(
        IProductService productService,
        ICartService cartService,
        ILogger<SalesViewModel> logger)
    {
        _productService = productService;
        _cartService = cartService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task ScanBarcodeAsync()
    {
        if (string.IsNullOrWhiteSpace(BarcodeInput))
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var product = await _productService.FindByBarcodeAsync(BarcodeInput);
            if (product is null)
            {
                ErrorMessage = "Không tìm thấy sản phẩm, vui lòng kiểm tra mã hoặc đồng bộ danh mục";
                return;
            }

            Cart = _cartService.AddItem(product.Id, 1, Cart);
            RefreshCartDisplay();
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không mong muốn khi quét mã sản phẩm");
            ErrorMessage = "Đã có lỗi xảy ra, vui lòng kiểm tra nhật ký";
        }
        finally
        {
            IsLoading = false;
            BarcodeInput = string.Empty;
        }
    }

    private void RefreshCartDisplay()
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            CartItems.Clear();
            foreach (var item in Cart.Items)
                CartItems.Add(item);
        });
    }
}
```

Thông báo trong ViewModel phải viết tiếng Việt có dấu.

## Mẫu Service

```csharp
public class PaymentService : IPaymentService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventorySyncService _inventorySyncService;
    private readonly IAuditService _auditService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IOrderRepository orderRepository,
        IInventorySyncService inventorySyncService,
        IAuditService auditService,
        ILogger<PaymentService> logger)
    {
        _orderRepository = orderRepository;
        _inventorySyncService = inventorySyncService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<PaymentResultDto> RecordCashPaymentAsync(
        Guid orderId,
        decimal amountReceived,
        Guid userId)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(orderId)
            .ConfigureAwait(false)
            ?? throw new BusinessException("Không tìm thấy đơn hàng");

        if (order.IsLocked)
            throw new BusinessException("Đơn hàng đang bị khóa bởi phiên thanh toán khác");

        if (amountReceived < order.TotalAmount)
            throw new BusinessException("Số tiền khách đưa không đủ để thanh toán");

        var changeAmount = amountReceived - order.TotalAmount;

        order.PaymentMethod = PaymentMethod.Cash;
        order.PaymentStatus = PaymentStatus.Success;
        order.Status = OrderStatus.Confirmed;
        order.UpdatedAt = DateTime.UtcNow;

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            PaymentMethod = PaymentMethod.Cash,
            AmountReceived = amountReceived,
            ChangeAmount = changeAmount,
            PaymentStatus = PaymentStatus.Success,
            CreatedAt = DateTime.UtcNow
        };

        await _orderRepository.AddPaymentAsync(order, payment).ConfigureAwait(false);

        await _inventorySyncService.SendStockDeductionAsync(order.Items, orderId)
            .ConfigureAwait(false);

        await _auditService.LogAsync(
            "CASH_PAYMENT",
            "Order",
            orderId,
            null,
            new { orderId, amountReceived },
            userId).ConfigureAwait(false);

        _logger.LogInformation("Đã ghi nhận thanh toán tiền mặt cho đơn hàng {OrderId}", orderId);

        return new PaymentResultDto
        {
            OrderId = orderId,
            AmountReceived = amountReceived,
            ChangeAmount = changeAmount,
            PaymentStatus = PaymentStatus.Success
        };
    }
}
```

Quy tắc Service:

- Service trả DTO, không trả entity cho UI.
- Service ghi nhật ký cho thao tác thành công quan trọng.
- Lỗi nghiệp vụ ném `BusinessException`.
- Lỗi hạ tầng phải ghi nhật ký và đưa thông báo thân thiện cho người dùng.
- Mỗi `await` trong Service dùng `ConfigureAwait(false)`.

## Mẫu Repository

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<Order?> GetByIdWithItemsAsync(Guid id);
    Task AddAsync(Order order);
    Task AddPaymentAsync(Order order, Payment payment);
    Task UpdateAsync(Order order);
}

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdWithItemsAsync(Guid id)
    {
        return await _context.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id)
            .ConfigureAwait(false);
    }

    public async Task AddPaymentAsync(Order order, Payment payment)
    {
        _context.Orders.Update(order);
        await _context.Payments.AddAsync(payment).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}
```

Quy tắc Repository:

- Repository trả entity.
- Repository không chứa quy tắc nghiệp vụ.
- Repository được dùng EF Core.
- Repository phải dùng bất đồng bộ.
- Mỗi `await` trong Repository dùng `ConfigureAwait(false)`.

## Quy tắc bất đồng bộ

- Không dùng `.Result`.
- Không dùng `.Wait()`.
- Không dùng `.GetAwaiter().GetResult()`.
- ViewModel không dùng `ConfigureAwait(false)`.
- Service và Repository dùng `ConfigureAwait(false)`.
- Cập nhật `ObservableCollection` trên luồng giao diện.

## Xử lý lỗi

### Lỗi nghiệp vụ

Service ném `BusinessException`. ViewModel bắt lỗi và hiển thị thông báo cho người dùng.

Ví dụ thông báo:

```text
Sản phẩm đã hết hàng
Số tiền khách đưa không đủ để thanh toán
Cần quản lý phê duyệt khuyến mãi này
```

### Lỗi hạ tầng

Mạng, cơ sở dữ liệu, VNPay, Inventory Manager hoặc giả lập máy in bị lỗi thì phải ghi nhật ký.
Thông báo hiện ra cho người dùng phải ngắn gọn và có dấu.

```text
Không kết nối được Inventory Manager, vui lòng thử lại
Lỗi máy in, vui lòng kiểm tra cấu hình và thử lại
```

### Lỗi không mong muốn

ViewModel bắt `Exception`, ghi nhật ký và hiển thị thông báo chung:

```text
Đã có lỗi xảy ra, vui lòng kiểm tra nhật ký
```

## EF Core

- Khóa chính dùng `Guid`.
- Thời gian lưu UTC.
- Tiền dùng `decimal`.
- Cột tiền dùng `numeric(18,2)`.
- Không xóa cứng sản phẩm đã có giao dịch.
- Migration tạo bằng lệnh `dotnet ef migrations add`.
- Không sửa migration đã được chia sẻ cho cả nhóm.

## Ghi nhật ký

Dùng log có cấu trúc, không nối chuỗi thủ công.

Dùng:

```csharp
_logger.LogInformation("Đã ghi nhận thanh toán tiền mặt cho đơn hàng {OrderId}", orderId);
```

Không nên:

```csharp
_logger.LogInformation($"Đã ghi nhận thanh toán tiền mặt cho đơn hàng {orderId}");
```

Nội dung nhật ký phải là tiếng Việt có dấu.

## Không được làm

- Không thêm MediatR, CQRS, AutoMapper, Redis, Hangfire nếu chưa có quyết định của nhóm.
- Không để ViewModel gọi DbContext.
- Không để Service gọi trực tiếp DbContext.
- Không trả entity từ Service lên ViewModel.
- Không bind EF entity trực tiếp lên View.
- Không dùng static state để chia sẻ session.
- Không dùng MessageBox trực tiếp trong ViewModel.
- Không thêm package mới nếu chưa cập nhật docs và thống nhất với nhóm.

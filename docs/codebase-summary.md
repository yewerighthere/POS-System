# Tóm tắt mã nguồn

File này là bản đồ nhanh của mã nguồn. Khi cần biết một tính năng nằm ở đâu, hãy đọc file
này trước khi tìm sâu vào mã nguồn.

## Đồ thị phụ thuộc

```text
SmartPOS.Shared
  -> không phụ thuộc project nào

SmartPOS.Data
  -> phụ thuộc SmartPOS.Shared

SmartPOS.Services
  -> phụ thuộc SmartPOS.Shared
  -> phụ thuộc SmartPOS.Data để dùng repository interface và entity
  -> không dùng trực tiếp AppDbContext

SmartPOS.WPF
  -> phụ thuộc SmartPOS.Shared
  -> phụ thuộc SmartPOS.Services
  -> phụ thuộc SmartPOS.Data chỉ để cấu hình DI

SmartPOS.CallbackApi
  -> phụ thuộc SmartPOS.Shared
  -> phụ thuộc SmartPOS.Services
  -> phụ thuộc SmartPOS.Data chỉ để cấu hình DI

InventoryManager.Api
  -> hệ thống riêng
  -> dùng InventoryDbContext và cơ sở dữ liệu riêng
  -> giao tiếp với POS qua API
```

## SmartPOS.Shared

### Enums

Thư mục: `SmartPOS.Shared/Enums`

- `UserRole`: Staff, Manager, Admin.
- `UserStatus`: Active, Locked, Inactive.
- `ShiftStatus`: Open, Closed.
- `OrderStatus`: Draft, Confirmed, Cancelled.
- `PaymentMethod`: Cash, VNPay.
- `PaymentStatus`: Pending, Success, Failed, Timeout.
- `ReturnStatus`: Requested, Approved, Rejected.
- `SyncStatus`: Success, Failed, Partial.

### Exceptions

Thư mục: `SmartPOS.Shared/Exceptions`

- `BusinessException`: lỗi nghiệp vụ có thể hiển thị cho người dùng.
- `StockInsufficientException`: tồn kho không đủ.
- `PaymentLockException`: đơn hàng đang bị khóa.
- `PromotionInvalidException`: khuyến mãi không hợp lệ.

### Constants

Thư mục: `SmartPOS.Shared/Constants`

- `AppConstants.PointsPerVND = 10000`.
- `AppConstants.PointValueVND = 1000`.
- `AppConstants.VNPayVersion = "2.1.0"`.
- `AppConstants.InvoiceNumberPrefix = "INV"`.
- `AppConstants.MaxLoginAttempts = 5`.
- `AppConstants.PaymentPollIntervalSeconds = 2`.

### DTO

Thư mục: `SmartPOS.Shared/DTOs`

- Auth: `LoginRequestDto`, `LoginResponseDto`, `UserSessionDto`, `CreateUserDto`, `UserDto`.
- Shift: `OpenShiftDto`, `CloseShiftDto`, `ShiftDto`, `ShiftSummaryDto`.
- Product: `ProductDto`, `ProductSearchResultDto`, `SyncProductDto`.
- Cart: `CartItemDto`, `CartSummaryDto`.
- Order: `CreateOrderDto`, `OrderDto`, `OrderItemDto`, `OrderItemInputDto`.
- Payment: `CashPaymentDto`, `VNPayRequestDto`, `VNPayCallbackDto`, `PaymentResultDto`.
- Invoice: `InvoiceDto`.
- Customer: `CustomerDto`, `CreateCustomerDto`.
- Return: `ReturnRequestDto`, `ReturnItemInputDto`, `ReturnDto`.
- Catalog: `CategoryDto`, `CreateCategoryDto`, `CreateProductDto`, `UpdatePriceDto`.
- Promotion: `PromotionDto`, `PromotionValidationResultDto`.
- Inventory: `StockDeductionEventDto`, `RestockEventDto`, `SyncResultDto`.
- Report: `ShiftReportDto`, `SalesReportDto`.

## SmartPOS.Data

### Entities

Thư mục: `SmartPOS.Data/Entities`

- `User`
- `UserSession`
- `Shift`
- `Category`
- `Product`
- `Promotion`
- `Customer`
- `Order`
- `OrderItem`
- `Payment`
- `Invoice`
- `Return`
- `ReturnItem`
- `InventorySyncLog`
- `Device`
- `DeviceLog`
- `AuditLog`

`Promotion` phải có cột `Code` để phục vụ luồng nhập mã khuyến mãi.

### DbContext

File: `SmartPOS.Data/AppDbContext.cs`

Nhiệm vụ:

- Khai báo `DbSet`.
- Cấu hình quan hệ.
- Cấu hình decimal thành `numeric(18,2)`.
- Cấu hình tên bảng và tên cột nếu cần.

### Repository interface

Thư mục: `SmartPOS.Data/Repositories/Interfaces`

- `IUserRepository`: tìm theo username, tìm theo id, thêm, cập nhật.
- `IUserSessionRepository`: thêm phiên, cập nhật đăng xuất.
- `IShiftRepository`: lấy ca đang mở, tìm theo id, thêm, cập nhật.
- `IProductRepository`: tìm theo id, mã vạch, external id, tìm kiếm, thêm, cập nhật.
- `ICategoryRepository`: lấy danh sách, tìm theo id, thêm, cập nhật.
- `IPromotionRepository`: tìm theo code, lấy khuyến mãi đang hiệu lực, thêm, cập nhật.
- `ICustomerRepository`: tìm theo số điện thoại, mã thành viên, thêm, cập nhật.
- `IOrderRepository`: tìm đơn, lấy kèm dòng hàng, thêm, cập nhật, thêm payment.
- `IInvoiceRepository`: tìm theo đơn, lấy số thứ tự trong ngày, thêm.
- `IReturnRepository`: tìm theo id, tìm theo đơn, thêm, cập nhật.
- `IInventorySyncLogRepository`: thêm, lấy lần gần nhất.
- `IDeviceRepository`: lấy thiết bị giả lập đang hoạt động, thêm, cập nhật.
- `IDeviceLogRepository`: thêm nhật ký thiết bị.
- `IAuditLogRepository`: thêm nhật ký, tìm theo đối tượng, lấy gần đây.

## SmartPOS.Services

Thư mục interface: `SmartPOS.Services/Interfaces`

- `IAuthService`: đăng nhập, đăng xuất, kiểm tra JWT, tạo tài khoản demo khi cần.
- `IShiftService`: mở ca, đóng ca, lấy ca đang mở, tóm tắt ca.
- `IProductService`: tìm sản phẩm theo mã, tìm kiếm sản phẩm.
- `ICartService`: thêm, sửa, xóa, tính lại giỏ hàng.
- `IPromotionService`: kiểm tra mã, xin phê duyệt, áp dụng khuyến mãi.
- `IPaymentService`: tiền mặt, VNPay, callback, lấy trạng thái, hủy VNPay.
- `IInvoiceService`: tạo hóa đơn, lấy hóa đơn, in giả lập.
- `IDeviceService`: giả lập in, ghi nhật ký thiết bị, lấy thiết bị giả lập.
- `ICustomerService`: khách hàng và điểm tích lũy.
- `IReturnService`: tạo, duyệt, từ chối trả hàng.
- `ICatalogService`: danh mục, sản phẩm, giá.
- `IInventorySyncService`: đồng bộ danh mục, đồng bộ tồn, trừ kho, nhập lại hàng.
- `IReportService`: báo cáo ca, báo cáo doanh thu.
- `IAuditService`: ghi nhật ký thao tác.

Service implementation nằm trong `SmartPOS.Services/Implementations`.

## SmartPOS.WPF

### Startup

`App.xaml.cs` cấu hình DI, nạp cấu hình, đăng ký service, repository, ViewModel và mở
`MainWindow`.

`SmartPOS.WPF` references `Microsoft.EntityFrameworkCore.Design` so it can be used as the startup project for
`dotnet ef database update --project src\SmartPOS.Data --startup-project src\SmartPOS.WPF`.

Inventory sync is registered as a typed `HttpClient` service in WPF so manager/admin login can navigate to `SyncView`
without duplicate DI registrations.

### Session

`CurrentSessionContext` giữ người dùng hiện tại và ca đang mở.

### Navigation

`NavigationService` điều hướng giữa các màn hình bằng ViewModel.

### ViewModel

Thư mục: `SmartPOS.WPF/ViewModels`

- `LoginViewModel`: đăng nhập, lưu `CurrentSessionContext` và điều hướng theo role.
- `ShiftViewModel`: mở ca, đóng ca.
- `SalesViewModel`: màn hình bán hàng, giả lập máy quét, giỏ hàng.
- `PaymentViewModel`: tiền mặt và VNPay.
- `InvoiceViewModel`: xem trước hóa đơn và giả lập in.
- `CustomerViewModel`: tìm và tạo khách hàng.
- `ReturnViewModel`: tạo, duyệt, từ chối trả hàng.
- `CatalogViewModel`: danh mục, sản phẩm, giá.
- `PromotionViewModel`: khuyến mãi.
- `ReportViewModel`: báo cáo.
- `AuditLogViewModel`: nhật ký thao tác.
- `SyncViewModel`: đồng bộ Inventory Manager.
- `UserManagementViewModel`: tạo tài khoản khi demo cần.

### View

Thư mục: `SmartPOS.WPF/Views`

Mỗi ViewModel nên có View tương ứng.

- `LoginView`: giao diện split-screen theo thiết kế, có password toggle và binding về `LoginViewModel`.

### Control

Thư mục: `SmartPOS.WPF/Controls`

- `CartItemControl`
- `ProductSearchBox`
- `NumericInputControl`
- `LoadingOverlay`
- `ReceiptPreviewControl`
- `ScannerEmulatorControl`

### Converter

Thư mục: `SmartPOS.WPF/Converters`

- `BoolToVisibilityConverter`
- `StringToVisibilityConverter`
- `CurrencyFormatter`
- `InverseBoolConverter`

## SmartPOS.CallbackApi

File chính: `Program.cs`

Endpoint:

```text
POST /api/vnpay/callback
```

Nhiệm vụ:

- Đọc form callback.
- Kiểm tra chữ ký VNPay.
- Gọi `IPaymentService.HandleVNPayCallbackAsync`.
- Trả kết quả cho VNPay.

Thông báo nhật ký:

```text
Đã nhận callback VNPay
Chữ ký VNPay không hợp lệ
Đã cập nhật thanh toán VNPay thành công
```

## InventoryManager.Api

Hệ thống riêng, có cơ sở dữ liệu riêng.

DbContext:

- `InventoryDbContext`: context riêng của Inventory Manager, dùng connection string
  `Host=localhost;Port=5433;Database=inventory_manager;Username=postgres;Password=1`.
- Migration đầu tiên: `InitialInventoryCreate`.

Entities:

- `InventoryCategory`
- `InventoryProduct`
- `StockItem`
- `StockTransaction`

Controller đề xuất:

- `ProductsController`: danh sách sản phẩm tồn kho.
- `SyncController`: danh mục và tồn kho cho POS đồng bộ.
- `StockController`: trừ kho và nhập lại hàng.

Endpoint:

```text
GET  /api/sync/catalog
GET  /api/sync/stock
POST /api/stock/deduct
POST /api/stock/restock
```

## SmartPOS.Tests

Thư mục: `tests/SmartPOS.Tests`

Tập trung kiểm thử Service:

- `AuthServiceTests`
- `ShiftServiceTests`
- `CartServiceTests`
- `PaymentServiceTests`
- `PromotionServiceTests`
- `InventorySyncServiceTests`
- `ReturnServiceTests`

Dùng xUnit, Moq và FluentAssertions nếu đã được thêm vào project test.

## Cấu hình chính

### POS

`SmartPOS.WPF/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=smartpos;Username=postgres;Password=1"
  },
  "InventoryManager": {
    "BaseUrl": "http://localhost:5001"
  },
  "Jwt": {
    "Issuer": "SmartPOS",
    "Audience": "SmartPOS.Client",
    "SecretKey": "SmartPOS_Demo_Secret_Key_2026_ChangeMe",
    "ExpiresMinutes": "480"
  },
  "VNPay": {
    "TmnCode": "YOUR_TMN_CODE",
    "HashSecret": "YOUR_HASH_SECRET",
    "PaymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "ReturnUrl": "http://localhost:5000/api/vnpay/return"
  }
}
```

### Inventory Manager

`InventoryManager.Api/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=inventory_manager;Username=postgres;Password=1"
  }
}
```

### Docker

`docker-compose.yml`

```yaml
services:
  postgres:
    image: postgres:16
    ports:
      - "5433:5432"
```

## Gói thư viện

- WPF: CommunityToolkit.Mvvm, ModernWpfUI, Serilog, QRCoder.
- Services: logging, HttpClient, BCrypt.Net-Next, System.IdentityModel.Tokens.Jwt.
- Data: Entity Framework Core, Npgsql.
- CallbackApi: ASP.NET Core.
- InventoryManager.Api: ASP.NET Core, Entity Framework Core, Npgsql.
- Tests: xUnit, Moq, FluentAssertions.

Không thêm gói mới nếu chưa có lý do rõ và chưa cập nhật docs.

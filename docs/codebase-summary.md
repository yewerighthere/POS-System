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

- Auth: `LoginRequestDto`, `LoginResponseDto`, `UserSessionDto`, `CreateUserDto`, `UserDto`, `UpdateUserDto`.
- Shift: `OpenShiftDto`, `CloseShiftDto`, `ShiftDto`, `ShiftSummaryDto`.
- Product: `ProductDto`, `ProductSearchResultDto`, `SyncProductDto`.
- Cart: `CartItemDto`, `CartSummaryDto`.
- Order: `CreateOrderDto`, `OrderDto`, `OrderItemDto`, `OrderItemInputDto`.
- Payment: `CashPaymentDto`, `VNPayRequestDto`, `VNPayCallbackDto`, `PaymentResultDto`.
- Invoice: `InvoiceDto`.
- Customer: `CustomerDto`, `CreateCustomerDto`, `CustomerListDto`, `CustomerDetailDto`, `CustomerOrderDto`, `CustomerOrderItemDto`, `CustomerOrderDetailDto`, `UpdateCustomerDto`.
- Return: `ReturnRequestDto`, `ReturnItemInputDto`, `ReturnDto`.
- Catalog: `CategoryDto`, `CreateCategoryDto`, `CreateProductDto`, `UpdatePriceDto`.
- Promotion: `PromotionDto`, `PromotionValidationResultDto`.
- Inventory: `StockDeductionEventDto`, `RestockEventDto`, `SyncResultDto`.
- Report: `ShiftReportDto`, `SalesReportDto`, `OrderLogDto`, `TopProductDto`, `RecentShiftDto`.

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

- `IUserRepository`: tìm theo username, tìm theo id, thêm, cập nhật, lấy tất cả (GetAllAsync).
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

- `IAuthService`: đăng nhập, đăng xuất, kiểm tra JWT, tạo tài khoản demo khi cần, tạo/cập nhật/khóa/reset mật khẩu nhân viên (User Management).
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

POS migration dùng `SmartPOS.Data/AppDbContextFactory` làm design-time factory theo `database-guide.md`.
`AppDbContext` và `AppDbContextFactory` đều lấy POS connection string từ `appsettings.json`; không hard-code
connection string trong code Data.
`SmartPOS.WPF` vẫn tham chiếu `Microsoft.EntityFrameworkCore.Design` để hỗ trợ EF tooling khi cần dùng WPF làm startup project.

Inventory sync is registered as a typed `HttpClient` service in WPF and reads `InventoryManager:BaseUrl`
from `appsettings.json`. The current demo port is `http://localhost:5145`.

Trong môi trường dev, WPF ghi log Serilog vào `logs/smartpos-yyyyMMdd.log` ở root repository nếu tìm thấy `SmartPOS.sln`.

### Session

`CurrentSessionContext` giữ người dùng hiện tại và ca đang mở.

### Navigation

`NavigationService` điều hướng giữa các màn hình bằng ViewModel. Nếu ViewModel chưa được map sang View,
service hiện ném `NotImplementedException`.

### ViewModel

Thư mục: `SmartPOS.WPF/ViewModels`

- `LoginViewModel`: đăng nhập, lưu `CurrentSessionContext` và điều hướng theo role.
- `ShiftViewModel`: mở ca, đóng ca, có `InitializeAsync` để tìm ca đang mở của user khi quay lại màn hình ca.
- `SalesViewModel`: màn hình bán hàng, giả lập máy quét, giỏ hàng (tích hợp customer lookup/creation popup, promotion code input, loyalty points toggle, checkout navigation).
- `PaymentViewModel`: tiền mặt và VNPay QR/polling.
- `InvoiceViewModel`: xem hóa đơn và in giả lập.
- `CustomerViewModel`: quản lý khách hàng (search/filter/sort/detail/edit/toggle status/view orders).
- `ReturnViewModel`: hiện còn TODO.
- `CatalogViewModel`: quản lý danh mục, sản phẩm, giá (CRUD + filter/search + deactivate/reactivate + image + inline sync).
- `PromotionViewModel`: quản lý khuyến mãi (CRUD, tìm kiếm, lọc, kích hoạt/khóa).
- `ReportViewModel`: báo cáo ca với shift report, recent shifts, top products, order log.
- `AuditLogViewModel`: hiện còn TODO.
- `SyncViewModel`: đồng bộ catalog và tồn kho với Inventory Manager (SyncCatalog, SyncStock, SyncAll commands).
- `UserManagementViewModel`: quản lý nhân viên (tìm kiếm, lọc vai trò/trạng thái, tạo mới, sửa thông tin, đặt lại mật khẩu, khóa/mở khóa tài khoản).

### View

Thư mục: `SmartPOS.WPF/Views`

Mỗi ViewModel nên có View tương ứng.

- `LoginView`: giao diện split-screen theo thiết kế, có password toggle và binding về `LoginViewModel`.
- `ShiftView`, `PaymentView`, `InvoiceView`: đã có UI mức cơ bản.
- `SalesView`: đã có UI hoàn chỉnh (tích hợp customer lookup/creation popup, promotion code, loyalty points, checkout).
- `CatalogView`: đã có UI hoàn chỉnh (CRUD + filter/search + deactivate/reactivate + image + inline sync).
- `ReportView`, `SyncView`: đã có UI hoàn chỉnh.
- `CustomerView`: giao diện quản lý khách hàng đầy đủ thông tin, lịch sử mua hàng, sửa thông tin và khóa/mở khóa.
- `PromotionView`: giao diện quản lý khuyến mãi với danh sách và popup tạo mới/sửa.
- `UserManagementView`: giao diện quản lý nhân viên trực quan với bảng dữ liệu và 3 popup overlay (thêm, sửa, reset mật khẩu), tích hợp trong Dashboard.
- `ReturnView`, `AuditLogView`: hiện vẫn là placeholder TODO.

### Control Dự Kiến

Thư mục: `SmartPOS.WPF/Controls`

Thư mục control riêng hiện chưa có trong source. Các control dưới đây là hướng tách UI sau này nếu cần:

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
- `NullToVisibilityConverter`
- `NullOrEmptyToVisibilityConverter`
- `DecimalToVisibilityConverter`
- `InverseBoolToVisibilityConverter`
- `InverseBoolConverter`
- `LocalImagePathConverter`
- `PercentToGridLengthConverter`
- `ProductImageConverter`
- `ImagePathToVisibilityConverter`

### Theme System

Thư mục: `SmartPOS.WPF/Themes`

Hệ thống theme trung tâm chuyển đổi từ `style.css` (glassmorphism) sang WPF ResourceDictionary. Đăng ký trong `App.xaml` qua `Themes/Generic.xaml`. 13 file XAML: Colors, Fonts, Spacing, Shadows, ButtonStyles, TextBoxStyles, BorderStyles, BadgeStyles, ModalStyles, ScrollBarStyles, SidebarStyles, TableStyles, Generic. Màu primary: `#0062FF`. Font: Inter/Segoe UI. 9 view đã refactor: Login, Shift, Sales, Payment, Catalog, Customer, Report, Sync, Invoice.

## SmartPOS.CallbackApi

File chính: `Program.cs`

Endpoint:

```text
GET/POST /api/vnpay/callback
```

Trạng thái hiện tại:

- Minimal API đã map `GET/POST /api/vnpay/callback`.
- Endpoint đọc query/form/json callback, kiểm tra chữ ký HMAC-SHA512 và gọi `IPaymentService.HandleVNPayCallbackAsync`.
- Endpoint trả trang HTML kết quả để điện thoại hiển thị thanh toán thành công/thất bại sau VNPay.

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

Controller hiện tại:

- `ProductsController`: GET tất cả sản phẩm kèm tồn kho và category, POST tạo sản phẩm mới (validate SKU/barcode trùng, kiểm tra category, tạo kèm StockItem), GET /categories trả danh sách danh mục active.
- `SyncController`: trả danh mục và tồn kho cho POS đồng bộ.
- `StockController`: xử lý trừ kho và nhập lại hàng ở mức cơ bản, đồng thời ghi `StockTransaction`.

Endpoint:

```text
GET  /api/sync/catalog
GET  /api/sync/stock
POST /api/stock/deduct
POST /api/stock/restock
GET  /api/products
POST /api/products
GET  /api/products/categories
```

## SmartPOS.Tests

Thư mục: `tests/SmartPOS.Tests`

Tập trung kiểm thử Service:

- `AuthServiceTests` — 7 test login/logout/JWT
- `ShiftServiceTests` — 5 test mở/đóng ca
- `CartServiceTests` — 8 test thật (add/inactive/stock/update/remove/tax/recalculate)
- `PaymentServiceTests` — 11 test cash/VNPay/callback/cancel
- `InvoiceServiceTests` — 4 test tạo/xem hóa đơn
- `DeviceServiceTests` — 3 test log/print giả lập
- `PromotionServiceTests` — placeholder, chưa có test thật
- `InventorySyncServiceTests` — 6 test sync catalog/stock/partial
- `ReturnServiceTests` — placeholder, chưa có test thật

Dùng xUnit, Moq và FluentAssertions.

Hiện test suite có **51 test thật** + 1 placeholder (ReturnServiceTests). Chạy:
`dotnet test tests/SmartPOS.Tests/SmartPOS.Tests.csproj --no-build`.

## Cấu hình chính

### POS

`SmartPOS.WPF/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=smartpos;Username=postgres;Password=1"
  },
  "InventoryManager": {
    "BaseUrl": "http://localhost:5145"
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

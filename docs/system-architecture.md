# Kiến trúc hệ thống

File này là nguồn tham chiếu chính về cấu trúc solution, trách nhiệm từng tầng, luồng dữ
liệu và sơ đồ cơ sở dữ liệu. Trước khi tạo file, class hoặc project mới, hãy đọc file này.

## Cấu trúc solution

```text
SmartPOS.sln
src/
  SmartPOS.WPF/
  SmartPOS.Services/
  SmartPOS.Data/
  SmartPOS.Shared/
  SmartPOS.CallbackApi/
  InventoryManager.Api/
tests/
  SmartPOS.Tests/
docs/
```

## Quy tắc phụ thuộc

- `SmartPOS.Shared` không phụ thuộc project nào khác.
- `SmartPOS.Data` phụ thuộc `SmartPOS.Shared`.
- `SmartPOS.Services` phụ thuộc `SmartPOS.Shared` và `SmartPOS.Data` để dùng repository
  interface và entity khi cần. Service không được dùng trực tiếp `AppDbContext` hoặc API
  EF Core.
- `SmartPOS.WPF` phụ thuộc `SmartPOS.Shared`, `SmartPOS.Services` và `SmartPOS.Data` chỉ
  để cấu hình DI trong `App.xaml.cs`.
- `SmartPOS.CallbackApi` phụ thuộc các project cần thiết để nhận callback và cập nhật đơn.
- `InventoryManager.Api` là hệ thống riêng, có cơ sở dữ liệu riêng.

Quy tắc quan trọng nhất:

```text
ViewModel -> Service -> Repository -> DbContext
```

Không để ViewModel gọi Repository hoặc DbContext. Không để Repository chứa quy tắc nghiệp vụ.

## Trách nhiệm từng project

### SmartPOS.Shared

Chứa DTO, enum, hằng số và exception dùng chung. Project này không chứa logic nghiệp vụ.

Thư mục đề xuất:

```text
DTOs/
Enums/
Exceptions/
Constants/
```

### SmartPOS.Data

Chứa entity, `AppDbContext`, repository interface, repository implementation và migration
của POS.

Thư mục đề xuất:

```text
Entities/
Repositories/Interfaces/
Repositories/Implementations/
Migrations/
AppDbContext.cs
```

Repository trả về entity, không trả về DTO. Repository được phép dùng EF Core và
`AppDbContext`.

### SmartPOS.Services

Chứa toàn bộ quy tắc nghiệp vụ. Service nhận DTO, trả DTO, gọi repository và gọi API ngoài
nếu cần. Service ném `BusinessException` cho lỗi nghiệp vụ.

Thư mục đề xuất:

```text
Interfaces/
Implementations/
```

### SmartPOS.WPF

Chứa giao diện, ViewModel, điều hướng, converter và control. ViewModel chỉ điều phối trạng
thái giao diện và gọi Service.

Thư mục đề xuất:

```text
ViewModels/
Views/
Controls/
Converters/
Navigation/
Session/
```

### SmartPOS.CallbackApi

Cổng nhận callback VNPay. Project này có một nhiệm vụ chính: nhận callback, kiểm tra chữ ký,
gọi `IPaymentService` và trả kết quả phù hợp cho VNPay.

Endpoint chính:

```text
POST /api/vnpay/callback
```

### InventoryManager.Api

Hệ thống quản lý tồn kho riêng. POS chỉ nói chuyện với hệ thống này qua HTTP API.

Endpoint dự kiến:

```text
GET  /api/sync/catalog
GET  /api/sync/stock
POST /api/stock/deduct
POST /api/stock/restock
```

## Luồng bán hàng tiền mặt

```text
Nhân viên chọn sản phẩm
  -> SalesViewModel
  -> IProductService
  -> IProductRepository
  -> Trả ProductDto
  -> Giỏ hàng cập nhật

Nhân viên thanh toán tiền mặt
  -> PaymentViewModel
  -> IPaymentService.RecordCashPaymentAsync
  -> Kiểm tra ca đang mở
  -> Kiểm tra đơn không bị khóa
  -> Kiểm tra tiền khách đưa
  -> Tạo payment
  -> Xác nhận đơn hàng
  -> Gửi API trừ kho sang Inventory Manager
  -> Tạo hóa đơn
  -> Ghi nhật ký
```

Thông báo tiền khách đưa không đủ:

```text
Số tiền khách đưa không đủ để thanh toán
```

## Luồng thanh toán VNPay

```text
Nhân viên chọn VNPay
  -> IPaymentService.CreateVNPayRequestAsync
  -> Khóa đơn hàng
  -> Tạo đường dẫn thanh toán và QR
  -> WPF hiện QR

VNPay gọi callback
  -> SmartPOS.CallbackApi
  -> Kiểm tra chữ ký HMAC-SHA512
  -> IPaymentService.HandleVNPayCallbackAsync
  -> Nếu thành công: cập nhật payment, mở khóa đơn, tạo hóa đơn, trừ kho
  -> Nếu thất bại: cập nhật thất bại, mở khóa đơn

WPF kiểm tra trạng thái mỗi 2 giây
  -> IPaymentService.GetOrderPaymentStatusAsync
  -> Đóng hộp thoại khi không còn Pending
```

Thông báo hết thời gian thanh toán:

```text
Thanh toán quá thời gian chờ, đơn hàng đã được mở khóa
```

## Luồng đồng bộ tồn kho

```text
Quản lý bấm đồng bộ
  -> IInventorySyncService.SyncCatalogAsync
  -> Gọi InventoryManager.Api
  -> Cập nhật hoặc tạo sản phẩm POS theo external_inventory_id
  -> Ghi inventory_sync_logs

Quản lý bấm đồng bộ tồn
  -> IInventorySyncService.SyncStockAsync
  -> Gọi InventoryManager.Api
  -> Cập nhật LocalStockQuantity
  -> Ghi inventory_sync_logs
```

## Hai cơ sở dữ liệu riêng

POS và Inventory Manager không dùng chung cơ sở dữ liệu.

```text
smartpos
  users
  user_sessions
  shifts
  categories
  products
  promotions
  customers
  orders
  order_items
  payments
  invoices
  returns
  return_items
  inventory_sync_logs
  devices
  device_logs
  audit_logs

inventory_manager
  inventory_products
  inventory_categories
  stock_items
  stock_transactions
```

## Sơ đồ bảng POS

Tất cả khóa chính dùng `Guid`. Tên cột trong PostgreSQL dùng `snake_case`.

### users

```sql
id uuid primary key
username varchar(100) unique not null
password_hash varchar(255) not null
full_name varchar(200)
role varchar(20) not null
status varchar(20) not null
created_at timestamptz not null
```

### user_sessions

```sql
id uuid primary key
user_id uuid not null
login_at timestamptz not null
logout_at timestamptz
ip_address varchar(50)
```

### shifts

```sql
id uuid primary key
user_id uuid not null
status varchar(20) not null
opening_cash numeric(18,2) not null
closing_cash numeric(18,2)
expected_cash numeric(18,2)
cash_difference numeric(18,2)
opened_at timestamptz not null
closed_at timestamptz
```

### categories

```sql
id uuid primary key
name varchar(200) unique not null
description text
is_active boolean not null
```

### products

```sql
id uuid primary key
category_id uuid
name varchar(300) not null
sku varchar(100) unique not null
barcode varchar(100) unique
qr_code varchar(200) unique
description text
unit_price numeric(18,2) not null
tax_rate numeric(5,4) not null
is_active boolean not null
external_inventory_id varchar(100) unique
local_stock_quantity int not null
last_synced_at timestamptz
created_at timestamptz not null
updated_at timestamptz
```

### promotions

```sql
id uuid primary key
code varchar(50) unique not null
name varchar(200) not null
description text
type varchar(30) not null
discount_value numeric(18,2) not null
min_order_amount numeric(18,2)
product_id uuid
start_date date not null
end_date date not null
requires_approval_threshold numeric(18,2)
is_active boolean not null
```

Cột `code` bắt buộc có vì nhân viên nhập mã khuyến mãi trong luồng demo.

### customers

```sql
id uuid primary key
full_name varchar(200)
phone varchar(20) unique
email varchar(200)
member_code varchar(50) unique
loyalty_points int not null
created_at timestamptz not null
updated_at timestamptz
```

### orders

```sql
id uuid primary key
shift_id uuid not null
user_id uuid not null
customer_id uuid
status varchar(20) not null
subtotal numeric(18,2) not null
discount_amount numeric(18,2) not null
tax_amount numeric(18,2) not null
total_amount numeric(18,2) not null
payment_method varchar(20)
payment_status varchar(20) not null
is_locked boolean not null
created_at timestamptz not null
updated_at timestamptz
```

### order_items

```sql
id uuid primary key
order_id uuid not null
product_id uuid not null
product_name varchar(300) not null
sku varchar(100) not null
unit_price numeric(18,2) not null
quantity int not null
discount_amount numeric(18,2) not null
subtotal numeric(18,2) not null
```

### payments

```sql
id uuid primary key
order_id uuid not null
payment_method varchar(20) not null
amount_received numeric(18,2)
change_amount numeric(18,2)
transaction_id varchar(200)
payment_status varchar(20) not null
vnpay_response jsonb
created_at timestamptz not null
```

### invoices

```sql
id uuid primary key
order_id uuid unique not null
invoice_number varchar(50) unique not null
total_amount numeric(18,2) not null
issued_at timestamptz not null
```

### returns và return_items

```sql
returns:
id uuid primary key
order_id uuid not null
requested_by uuid not null
approved_by uuid
status varchar(20) not null
reason text not null
refund_amount numeric(18,2)
created_at timestamptz not null
resolved_at timestamptz

return_items:
id uuid primary key
return_id uuid not null
order_item_id uuid not null
quantity int not null
```

### inventory_sync_logs

```sql
id uuid primary key
sync_type varchar(50) not null
status varchar(20) not null
message text
synced_at timestamptz not null
```

### devices và device_logs

Bảng này dùng cho cấu hình giả lập thiết bị trong demo.

```sql
devices:
id uuid primary key
name varchar(200) not null
device_type varchar(50) not null
connection_type varchar(50) not null
serial_number varchar(100) unique
port_name varchar(50)
is_active boolean not null

device_logs:
id uuid primary key
device_id uuid
event_type varchar(50) not null
message text
created_at timestamptz not null
```

### audit_logs

```sql
id uuid primary key
user_id uuid not null
action varchar(100) not null
entity varchar(100)
entity_id uuid
old_value jsonb
new_value jsonb
created_at timestamptz not null
```

## Quyết định kiến trúc

- POS và Inventory Manager dùng hai cơ sở dữ liệu riêng để tránh phụ thuộc trực tiếp.
- Hai hệ thống chỉ trao đổi qua HTTP API.
- WPF kiểm tra trạng thái VNPay bằng cách hỏi lại mỗi 2 giây, không dùng SignalR.
- JWT lưu trong bộ nhớ của phiên ứng dụng, không cần refresh token cho demo.
- Repository interface riêng theo từng nghiệp vụ, không dùng generic repository.
- Callback VNPay nằm trong project riêng vì WPF không phù hợp để nhận callback từ internet.
- Scanner và printer là giao diện giả lập, không tích hợp phần cứng thật trong demo.

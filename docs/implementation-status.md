# Trạng thái triển khai

File này giúp nhóm và tác nhân AI biết tính năng nào đã làm, đang làm hoặc chưa làm. Mỗi khi hoàn thành một module, hãy cập nhật file này trước khi giao việc tiếp.

Nếu cần danh sách task chi tiết để triển khai từ đầu đến cuối, đọc thêm `docs/development-task-list.md`.

## Trạng thái dùng chung

- `Chưa làm`: chưa có code hoặc chỉ mới có skeleton/stub.
- `Đang làm`: đã có code một phần, cần đọc source trước khi sửa tiếp.
- `Cần kiểm thử`: code có thể đã xong nhưng chưa đủ test hoặc chưa chạy demo.
- `Đã xong`: đã chạy được theo luồng demo và có test tối thiểu.
- `Tạm hoãn`: không thuộc ưu tiên hiện tại.

## Tổng quan hiện tại

Solution skeleton đã được scaffold:

- Đã có `SmartPOS.sln`.
- Đã có các project trong `src/`.
- Đã có project test trong `tests/`.
- Đã có DTO, enum, exception, entity, repository interface, service interface, ViewModel/View shell và API shell.
- Business logic đã có cho F-01 Auth/JWT, F-02 Shift, F-03 Product/Search/Cart mức cơ bản, F-05 Cash Payment, F-11 Catalog mức cơ bản và một phần F-13 Inventory API/Sync.
- Repository và service implementation vẫn còn nhiều stub ở Invoice, Return, Report, Audit, Device, Customer, Promotion và VNPay.
- Đã có migration POS `InitialCreate` và `AddUserContactFields`.
- Đã apply migration POS vào PostgreSQL Docker container qua port `5433`.
- AuthService có hàm tạo user demo `quantri`, `quanly`, `nhanvien`; `DataSeeder` hiện seed danh mục và 10 sản phẩm mẫu.
- Đã có UI flow đăng nhập, mở ca, bán hàng/cash payment ở mức cơ bản và điều hướng theo role; nhiều flow nghiệp vụ còn lại chưa hoàn chỉnh.

Vì vậy, trạng thái hiện tại nên hiểu là: **nền project đã có, tính năng nghiệp vụ chưa hoàn thành**.

## Trạng thái tính năng

| Mã | Tính năng | Trạng thái | Ghi chú |
|---|---|---|---|
| F-01 | Đăng nhập, đăng xuất, tạo tài khoản | Đã xong | Đã implement repository, AuthService với BCrypt/JWT, seed 3 user demo, LoginViewModel, LoginView theo thiết kế, điều hướng theo role và kiểm thử thủ công 3 tài khoản demo |
| F-02 | Mở ca, đóng ca | Đã xong | Đã implement IShiftRepository (GetOpenShiftAsync, GetByIdAsync, AddAsync, UpdateAsync, GetCashRevenueAsync, GetTotalSalesAsync), ShiftService (OpenShiftAsync, CloseShiftAsync, GetOpenShiftAsync, GetShiftSummaryAsync), ShiftViewModel với CommunityToolkit.Mvvm, ShiftView.xaml UI mở/đóng ca, lưu ca vào CurrentSessionContext; 5 unit test pass. ShiftView.xaml.cs gọi InitializeAsync khi Loaded để tự nạp ca đang mở sau khi app khởi động lại; sau khi mở ca thành công tự điều hướng sang SalesView. |
| F-03 | Bán hàng: chọn sản phẩm, tìm kiếm, giỏ hàng, tính tiền | Đang làm | Đã implement ProductRepository/ProductService tìm barcode/search, CartService thêm/sửa/xóa/tính lại và SalesViewModel/SalesView mức cơ bản. Còn cần hoàn thiện kiểm tra inactive/giá thay đổi, thuế, UX và test search/sales. |
| F-04 | Khuyến mãi và giảm giá | Chưa làm | Entity `Promotion` đã có `Code`; cần implement validation, áp dụng mã và approval |
| F-05 | Thanh toán tiền mặt | Đã xong | Đã implement OrderRepository, PaymentService.CreateOrderFromCartAsync và RecordCashPaymentAsync, PaymentViewModel với quick-cash buttons, PaymentView.xaml, kiểm tra thiếu tiền/order khóa và tạo payment/order status. Inventory/audit side effect vẫn bọc lỗi vì F-13/F-15 chưa hoàn chỉnh. |
| F-06 | Thanh toán VNPay | Chưa làm | Cần tạo URL, QR, khóa order và poll trạng thái |
| F-07 | Callback thanh toán | Chưa làm | Đã có CallbackApi shell; cần callback endpoint, chữ ký và xử lý trạng thái |
| F-08 | Hóa đơn và giả lập in | Chưa làm | Cần invoice number, preview, fake print và device log |
| F-09 | Khách hàng và điểm tích lũy | Chưa làm | Cần implement customer lookup, create, cộng/trừ điểm |
| F-10 | Trả hàng và hoàn tiền | Chưa làm | Bắt buộc cho demo cuối; cần approve/reject và restock |
| F-11 | Danh mục sản phẩm | Đang làm | Đã implement CategoryRepository/ProductRepository và CatalogService tạo danh mục, tạo sản phẩm, cập nhật giá, lấy danh sách ở mức cơ bản; DataSeeder có 3 danh mục và 10 sản phẩm. Còn thiếu validate SKU/barcode/QR duy nhất đầy đủ, deactivate, audit và UI quản lý hoàn chỉnh. |
| F-12 | Giá, thuế và khuyến mãi | Chưa làm | Cần implement quản lý giá, thuế, khuyến mãi và audit sửa giá |
| F-13 | Đồng bộ Inventory Manager | Đang làm | Đã tách `InventoryDbContext`, entity riêng và migration `InitialInventoryCreate`; Inventory API đã có `GET /api/sync/catalog`, `GET /api/sync/stock`, `POST /api/stock/deduct`, `POST /api/stock/restock`. POS `InventorySyncService` đã gọi API và ghi log dự kiến, nhưng `InventorySyncLogRepository` còn stub nên sync sẽ lỗi khi ghi DB log. |
| F-14 | Thiết bị giả lập và nhật ký thiết bị | Chưa làm | Cần fake scanner/printer, device config và device logs |
| F-15 | Báo cáo, audit log và cấu hình cửa hàng | Chưa làm | Ưu tiên báo cáo ca và audit log trước |

## Trạng thái project

| Project | Trạng thái | Ghi chú |
|---|---|---|
| SmartPOS.Shared | Cần kiểm thử | Đã có enum, DTO, exception, constant; cần rà lại shape DTO khi implement thật |
| SmartPOS.Data | Đang làm | Đã có entity, DbContext, design-time factory, migrations POS, Auth/Shift/Product/Category/Order repository và DataSeeder sản phẩm mẫu. Repository Invoice, Return, InventorySyncLog, Device, DeviceLog, AuditLog còn stub. |
| SmartPOS.Services | Đang làm | Auth, Shift, Product, Cart, Catalog, Cash Payment và một phần InventorySync đã có logic. VNPay, Invoice, Promotion, Customer, Return, Report, Audit, Device còn stub hoặc mới mức skeleton. |
| SmartPOS.WPF | Đang làm | Login, Shift, Sales, Payment có UI/ViewModel mức cơ bản. Invoice, Customer, Return, Promotion, Report, AuditLog, Sync, UserManagement vẫn placeholder/TODO. |
| SmartPOS.CallbackApi | Đang làm | Đã có Minimal API shell; callback chỉ trả `OK` |
| InventoryManager.Api | Đang làm | Đã có `InventoryDbContext`, entity inventory riêng, migration `InitialInventoryCreate`, SyncController và StockController có logic cơ bản. ProductsController vẫn trả `Ok()` rỗng. |
| SmartPOS.Tests | Đang làm | Test suite hiện pass 20/20; có test cho Auth, Shift, Cart, Payment, Promotion, InventorySync và Return ở mức hiện tại. Cần bổ sung test cho search/catalog/invoice/audit/report và các luồng UI/demo. |

## Việc nên làm tiếp theo

Chi tiết task nằm trong `docs/development-task-list.md`. Thứ tự ưu tiên ngắn gọn:

1. Bổ sung/kiểm tra seed user demo, external_inventory_id cho sản phẩm và mã khuyến mãi `GIAM10`.
2. Hoàn thiện `InventorySyncLogRepository` để POS sync không lỗi khi ghi log.
3. Hoàn thiện Product/Search/Cart/Sales: inactive, thuế, giá thay đổi, test search.
4. Implement Invoice.
5. Implement VNPay, Return, Report và Audit theo thứ tự demo.

## Dữ liệu demo cần có

### Tài khoản

| Vai trò | Tên đăng nhập | Mật khẩu |
|---|---|---|
| Quản trị | quantri | 123456 |
| Quản lý | quanly | 123456 |
| Nhân viên | nhanvien | 123456 |

### Sản phẩm

Cần tối thiểu 10 sản phẩm có:

- Tên sản phẩm.
- SKU.
- Mã vạch hoặc QR.
- Giá bán.
- Thuế suất.
- Tồn kho.
- `external_inventory_id`.

### Khuyến mãi

Cần tối thiểu một khuyến mãi:

```text
Mã: GIAM10
Tên: Giảm 10 phần trăm
Điều kiện: đơn hàng từ 50000
Trạng thái: đang hoạt động
```

## Điều kiện để đánh dấu đã xong

Một tính năng chỉ nên đánh dấu `Đã xong` khi:

- Chạy được trên máy demo.
- Có đủ thông báo tiếng Việt có dấu.
- Không phá luồng demo chính.
- Có test Service nếu tính năng có quy tắc nghiệp vụ.
- Đã cập nhật docs nếu có thay đổi về schema, API, config hoặc quy tắc.
- Đã cập nhật checkbox liên quan trong `docs/development-task-list.md`.

## Mẫu cập nhật trạng thái

```text
F-05 Thanh toán tiền mặt: Cần kiểm thử
Đã tạo PaymentService và OrderRepository.AddPaymentAsync.
Còn thiếu test trừ kho khi Inventory Manager lỗi.
```

## Rủi ro cần theo dõi

- PostgreSQL Docker đang map ra host port `5433` để tránh đụng PostgreSQL local ở port `5432`.
- VNPay cần ngrok và cấu hình callback đúng.
- Đồng bộ tồn kho dễ lỗi nếu API Inventory Manager chưa chạy.
- Trả hàng cần tính đúng số lượng đã trả trước đó.
- Khuyến mãi cần `code`, nếu thiếu sẽ không khớp luồng demo.
- Giả lập in không nên làm thất bại thanh toán nếu có lỗi.
- Tạo tài khoản mới cần giới hạn cho Manager hoặc Admin.

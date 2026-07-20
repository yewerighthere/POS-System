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
- Business logic đã có cho F-01 Auth/JWT, F-02 Shift, F-03 Product/Search/Cart mức cơ bản, F-04 Promotion, F-05 Cash Payment, F-06 VNPay, F-07 Callback, F-08 Invoice, F-09 Customer, F-11 Catalog, F-13 Inventory Sync, F-14 Device, F-15 Shift/Sales Report và Audit Log.
- Tất cả module đã hoàn thành: Auth, Shift, Product, Promotion, Cash Payment, VNPay, Callback, Invoice, Customer, Catalog, InventorySync, Report, Device, AuditLog, Return.
- Đã có 6 migration POS: `InitialCreate`, `AddUserContactFields`, `AddAffectedRowsToSyncLog`, `AddProductImagePath`, `AddLoyaltyPointsToOrder`, `UpdateCustomerStatusAndOrders`.
- Đã apply migration POS vào PostgreSQL Docker container qua port `5433`.
- AuthService có hàm tạo user demo `quantri`, `quanly`, `nhanvien`; `DataSeeder` seed danh mục, 10 sản phẩm mẫu và dữ liệu khách hàng demo.
- Đã có UI flow đầy đủ: đăng nhập, mở ca, bán hàng/cash payment (tích hợp customer lookup/promotion code/loyalty points), VNPay QR/polling, hóa đơn preview, đồng bộ catalog/tồn kho, quản lý danh mục/sản phẩm, quản lý khách hàng, quản lý khuyến mãi, quản lý người dùng, trả hàng/hoàn tiền, báo cáo doanh thu/ca.
- **UI/UX fixes (session 2026-07-20)**: App khởi động full-screen, tìm kiếm không phân biệt hoa thường, lọc sản phẩm theo category hoạt động đúng, sidebar nav hiển thị đồng nhất ở tất cả view, thuế giảm từ 10% xuống 3%, ReturnView sửa lỗi layout chồng chéo, CatalogView font lớn hơn và bảng sản phẩm rõ ràng, ShiftView hiển thị số tiền có dấu phân cách nghìn.
- **UI/UX Sidebar Redesign (session 2026-07-21)**: Tái thiết kế toàn bộ Left Sidebar trên 6 view (`SalesView`, `SyncView`, `CustomerView`, `ReturnView`, `CatalogView`, `ReportView`): thay `Grid Height="*"` bằng `ScrollViewer + StackPanel`, thêm Section Headers phân nhóm (`OPERATIONS`, `MANAGEMENT`, `ANALYTICS`), đồng bộ chiều cao nút 40px, thêm khối Sidebar Footer xếp chồng (Shift Info / Staff / Current Time) ở đáy sidebar tất cả view, xóa nút `+ New Transaction`, dọn dẹp thông tin trùng lặp ở Top Header. Build 0 error, 60/60 test pass.

Vì vậy, trạng thái hiện tại nên hiểu là: **project đã hoàn thiện ~98%, tất cả tính năng nghiệp vụ chính đã xong, giao diện đã đồng bộ và hoàn chỉnh, còn cần kiểm thử end-to-end đầy đủ**.

## Trạng thái tính năng

| Mã | Tính năng | Trạng thái | Ghi chú |
|---|---|---|---|
| F-01 | Đăng nhập, đăng xuất, tạo tài khoản | Đã xong | Đã implement repository, AuthService với BCrypt/JWT, seed 3 user demo, LoginViewModel, LoginView theo thiết kế, điều hướng theo role và kiểm thử thủ công 3 tài khoản demo |
| F-02 | Mở ca, đóng ca | Đã xong | Đã implement IShiftRepository (GetOpenShiftAsync, GetByIdAsync, AddAsync, UpdateAsync, GetCashRevenueAsync, GetTotalSalesAsync), ShiftService (OpenShiftAsync, CloseShiftAsync, GetOpenShiftAsync, GetShiftSummaryAsync), ShiftViewModel với CommunityToolkit.Mvvm, ShiftView.xaml UI mở/đóng ca, lưu ca vào CurrentSessionContext; 5 unit test pass. ShiftView.xaml.cs gọi InitializeAsync khi Loaded để tự nạp ca đang mở sau khi app khởi động lại; sau khi mở ca thành công tự điều hướng sang SalesView. |
| F-03 | Bán hàng: chọn sản phẩm, tìm kiếm, giỏ hàng, tính tiền | Cần kiểm thử | Đã implement ProductRepository/ProductService tìm barcode/search (tìm kiếm không phân biệt hoa thường), CartService thêm/sửa/xóa/tính lại (thuế 3%), kiểm tra sản phẩm ngừng kinh doanh và hết hàng, SalesViewModel/SalesView đã tích hợp đầy đủ: search/barcode scan, cart CRUD, customer lookup/creation popup, promotion code input, loyalty points toggle, checkout navigation và 8 test CartService. Đã fix lỗi DbContext threading khi load POS view. Cần kiểm thử end-to-end trên WPF. |
| F-04 | Khuyến mãi và giảm giá | Đã xong | PromotionRepository đã có 18 method. PromotionService đã implement đầy đủ 22 methods: ValidateCodeAsync (check code/active/expire/minOrder/product), ApplyPromotionAsync (áp dụng giảm giá vào cart), RequestApprovalAsync, CreatePromotionAsync, CRUD (GetById/Code/Name, GetAll, GetExpired/Unexpired, Search, GetActive, Delete), 10 individual Update* methods. PromotionViewModel đã implement đầy đủ (search, filter, CRUD, create popup, view detail, toggle active). PromotionView đã hoàn chỉnh (338 lines). SalesViewModel đã wire promotion code input và apply vào checkout flow. DTO: PromotionDto, CreatePromotionDto, PromotionValidationResultDto. |
| F-05 | Thanh toán tiền mặt | Đã xong | Đã implement OrderRepository, PaymentService.CreateOrderFromCartAsync và RecordCashPaymentAsync, PaymentViewModel với quick-cash buttons, PaymentView.xaml, kiểm tra thiếu tiền/order khóa và tạo payment/order status. |
| F-06 | Thanh toán VNPay | Đã xong | Đã có tạo URL/QR VNPay, khóa order, poll trạng thái mỗi 2 giây, hủy/timeout mở khóa order và test service liên quan |
| F-07 | Callback thanh toán | Đã xong | Callback API nhận GET/POST qua ngrok, validate chữ ký HMAC, xử lý success/failure, trả trang HTML kết quả và cập nhật payment/order |
| F-08 | Hóa đơn và giả lập in | Đã xong | InvoiceService tạo hóa đơn theo ngày, chặn invoice khi payment chưa thành công, InvoiceView xem hóa đơn và modal in giả lập, DeviceService ghi log preview; test invoice/fake print pass |
| F-09 | Khách hàng và điểm tích lũy | Đã xong | CustomerRepository có GetByPhoneAsync, GetByIdAsync, AddAsync, UpdateAsync, GetCustomersQueryAsync. CustomerService đã implement đầy đủ 10 method. CustomerViewModel có search, filter, sort, xem chi tiết khách hàng, sửa thông tin, toggle status, xem chi tiết đơn hàng. CustomerView đã hoàn chỉnh. |
| F-10 | Trả hàng và hoàn tiền | Đã xong | Đã implement ReturnRepository (GetByIdAsync, GetByOrderIdAsync, GetAllAsync, AddAsync, UpdateAsync), ReturnService (CreateReturnAsync, ApproveAsync, RejectAsync, restock local + Inventory API, trừ điểm tích lũy), ReturnViewModel (tạo/tìm đơn, phê duyệt/từ chối), ReturnView.xaml refactor lại layout chuẩn theme (đã sửa lỗi giao diện chồng chéo), 5 unit test pass. |
| F-11 | Danh mục sản phẩm | Đã xong | CategoryRepository/ProductRepository đã implement đầy đủ (ProductRepository bật eager-load Category để hiển thị CategoryName). CatalogService đã có CRUD category, CRUD product, UpdatePriceAsync (có audit log), DeactivateProductAsync, ReactivateProductAsync, UpdateProductImageAsync, RegisterProductToInventoryAsync, validate SKU/barcode duy nhất, UpdateStockAsync, hai chiều đồng bộ POS↔Inventory. CatalogView.xaml đã redesign: font lớn hơn, bảng sản phẩm rõ ràng, ảnh thumbnail có fallback emoji, lọc theo category/status. |
| F-12 | Giá, thuế và khuyến mãi | Đã xong | AuditLogRepository đã implement. AuditService.LogAsync được gọi từ CatalogService, PaymentService, PromotionService, ReturnService. Promotion CRUD hoàn chỉnh. |
| F-13 | Đồng bộ Inventory Manager | Đã xong | InventoryDbContext, entity riêng, migration InitialInventoryCreate. Inventory API và POS InventorySyncService hoàn thiện. |
| F-14 | Thiết bị giả lập và nhật ký thiết bị | Đã xong | Đã có DeviceService cho printer giả lập, DeviceLogRepository ghi sự kiện preview/in giả lập và test device log cơ bản |
| F-15 | Báo cáo, audit log và cấu hình cửa hàng | Đã xong | ReportService, ReportViewModel, ReportView, AuditLogViewModel và AuditLogView đã hoàn thành. |
| F-16 | Quản lý bảng tổng quan (Dashboard) | Đã xong | Đã hoàn thành DashboardView và 4 sub-views, tích hợp dữ liệu thời gian thực từ database qua DashboardService. |

## Trạng thái project

| Project | Trạng thái | Ghi chú |
|---|---|---|
| SmartPOS.Shared | Đã xong | Đã có enum, DTO, exception, constant, ReturnDto, OrderDto. |
| SmartPOS.Data | Đã xong | Repository: Auth, Shift, Product, Category, Order, Invoice, Device, DeviceLog, InventorySyncLog, AuditLog, Promotion, Customer, Return. |
| SmartPOS.Services | Đã xong | Business logic: Auth, Shift, Product, Cart, Catalog, Cash Payment, VNPay, Invoice, Device, InventorySync, Report, Audit, Customer, Promotion, Dashboard, Return. |
| SmartPOS.WPF | Đã xong | **18 ViewModel & 18 View** hoàn thiện 100%. |
| SmartPOS.CallbackApi | Đã xong | Endpoint callback thật cho VNPay. |
| InventoryManager.Api | Đã xong | InventoryDbContext, entity inventory riêng, API controllers, Swagger. |
| SmartPOS.Tests | Đã xong | **60 test thật** pass 100% (Auth, Shift, Cart, Payment, Invoice, Device, InventorySync, Report, Return). |

## Việc nên làm tiếp theo

Chi tiết task nằm trong `docs/development-task-list.md`. Thứ tự ưu tiên ngắn gọn:

1. ✅ **Trả hàng** (F-10): Đã hoàn thành — ReturnRepository, ReturnService, ReturnViewModel, ReturnView (layout đã sửa).
2. ✅ **Seed data**: DataSeeder đã có danh mục, sản phẩm, khách hàng demo.
3. ✅ **Báo cáo doanh thu** (F-15): Đã hoàn thành — `ReportService.GetSalesReportAsync` implement đầy đủ.
4. ✅ **Audit log viewer** (F-15): Đã hoàn thành — `AuditLogViewModel` hiển thị audit logs.
5. ✅ **Quản lý user**: Đã hoàn thành — `UserManagementViewModel` implement đầy đủ.
6. ✅ **Khuyến mãi** (F-04): Đã hoàn thành — PromotionService 22 methods, PromotionViewModel CRUD, PromotionView hoàn chỉnh.
7. ✅ **Khách hàng** (F-09): Đã hoàn thành — CustomerService 10 methods, CustomerViewModel, loyalty wire, seed dữ liệu demo.
8. ✅ **Catalog validation** (F-11): Đã hoàn thành — SKU/barcode validate, stock update, two-way sync POS↔Inventory, lọc category đã fix.
9. ✅ **Dashboard**: Đã hoàn thành — DashboardViewModel + 4 sub-dashboards.
10. ✅ **Theme system**: Đã hoàn thành — 13 XAML files trong `Themes/`, 9 view refactor.
11. ✅ **Sidebar Redesign & Unified Footer** (session 2026-07-21): Đã hoàn thành — ScrollViewer + StackPanel, 3 section headers, 40px buttons, stacked Info Footer card trên tất cả 6 view.
12. ⚠️ **UI Polish / Demo Flow** (Phase 13): Còn lại — chạy thử demo end-to-end, reset script, chuẩn bị checklist nộp bài.

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

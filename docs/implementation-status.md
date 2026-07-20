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
- Repository và service implementation còn stub ở Return (repo + service throw NotImplementedException). CustomerService đã hoàn thành đầy đủ. Các module đã hoàn thành: Auth, Shift, Product, Promotion, Cash Payment, VNPay, Callback, Invoice, Customer, Catalog, InventorySync, Report, Device, AuditLog.
- Đã có 6 migration POS: `InitialCreate`, `AddUserContactFields`, `AddAffectedRowsToSyncLog`, `AddProductImagePath`, `AddLoyaltyPointsToOrder`, `UpdateCustomerStatusAndOrders`.
- Đã apply migration POS vào PostgreSQL Docker container qua port `5433`.
- AuthService có hàm tạo user demo `quantri`, `quanly`, `nhanvien`; `DataSeeder` hiện seed danh mục và 10 sản phẩm mẫu.
- Đã có UI flow đăng nhập, mở ca, bán hàng/cash payment (tích hợp customer lookup/promotion code/loyalty points), VNPay QR/polling, hóa đơn preview, đồng bộ catalog/tồn kho, quản lý danh mục/sản phẩm, quản lý khách hàng (CustomerViewModel + CustomerView), quản lý khuyến mãi (PromotionViewModel + PromotionView), quản lý người dùng (UserManagementViewModel + UserManagementView) và báo cáo doanh thu/ca. Các View/ViewModel shell đã tạo nhưng chưa implement logic: ReturnViewModel/View, AuditLogViewModel/View.

Vì vậy, trạng thái hiện tại nên hiểu là: **nền project đã vững, khoảng 85% tính năng nghiệp vụ đã hoàn thành, cần tập trung vào Return/Refund**.

## Trạng thái tính năng

| Mã | Tính năng | Trạng thái | Ghi chú |
|---|---|---|---|
| F-01 | Đăng nhập, đăng xuất, tạo tài khoản | Đã xong | Đã implement repository, AuthService với BCrypt/JWT, seed 3 user demo, LoginViewModel, LoginView theo thiết kế, điều hướng theo role và kiểm thử thủ công 3 tài khoản demo |
| F-02 | Mở ca, đóng ca | Đã xong | Đã implement IShiftRepository (GetOpenShiftAsync, GetByIdAsync, AddAsync, UpdateAsync, GetCashRevenueAsync, GetTotalSalesAsync), ShiftService (OpenShiftAsync, CloseShiftAsync, GetOpenShiftAsync, GetShiftSummaryAsync), ShiftViewModel với CommunityToolkit.Mvvm, ShiftView.xaml UI mở/đóng ca, lưu ca vào CurrentSessionContext; 5 unit test pass. ShiftView.xaml.cs gọi InitializeAsync khi Loaded để tự nạp ca đang mở sau khi app khởi động lại; sau khi mở ca thành công tự điều hướng sang SalesView. |
| F-03 | Bán hàng: chọn sản phẩm, tìm kiếm, giỏ hàng, tính tiền | Cần kiểm thử | Đã implement ProductRepository/ProductService tìm barcode/search, CartService thêm/sửa/xóa/tính lại (có tax 10%), kiểm tra sản phẩm ngừng kinh doanh và hết hàng, SalesViewModel/SalesView đã tích hợp đầy đủ: search/barcode scan, cart CRUD, customer lookup/creation popup, promotion code input, loyalty points toggle, checkout navigation và 8 test CartService. Cần kiểm thử end-to-end trên WPF. |
| F-04 | Khuyến mãi và giảm giá | Đã xong | PromotionRepository đã có 18 method. PromotionService đã implement đầy đủ 22 methods: ValidateCodeAsync (check code/active/expire/minOrder/product), ApplyPromotionAsync (áp dụng giảm giá vào cart), RequestApprovalAsync, CreatePromotionAsync, CRUD (GetById/Code/Name, GetAll, GetExpired/Unexpired, Search, GetActive, Delete), 10 individual Update* methods. PromotionViewModel đã implement đầy đủ (search, filter, CRUD, create popup, view detail, toggle active). PromotionView đã hoàn chỉnh (338 lines). SalesViewModel đã wire promotion code input và apply vào checkout flow. DTO: PromotionDto, CreatePromotionDto, PromotionValidationResultDto. |
| F-05 | Thanh toán tiền mặt | Đã xong | Đã implement OrderRepository, PaymentService.CreateOrderFromCartAsync và RecordCashPaymentAsync, PaymentViewModel với quick-cash buttons, PaymentView.xaml, kiểm tra thiếu tiền/order khóa và tạo payment/order status. |
| F-06 | Thanh toán VNPay | Đã xong | Đã có tạo URL/QR VNPay, khóa order, poll trạng thái mỗi 2 giây, hủy/timeout mở khóa order và test service liên quan |
| F-07 | Callback thanh toán | Đã xong | Callback API nhận GET/POST qua ngrok, validate chữ ký HMAC, xử lý success/failure, trả trang HTML kết quả và cập nhật payment/order |
| F-08 | Hóa đơn và giả lập in | Đã xong | InvoiceService tạo hóa đơn theo ngày, chặn invoice khi payment chưa thành công, InvoiceView xem hóa đơn và modal in giả lập, DeviceService ghi log preview; test invoice/fake print pass |
| F-09 | Khách hàng và điểm tích lũy | Đã xong | CustomerRepository có GetByPhoneAsync, GetByIdAsync, AddAsync, UpdateAsync, GetCustomersQueryAsync. CustomerService đã implement đầy đủ 10 method. CustomerViewModel có search, filter, sort, xem chi tiết khách hàng, sửa thông tin, toggle status, xem chi tiết đơn hàng. CustomerView đã hoàn chỉnh. |
| F-10 | Trả hàng và hoàn tiền | Đã xong | Đã implement ReturnRepository (GetByIdAsync, GetByOrderIdAsync, GetAllAsync, AddAsync, UpdateAsync), ReturnService (CreateReturnAsync, ApproveAsync, RejectAsync, restock local + Inventory API, trừ điểm tích lũy), ReturnViewModel (tạo/tìm đơn, phê duyệt/từ chối), ReturnView.xaml thiết kế chuẩn theme, 5 unit test pass. |
| F-11 | Danh mục sản phẩm | Đã xong | CategoryRepository/ProductRepository đã implement đầy đủ. CatalogService đã có CRUD category, CRUD product, UpdatePriceAsync (có audit log), DeactivateProductAsync, ReactivateProductAsync, UpdateProductImageAsync, RegisterProductToInventoryAsync, validate SKU/barcode duy nhất, UpdateStockAsync, hai chiều đồng bộ POS↔Inventory. |
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

1. **Trả hàng** (F-10): Implement `ReturnRepository`, `ReturnService` (CreateReturnAsync, ApproveAsync, RejectAsync), `ReturnViewModel` — bắt buộc cho demo cuối.
2. **Seed data**: Thêm mã khuyến mãi `GIAM10` vào `DataSeeder` hoặc `InventoryDataSeeder`.
3. **Báo cáo doanh thu** (F-15): ✅ Đã hoàn thành — `ReportService.GetSalesReportAsync` đã implement đầy đủ.
4. **Audit log viewer** (F-15): Implement `AuditLogViewModel` để hiển thị audit logs.
5. **Quản lý user**: ✅ Đã hoàn thành — `UserManagementViewModel` đã implement đầy đủ.
6. **UI polish**: Hoàn thiện navigation, loading state, thông báo lỗi tiếng Việt.
7. **Khuyến mãi** (F-04): ✅ Đã hoàn thành — PromotionService 22 methods, PromotionViewModel CRUD, PromotionView hoàn chỉnh.
8. **Khách hàng** (F-09): ✅ Đã hoàn thành — CustomerService 10 methods, CustomerViewModel, loyalty wire.
9. **Catalog validation** (F-11): ✅ Đã hoàn thành — SKU/barcode validate, stock update, two-way sync POS↔Inventory.
10. **Dashboard**: ✅ Đã hoàn thành — DashboardViewModel + 4 sub-dashboards (CatalogPromo, Inventory, Report, UserStaff).
11. **Theme system**: ✅ Đã hoàn thành — 13 XAML files trong `Themes/`, 9 view đã refactor dùng global `{StaticResource}`.
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

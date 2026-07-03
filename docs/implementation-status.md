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
- Business logic đã có cho F-01 Auth/JWT, F-02 Shift, F-03 Product/Search/Cart mức cơ bản, F-05 Cash Payment, F-06 VNPay, F-07 Callback, F-08 Invoice, F-11 Catalog, F-13 Inventory Sync, F-14 Device, F-15 Shift Report và Audit Log.
- Repository và service implementation còn stub ở Return (repo + service throw NotImplementedException), Promotion (validate/apply/approval throw NotImplementedException). Report còn thiếu `GetSalesReportAsync`. CustomerService đã hoàn thành đầy đủ (FindByPhone, Create, AddLoyaltyPoints, DeductLoyaltyPoints, CalculatePoints, GetCustomerList, ToggleStatus, GetDetail, Update, GetOrderDetail). Các module đã hoàn thành: Invoice, Device, AuditLog, VNPay, Catalog, InventorySync, Customer.
- Đã có 6 migration POS: `InitialCreate`, `AddUserContactFields`, `AddAffectedRowsToSyncLog`, `AddProductImagePath`, `AddLoyaltyPointsToOrder`, `UpdateCustomerStatusAndOrders`.
- Đã apply migration POS vào PostgreSQL Docker container qua port `5433`.
- AuthService có hàm tạo user demo `quantri`, `quanly`, `nhanvien`; `DataSeeder` hiện seed danh mục và 10 sản phẩm mẫu.
- Đã có UI flow đăng nhập, mở ca, bán hàng/cash payment (tích hợp customer lookup/promotion code/loyalty points), VNPay QR/polling, hóa đơn preview, đồng bộ catalog/tồn kho, quản lý danh mục/sản phẩm, quản lý khách hàng (CustomerViewModel + CustomerView) và báo cáo ca. Các View/ViewModel shell đã tạo nhưng chưa implement logic: PromotionViewModel/View, ReturnViewModel/View, AuditLogViewModel/View, UserManagementViewModel/View.

Vì vậy, trạng thái hiện tại nên hiểu là: **nền project đã vững, khoảng 70% tính năng nghiệp vụ đã hoàn thành, cần tập trung vào Promotion validate/apply, Return/Refund và Sales Report**.

## Trạng thái tính năng

| Mã | Tính năng | Trạng thái | Ghi chú |
|---|---|---|---|
| F-01 | Đăng nhập, đăng xuất, tạo tài khoản | Đã xong | Đã implement repository, AuthService với BCrypt/JWT, seed 3 user demo, LoginViewModel, LoginView theo thiết kế, điều hướng theo role và kiểm thử thủ công 3 tài khoản demo |
| F-02 | Mở ca, đóng ca | Đã xong | Đã implement IShiftRepository (GetOpenShiftAsync, GetByIdAsync, AddAsync, UpdateAsync, GetCashRevenueAsync, GetTotalSalesAsync), ShiftService (OpenShiftAsync, CloseShiftAsync, GetOpenShiftAsync, GetShiftSummaryAsync), ShiftViewModel với CommunityToolkit.Mvvm, ShiftView.xaml UI mở/đóng ca, lưu ca vào CurrentSessionContext; 5 unit test pass. ShiftView.xaml.cs gọi InitializeAsync khi Loaded để tự nạp ca đang mở sau khi app khởi động lại; sau khi mở ca thành công tự điều hướng sang SalesView. |
| F-03 | Bán hàng: chọn sản phẩm, tìm kiếm, giỏ hàng, tính tiền | Cần kiểm thử | Đã implement ProductRepository/ProductService tìm barcode/search, CartService thêm/sửa/xóa/tính lại (có tax 10%), kiểm tra sản phẩm ngừng kinh doanh và hết hàng, SalesViewModel/SalesView đã tích hợp đầy đủ: search/barcode scan, cart CRUD, customer lookup/creation popup, promotion code input, loyalty points toggle, checkout navigation và 8 test CartService. Cần kiểm thử end-to-end trên WPF. |
| F-04 | Khuyến mãi và giảm giá | Đang làm | PromotionRepository đã có 18 method (CRUD, search, filter). PromotionService đã có CreatePromotionAsync, GetByCodeAsync, GetAllPromotionsAsync, nhiều Update* method, DeleteAsync. Còn 3 method core chưa implement: ValidateCodeAsync, ApplyPromotionAsync, RequestApprovalAsync (đều throw NotImplementedException). PromotionViewModel vẫn là skeleton. |
| F-05 | Thanh toán tiền mặt | Đã xong | Đã implement OrderRepository, PaymentService.CreateOrderFromCartAsync và RecordCashPaymentAsync, PaymentViewModel với quick-cash buttons, PaymentView.xaml, kiểm tra thiếu tiền/order khóa và tạo payment/order status. Inventory/audit side effect vẫn bọc lỗi vì F-13/F-15 chưa hoàn chỉnh. |
| F-06 | Thanh toán VNPay | Đã xong | Đã có tạo URL/QR VNPay, khóa order, poll trạng thái mỗi 2 giây, hủy/timeout mở khóa order và test service liên quan |
| F-07 | Callback thanh toán | Đã xong | Callback API nhận GET/POST qua ngrok, validate chữ ký HMAC, xử lý success/failure, trả trang HTML kết quả và cập nhật payment/order |
| F-08 | Hóa đơn và giả lập in | Đã xong | InvoiceService tạo hóa đơn theo ngày, chặn invoice khi payment chưa thành công, InvoiceView xem hóa đơn và modal in giả lập, DeviceService ghi log preview; test invoice/fake print pass |
| F-09 | Khách hàng và điểm tích lũy | Đã xong | CustomerRepository có GetByPhoneAsync, GetByIdAsync, AddAsync, UpdateAsync, GetCustomersQueryAsync. CustomerService đã implement đầy đủ 10 method: FindByPhoneAsync, CreateAsync, AddLoyaltyPointsAsync, DeductLoyaltyPointsAsync, CalculatePoints (subtotal/10000), GetCustomerListAsync (search/filter/sort), ToggleCustomerStatusAsync, GetCustomerDetailAsync, UpdateCustomerAsync, GetCustomerOrderDetailAsync. CustomerViewModel có search, filter (status/order), sort (9 options), xem chi tiết khách hàng, sửa thông tin, toggle status (ban/unban), xem chi tiết đơn hàng. CustomerView đã hoàn chỉnh với popup chi tiết. SalesViewModel đã wire customer lookup/creation popup và loyalty points toggle. PaymentService.CreateOrderFromCartAsync đã map loyalty fields (PointsEarned, PointsUsed, PointsDiscountAmount). 2 migration mới: AddLoyaltyPointsToOrder, UpdateCustomerStatusAndOrders. DTO mới: CustomerListDto, CustomerDetailDto, CustomerOrderDto, CustomerOrderItemDto, CustomerOrderDetailDto, UpdateCustomerDto. |
| F-10 | Trả hàng và hoàn tiền | Chưa làm | Bắt buộc cho demo cuối; cần approve/reject và restock |
| F-11 | Danh mục sản phẩm | Đang làm | CategoryRepository/ProductRepository đã implement đầy đủ. CatalogService đã có CRUD category, CRUD product, UpdatePriceAsync (có audit log), DeactivateProductAsync, ReactivateProductAsync, UpdateProductImageAsync, RegisterProductToInventoryAsync. CatalogViewModel đã có UI filter/search/add/update price/deactivate/reactivate/image. DataSeeder có 3 danh mục và 10 sản phẩm với ExternalInventoryId. Còn thiếu validate SKU/barcode/QR duy nhất và test. |
| F-12 | Giá, thuế và khuyến mãi | Đang làm | AuditLogRepository đã implement (AddAsync, GetByEntityAsync, GetRecentAsync). AuditService.LogAsync đã implement và được gọi từ CatalogService (tạo/sửa category, tạo/sửa/giá/deactivate/reactivate/image product) và PaymentService (cash payment). CartService.Recalculate có TaxAmount nhưng hardcode = 0. Promotion CRUD đã có (xem F-04). AuditLogViewModel vẫn skeleton. |
| F-13 | Đồng bộ Inventory Manager | Đã xong | InventoryDbContext, entity riêng, migration InitialInventoryCreate. Inventory API: SyncController (GET catalog, GET stock), StockController (POST deduct với validation + StockTransaction, POST restock), ProductsController (GET products, POST create product với SKU/barcode validation, GET categories). InventoryDataSeeder có 3 category + 10 product + 10 stock item với fixed GUID. POS InventorySyncService: SyncCatalogAsync (upsert by ExternalInventoryId), SyncStockAsync (update LocalStockQuantity, trả PARTIAL nếu skip), SendStockDeductionAsync (gọi API + trừ local stock), RestockAsync, RegisterProductToInventoryAsync. InventorySyncLogRepository đã implement (AddAsync, GetLatestAsync). SyncViewModel có SyncCatalog/SyncStock/SyncAll commands. 6 test sync pass. |
| F-14 | Thiết bị giả lập và nhật ký thiết bị | Đã xong | Đã có DeviceService cho printer giả lập, DeviceLogRepository ghi sự kiện preview/in giả lập và test device log cơ bản |
| F-15 | Báo cáo, audit log và cấu hình cửa hàng | Đang làm | ReportService.GetShiftReportAsync đã implement đầy đủ (total sales, cash/VNPay revenue, order count, top products, order log). ReportViewModel và ReportView.xaml đã xong với 4 summary card, recent shifts, top products bar chart, order log table. AuditLogRepository (AddAsync, GetByEntityAsync, GetRecentAsync) và AuditService.LogAsync đã implement. Còn thiếu: GetSalesReportAsync (NotImplementedException), AuditLogViewModel (skeleton), store configuration. |

## Trạng thái project

| Project | Trạng thái | Ghi chú |
|---|---|---|
| SmartPOS.Shared | Cần kiểm thử | Đã có enum, DTO, exception, constant; cần rà lại shape DTO khi implement thật |
| SmartPOS.Data | Đang làm | Đã có entity, DbContext, design-time factory, 6 migrations POS (InitialCreate, AddUserContactFields, AddAffectedRowsToSyncLog, AddProductImagePath, AddLoyaltyPointsToOrder, UpdateCustomerStatusAndOrders), DataSeeder sản phẩm mẫu. Các repository đã implement: Auth, Shift, Product, Category, Order, Invoice, Device, DeviceLog, InventorySyncLog, AuditLog, Promotion, Customer. Repository Return còn stub (NotImplementedException). |
| SmartPOS.Services | Đang làm | Đã có logic: Auth, Shift, Product, Cart (tax 10%, inactive/stock validation), Catalog (CRUD + deactivate/reactivate/image), Cash Payment, VNPay (tạo URL/QR/poll/cancel/callback), Invoice (tạo hóa đơn theo ngày + preview), Device (fake print + log), InventorySync (sync catalog/stock + deduct/restock + register + sync log), Report (shift report + recent shifts + top products + order log), Audit (log async), Customer (10 method: CRUD + loyalty + list/detail/toggle). Còn stub: ReturnService (NotImplementedException), PromotionService.ValidateCodeAsync/ApplyPromotionAsync/RequestApprovalAsync, ReportService.GetSalesReportAsync. |
| SmartPOS.WPF | Đang làm | 13 ViewModel đã tạo: LoginViewModel (real), ShiftViewModel (real — clock realtime), SalesViewModel (real — search/barcode/cart/customer/promotion/loyalty/checkout), PaymentViewModel (real — cash + VNPay QR/polling), InvoiceViewModel (real — preview + fake print), CatalogViewModel (real — CRUD + filter/search/deactivate/reactivate/image + inline sync), ReportViewModel (real — shift report + recent shifts + top products + order log), SyncViewModel (real — sync catalog/stock/all), CustomerViewModel (real — search/filter/sort/detail/edit/toggle status/view orders). Các màn vẫn placeholder/TODO (shell, chưa implement logic): PromotionViewModel, ReturnViewModel, AuditLogViewModel, UserManagementViewModel. 13 View tương ứng đã tạo (CustomerView đã hoàn chỉnh). NavigationService map ViewModel → View bằng convention Replace("ViewModel","View"). App.xaml.cs tự động migrate database và seed data khi startup. |
| SmartPOS.CallbackApi | Đã xong | Đã có endpoint callback thật cho VNPay, nhận GET/POST, validate signature, gọi PaymentService và trả trang HTML kết quả. |
| InventoryManager.Api | Đã xong | Đã có `InventoryDbContext`, entity inventory riêng, migration `InitialInventoryCreate`, InventoryDataSeeder (3 categories + 10 products + 10 stock items với fixed GUID). SyncController (GET catalog/stock), StockController (POST deduct/restock với validation + StockTransaction), ProductsController (GET all products, POST create product với SKU/barcode validation, GET categories). Swagger enabled. |
| SmartPOS.Tests | Đang làm | Test suite hiện có 44 test thật + 2 placeholder (PromotionServiceTests, ReturnServiceTests chỉ có `Assert.True(true)`). Các test đã implement: AuthServiceTests (7), ShiftServiceTests (5), CartServiceTests (8 test thật - add/update/remove/recalculate/tax/stock/inactive), PaymentServiceTests (11 bao gồm VNPay), InvoiceServiceTests (4), DeviceServiceTests (3), InventorySyncServiceTests (6). Cần bổ sung: test thật cho Promotion, Return, và test cho search/catalog/audit/report. |

## Việc nên làm tiếp theo

Chi tiết task nằm trong `docs/development-task-list.md`. Thứ tự ưu tiên ngắn gọn:

1. **Khuyến mãi** (F-04): Implement `PromotionService.ValidateCodeAsync`, `ApplyPromotionAsync`, `RequestApprovalAsync` và `PromotionViewModel` — bắt buộc cho luồng demo.
2. **Trả hàng** (F-10): Implement `ReturnRepository`, `ReturnService` (CreateReturnAsync, ApproveAsync, RejectAsync), `ReturnViewModel` — bắt buộc cho demo cuối.
3. **Khách hàng** (F-09): ✅ Đã hoàn thành - CustomerService (10 methods), CustomerViewModel, CustomerView đã implement đầy đủ. Loyalty points đã wire vào SalesViewModel và PaymentService.
4. **Seed data**: Thêm mã khuyến mãi `GIAM10` vào `DataSeeder` hoặc `InventoryDataSeeder`.
5. **Test thật**: Thay 2 placeholder test (Promotion, Return) bằng test thật; bổ sung test cho Catalog/Audit/Report.
6. **Báo cáo doanh thu** (F-15): Implement `ReportService.GetSalesReportAsync`.
7. **Audit log viewer** (F-15): Implement `AuditLogViewModel` để hiển thị audit logs.
8. **Quản lý user**: Implement `UserManagementViewModel` (tạo tài khoản mới khi giảng viên yêu cầu).
9. **UI polish**: Hoàn thiện navigation, loading state, thông báo lỗi tiếng Việt.

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

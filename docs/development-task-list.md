# Development Task List

File này là checklist triển khai dự án SmartPOS từ trạng thái hiện tại đến demo cuối. Khi làm xong task nào, cập nhật checkbox và đồng bộ trạng thái feature trong `docs/implementation-status.md`.

## Cách Dùng

- Làm theo thứ tự từ trên xuống nếu chưa có task khẩn cấp hơn.
- Mỗi task nên nhỏ đủ để một người làm, review và test được.
- Khi chia việc cho 6 thành viên triển khai, dùng bảng phân công trong `docs/team-workflow.md`.
- Khi task thay đổi schema, API hoặc business rule, cập nhật docs liên quan.
- Khi hoàn thành feature, cập nhật `docs/implementation-status.md`.
- Không implement business logic trong ViewModel hoặc Repository.

Trạng thái đề xuất:

```text
[ ] Chưa làm
[~] Đang làm
[x] Đã xong
```

## Phase 0 - Dọn Nền Tảng Sau Scaffold

- [x] TASK-0001: Chạy `dotnet build SmartPOS.sln` và ghi lại warning/error còn lại.
- [x] TASK-0002: Kiểm tra lại toàn bộ package trong `.csproj`, đảm bảo không có package ngoài danh sách đã thống nhất.
- [x] TASK-0003: Cập nhật `docker-compose.yml` để PostgreSQL có user, password và database khớp với `appsettings.json`.
- [x] TASK-0004: Tách Inventory Manager sang `InventoryDbContext` riêng.
- [x] TASK-0005: Tạo entity riêng cho `inventory_products`, `inventory_categories`, `stock_items`, `stock_transactions`.
- [x] TASK-0006: Tạo migration đầu tiên cho POS.
- [x] TASK-0007: Chạy `dotnet ef database update` cho POS.
- [x] TASK-0008: Tạo seed data tối thiểu cho tài khoản demo, danh mục, sản phẩm, tồn kho và mã khuyến mãi. AuthService có hàm tạo user demo; DataSeeder đã có danh mục và 10 sản phẩm/tồn kho cục bộ; còn cần đảm bảo user demo được gọi trong startup, thêm `external_inventory_id` và mã `GIAM10`. (Đã hoàn thành và map ID chuẩn)
- [x] TASK-0009: Kiểm tra WPF khởi động được tới `LoginView`.
- [x] TASK-0010: Kiểm tra Callback API và Inventory API start được trên port dự kiến.

## Phase 1 - Auth, Session Và Phân Quyền

Liên quan: F-01.

- [x] TASK-0101: Hoàn thiện `User` và `UserSession` entity nếu còn thiếu field cần cho login.
- [x] TASK-0102: Implement `IUserRepository`.
- [x] TASK-0103: Implement `IUserSessionRepository`.
- [x] TASK-0104: Implement hash password bằng BCrypt.
- [x] TASK-0105: Implement `AuthService.LoginAsync`.
- [x] TASK-0106: Implement `AuthService.LogoutAsync`.
- [x] TASK-0107: Implement validate session/token tối giản cho demo.
- [x] TASK-0108: Implement tạo tài khoản demo nếu database chưa có user.
- [x] TASK-0109: Implement `LoginViewModel` command đăng nhập.
- [x] TASK-0110: Hiển thị lỗi đăng nhập bằng tiếng Việt có dấu.
- [x] TASK-0111: Điều hướng sau login theo role.
- [x] TASK-0112: Viết test cho login thành công, sai mật khẩu, tài khoản bị khóa.

## Phase 2 - Mở Ca Và Đóng Ca

Liên quan: F-02.

- [x] TASK-0201: Implement `IShiftRepository`.
- [x] TASK-0202: Implement `ShiftService.OpenShiftAsync`.
- [x] TASK-0203: Chặn mở ca mới nếu user còn ca đang mở.
- [x] TASK-0204: Implement `ShiftService.CloseShiftAsync`.
- [x] TASK-0205: Tính tiền mặt dự kiến khi đóng ca.
- [x] TASK-0206: Implement `ShiftService.GetOpenShiftAsync`.
- [x] TASK-0207: Implement `ShiftViewModel` mở ca, đóng ca.
- [x] TASK-0208: Lưu ca hiện tại vào `CurrentSessionContext`.
- [x] TASK-0209: Viết test cho mở ca, đóng ca, không cho mở trùng ca.

## Phase 3 - Catalog, Product Và Inventory Seed

Liên quan: F-03, F-11, F-12.

- [x] TASK-0301: Implement `ICategoryRepository`.
- [x] TASK-0302: Implement `IProductRepository`.
- [x] TASK-0303: Implement `CatalogService` tạo danh mục, tạo sản phẩm, cập nhật giá. Đã có logic cơ bản, validate SKU/barcode/QR duy nhất, deactivate và audit.
- [x] TASK-0304: Implement `ProductService.FindByBarcodeAsync`.
- [x] TASK-0305: Implement `ProductService.SearchAsync`.
- [x] TASK-0306: Tạo dữ liệu mẫu tối thiểu 10 sản phẩm.
- [x] TASK-0307: Implement `CatalogViewModel`. (Bao gồm sửa lỗi Race condition khi load song song)
- [x] TASK-0308: Implement `SalesViewModel` phần tìm sản phẩm và quét mã giả lập.
- [x] TASK-0309: Viết test tìm sản phẩm theo barcode, SKU, tên. (Bỏ qua theo yêu cầu)

## Phase 4 - Cart Và Luồng Bán Hàng Cơ Bản

Liên quan: F-03.

- [x] TASK-0401: Chuẩn hóa `CartItemDto` và `CartSummaryDto` ở mức hiện tại.
- [x] TASK-0402: Implement `CartService.AddItem`.
- [x] TASK-0403: Implement `CartService.UpdateItem`.
- [x] TASK-0404: Implement `CartService.RemoveItem`.
- [x] TASK-0405: Implement `CartService.Recalculate`.
- [x] TASK-0406: Kiểm tra tồn kho trước khi thêm sản phẩm vào giỏ.
- [x] TASK-0407: Implement UI bán hàng cơ bản trong `SalesView`.
- [x] TASK-0408: Hiển thị subtotal, discount, tax, total. (Thuế đã giảm từ 10% xuống 3% trong CartService)
- [x] TASK-0409: Viết test thêm sản phẩm, sửa số lượng, xóa sản phẩm, hết hàng. (CartServiceTests có 8 test thật)

## Phase 5 - Order Và Thanh Toán Tiền Mặt

Liên quan: F-05.

- [x] TASK-0501: Implement `IOrderRepository`.
- [x] TASK-0502: Implement tạo order draft từ giỏ hàng.
- [x] TASK-0503: Implement `PaymentService.RecordCashPaymentAsync`.
- [x] TASK-0504: Chặn thanh toán nếu chưa mở ca.
- [x] TASK-0505: Chặn thanh toán nếu tiền khách đưa không đủ.
- [x] TASK-0506: Cập nhật `OrderStatus`, `PaymentStatus`, `PaymentMethod` sau khi thanh toán.
- [x] TASK-0507: Ghi payment record.
- [x] TASK-0508: Gửi sự kiện trừ kho sau khi thanh toán thành công.
- [x] TASK-0509: Implement `PaymentViewModel` cho tiền mặt.
- [x] TASK-0510: Viết test thanh toán tiền mặt thành công, thiếu tiền, order bị khóa.

## Phase 6 - VNPay Callback

Liên quan: F-06, F-07.

- [x] TASK-0601: Chuẩn hóa config VNPay trong `appsettings.json`.
- [x] TASK-0602: Implement tạo URL thanh toán VNPay.
- [x] TASK-0603: Implement QR hiển thị trong WPF.
- [x] TASK-0604: Khóa order khi đang chờ thanh toán VNPay.
- [x] TASK-0605: Implement endpoint `POST /api/vnpay/callback`.
- [x] TASK-0606: Implement kiểm tra chữ ký VNPay HMAC-SHA512.
- [x] TASK-0607: Implement `PaymentService.HandleVNPayCallbackAsync`.
- [x] TASK-0608: Implement poll trạng thái thanh toán mỗi 2 giây.
- [x] TASK-0609: Mở khóa order khi VNPay thất bại hoặc timeout.
- [x] TASK-0610: Viết test callback thành công, callback sai chữ ký, timeout.

## Phase 7 - Invoice Và Giả Lập In

Liên quan: F-08.

- [x] TASK-0701: Implement `IInvoiceRepository`.
- [x] TASK-0702: Implement tạo số hóa đơn theo ngày.
- [x] TASK-0703: Implement `InvoiceService.CreateInvoiceAsync`.
- [x] TASK-0704: Chỉ tạo invoice khi payment thành công.
- [x] TASK-0705: Implement màn hình xem trước hóa đơn.
- [x] TASK-0706: Implement `DeviceService` cho giả lập in.
- [x] TASK-0707: Ghi `DeviceLog` khi in thành công hoặc thất bại.
- [x] TASK-0708: Viết test tạo hóa đơn, không tạo hóa đơn khi payment chưa thành công.

## Phase 8 - Customer Và Điểm Tích Lũy

Liên quan: F-09.

- [x] TASK-0801: Implement `ICustomerRepository`. ✅ (GetByPhoneAsync, GetByIdAsync, AddAsync, UpdateAsync, GetCustomersQueryAsync)
- [x] TASK-0802: Implement tìm khách theo số điện thoại. ✅ (FindByPhoneAsync)
- [x] TASK-0803: Implement tạo khách hàng mới. ✅ (CreateAsync + SalesView popup)
- [x] TASK-0804: Implement cộng điểm sau thanh toán thành công. ✅ (AddLoyaltyPointsAsync)
- [x] TASK-0805: Implement trừ điểm khi return được duyệt. ✅ (DeductLoyaltyPointsAsync)
- [x] TASK-0806: Implement `CustomerViewModel`. ✅ (search/filter/sort/detail/edit/toggle/view orders)
- [x] TASK-0807: Viết test tạo khách, tìm khách, cộng/trừ điểm. (Bỏ qua theo yêu cầu)

## Phase 9 - Promotion

Liên quan: F-04, F-12.

- [x] TASK-0901: Implement `IPromotionRepository`. 18 methods.
- [x] TASK-0902: Implement kiem tra ma khuyen mai. ValidateCodeAsync.
- [x] TASK-0903: Kiem tra ngay hieu luc.
- [x] TASK-0904: Kiem tra gia tri don hang toi thieu.
- [x] TASK-0905: Kiem tra san pham ap dung neu co.
- [x] TASK-0906: Implement nguong can Manager/Admin phe duyet. RequestApprovalAsync.
- [x] TASK-0907: Chi cho ap dung mot khuyen mai trong mot don. ApplyPromotionAsync.
- [x] TASK-0908: Implement `PromotionViewModel`. Full CRUD, PromotionView 338 lines.
- [x] TASK-0909: Viet test promotion. PromotionServiceTests.

## Phase 10 - Return Và Refund

Liên quan: F-10.

- [x] TASK-1001: Implement `IReturnRepository`.
- [x] TASK-1002: Implement tạo yêu cầu trả hàng.
- [x] TASK-1003: Chỉ cho trả hàng với order đã thanh toán thành công.
- [x] TASK-1004: Chặn số lượng trả vượt quá số lượng đã mua trừ số lượng đã trả trước đó.
- [x] TASK-1005: Implement duyệt trả hàng.
- [x] TASK-1006: Implement từ chối trả hàng.
- [x] TASK-1007: Gửi sự kiện nhập lại hàng sang Inventory Manager khi duyệt.
- [x] TASK-1008: Cập nhật điểm khách hàng nếu cần.
- [x] TASK-1009: Implement `ReturnViewModel`.
- [x] TASK-1010: Viết test tạo return, duyệt return, từ chối return, trả quá số lượng.

## Phase 11 - Inventory Manager Sync

Liên quan: F-13.

- [x] TASK-1101: Hoàn thiện endpoint `GET /api/sync/catalog` ở mức cơ bản.
- [x] TASK-1102: Hoàn thiện endpoint `GET /api/sync/stock` ở mức cơ bản.
- [x] TASK-1103: Hoàn thiện endpoint `POST /api/stock/deduct` ở mức cơ bản.
- [x] TASK-1104: Hoàn thiện endpoint `POST /api/stock/restock` ở mức cơ bản.
- [x] TASK-1105: Implement `InventorySyncService.SyncCatalogAsync`. Đã upsert sản phẩm POS theo `external_inventory_id`, tạo mới và cập nhật đầy đủ.
- [x] TASK-1106: Implement `InventorySyncService.SyncStockAsync`. Đã cập nhật `LocalStockQuantity`, trả PARTIAL nếu skip sản phẩm không tìm thấy.
- [x] TASK-1107: Implement ghi `InventorySyncLog`. `InventorySyncLogRepository` đã có `AddAsync` và `GetLatestAsync`.
- [x] TASK-1108: Xử lý lỗi khi Inventory Manager API không chạy. Service catch/log, trả `FAILED` và ghi `InventorySyncLog` với `SyncStatus.Failed`.
- [x] TASK-1109: Implement `SyncViewModel`. Đã có SyncCatalog, SyncStock, SyncAll commands với loading state và status tracking.
- [x] TASK-1110: Viết test sync thành công, sync thất bại, partial sync. `InventorySyncServiceTests` có 6 test: catalog upsert, stock update, catalog/stock API down → FAILED, partial sync.

## Phase 12 - Report Và Audit Log

Liên quan: F-15.

- [x] TASK-1201: Implement `IAuditLogRepository`. Đã có `AddAsync`, `GetByEntityAsync`, `GetRecentAsync`.
- [x] TASK-1202: Implement `AuditService.LogAsync`. Đã tạo `AuditLog` entity, JSON serialize old/new values, gracefully swallow errors.
- [x] TASK-1203: Ghi audit log cho thanh toán, sửa giá, khuyến mãi, trả hàng. Đã ghi đầy đủ cho: thanh toán, sửa giá, tạo/sửa category/product, khuyến mãi (PromotionService), trả hàng (ReturnService).
- [x] TASK-1204: Implement `IReportService.GetShiftReportAsync`.
- [x] TASK-1205: Implement IReportService.GetSalesReportAsync.
- [x] TASK-1206: Implement `ReportViewModel`.
- [x] TASK-1207: Implement `AuditLogViewModel`.
- [x] TASK-1208: Viết test report ca, report doanh thu, ghi audit log. ✅ ReportServiceTests 7 tests (valid shift, not found, revenue, cash/vnpay split, top products, order log, empty shift).

## Phase 13 - UI Polish Và Demo Flow

- [x] TASK-1301: Hoàn thiện navigation giữa các màn hình chính. (Sidebar đồng nhất ở tất cả view, có tab Transactions/Settings)
- [x] TASK-1302: Thêm loading state cho các ViewModel quan trọng. (SalesView, CatalogView có loading state)
- [x] TASK-1303: Chuẩn hóa thông báo lỗi tiếng Việt có dấu. (Các view chính đã có thông báo tiếng Việt)
- [ ] TASK-1304: Tạo dữ liệu demo đầy đủ. (Cần kiểm tra lại mã GIAM10 và seed đủ scenario)
- [ ] TASK-1305: Viết script hoặc hướng dẫn reset database demo.
- [ ] TASK-1306: Chạy thử luồng demo bắt buộc từ đầu đến cuối.
- [ ] TASK-1307: Ghi lại lỗi demo và sửa theo mức ưu tiên.
- [ ] TASK-1308: Cập nhật README nếu setup thay đổi.
- [x] TASK-1309: Cập nhật `implementation-status.md` sau buổi test demo.
- [ ] TASK-1310: Chuẩn bị checklist trước khi nộp bài.

## Phase 14 - Theme System (CSS to WPF)

- [x] TASK-1401 đến TASK-1420: Đã hoàn thành toàn bộ hệ thống theme trung tâm. 13 file XAML trong `Themes/`, đăng ký trong `App.xaml`, refactor 9 view (Login, Shift, Sales, Payment, Catalog, Customer, Report, Sync, Invoice). Màu primary chuyển sang `#0062FF`. Build pass 0 lỗi. Docs đã cập nhật.

## Phase 15 - Dashboard (Admin/Manager)

Liên quan: F-16.

- [x] TASK-1501: Tạo `IDashboardService` và `DashboardService` (GetOverviewAsync trả `DashboardOverviewDto`).
- [x] TASK-1502: Tạo `DashboardOverviewDto` (TodayRevenue, TotalOrders, NewCustomers, InventoryAlerts, Revenue7Days, TopCategories, RecentTransactions, ActivePromotions).
- [x] TASK-1503: Tạo `DashboardViewModel` (overview data + navigate to 4 sub-dashboards + logout).
- [x] TASK-1504: Tạo `DashboardView.xaml` (main dashboard layout).
- [x] TASK-1505: Tạo `DashboardCatalogPromoViewModel` + `DashboardCatalogPromoView.xaml`.
- [x] TASK-1506: Tạo `DashboardInventoryViewModel` + `DashboardInventoryView.xaml`.
- [x] TASK-1507: Tạo `DashboardReportViewModel` + `DashboardReportView.xaml`.
- [x] TASK-1508: Tạo `DashboardUserStaffViewModel` + `DashboardUserStaffView.xaml`.
- [x] TASK-1509: Đăng ký DI trong `App.xaml.cs` (IDashboardService, DashboardViewModel, 4 sub-ViewModels, 5 Views).

## Definition Of Done

Một task chỉ nên đánh dấu `[x]` khi:

- Code build được.
- Không phá flow `View -> ViewModel -> Service -> Repository -> DbContext`.
- Có xử lý lỗi tối thiểu.
- Có test service nếu task chứa business rule.
- Có cập nhật docs nếu thay đổi schema, API, config hoặc quy tắc nghiệp vụ.
- Chạy được trên máy demo hoặc có ghi chú rõ nếu chưa chạy được vì thiếu môi trường ngoài.

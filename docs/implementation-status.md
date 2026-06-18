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
- Business logic hầu hết chưa được implement.
- Repository và service implementation hiện vẫn chủ yếu là stub.
- Đã có migration POS đầu tiên `InitialCreate`.
- Đã apply migration POS vào PostgreSQL Docker container qua port `5433`.
- Chưa có seed data demo.
- Chưa có UI flow hoàn chỉnh.

Vì vậy, trạng thái hiện tại nên hiểu là: **nền project đã có, tính năng nghiệp vụ chưa hoàn thành**.

## Trạng thái tính năng

| Mã | Tính năng | Trạng thái | Ghi chú |
|---|---|---|---|
| F-01 | Đăng nhập, đăng xuất, tạo tài khoản | Đã xong | Đã implement repository, AuthService với BCrypt/JWT, seed 3 user demo, LoginViewModel, LoginView theo thiết kế, điều hướng theo role và kiểm thử thủ công 3 tài khoản demo |
| F-02 | Mở ca, đóng ca | Chưa làm | Đã có skeleton; cần implement ShiftService và lưu ca hiện tại |
| F-03 | Bán hàng: chọn sản phẩm, tìm kiếm, giỏ hàng, tính tiền | Chưa làm | Đã có skeleton ProductService/CartService/SalesViewModel; cần implement search, cart, kiểm tra tồn kho và UI |
| F-04 | Khuyến mãi và giảm giá | Chưa làm | Entity `Promotion` đã có `Code`; cần implement validation, áp dụng mã và approval |
| F-05 | Thanh toán tiền mặt | Chưa làm | Cần implement order/payment flow và trừ kho sau thanh toán |
| F-06 | Thanh toán VNPay | Chưa làm | Cần tạo URL, QR, khóa order và poll trạng thái |
| F-07 | Callback thanh toán | Chưa làm | Đã có CallbackApi shell; cần callback endpoint, chữ ký và xử lý trạng thái |
| F-08 | Hóa đơn và giả lập in | Chưa làm | Cần invoice number, preview, fake print và device log |
| F-09 | Khách hàng và điểm tích lũy | Chưa làm | Cần implement customer lookup, create, cộng/trừ điểm |
| F-10 | Trả hàng và hoàn tiền | Chưa làm | Bắt buộc cho demo cuối; cần approve/reject và restock |
| F-11 | Danh mục sản phẩm | Chưa làm | Cần implement quản lý danh mục, sản phẩm, SKU/barcode/QR duy nhất |
| F-12 | Giá, thuế và khuyến mãi | Chưa làm | Cần implement quản lý giá, thuế, khuyến mãi và audit sửa giá |
| F-13 | Đồng bộ Inventory Manager | Đang làm | Đã tách `InventoryDbContext`, entity riêng và migration `InitialInventoryCreate`; API shell cần implement sync/stock |
| F-14 | Thiết bị giả lập và nhật ký thiết bị | Chưa làm | Cần fake scanner/printer, device config và device logs |
| F-15 | Báo cáo, audit log và cấu hình cửa hàng | Chưa làm | Ưu tiên báo cáo ca và audit log trước |

## Trạng thái project

| Project | Trạng thái | Ghi chú |
|---|---|---|
| SmartPOS.Shared | Cần kiểm thử | Đã có enum, DTO, exception, constant; cần rà lại shape DTO khi implement thật |
| SmartPOS.Data | Đang làm | Đã có entity, DbContext, design-time factory, migration đầu tiên và repository skeleton; repository chưa implement |
| SmartPOS.Services | Đang làm | AuthService đã có logic BCrypt/JWT; các service nghiệp vụ khác vẫn chủ yếu là skeleton |
| SmartPOS.WPF | Đang làm | Login flow đã có giao diện theo thiết kế và điều hướng theo role; các màn hình còn lại vẫn placeholder/skeleton |
| SmartPOS.CallbackApi | Đang làm | Đã có Minimal API shell; callback chỉ trả `OK` |
| InventoryManager.Api | Đang làm | Đã có `InventoryDbContext`, entity inventory riêng, migration `InitialInventoryCreate` và controller shell; action chỉ trả `Ok()` |
| SmartPOS.Tests | Đang làm | AuthService đã có test nghiệp vụ và pass 14/14; các module khác vẫn cần bổ sung test |

## Việc nên làm tiếp theo

Chi tiết task nằm trong `docs/development-task-list.md`. Thứ tự ưu tiên ngắn gọn:

1. Tạo seed data demo.
2. Implement Auth.
3. Implement Shift.
4. Implement Product/Search và Cart.
5. Implement Cash Payment.
6. Implement Invoice.
7. Implement Inventory Sync, VNPay, Return, Report và Audit theo thứ tự demo.

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

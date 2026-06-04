# Trạng thái triển khai

File này giúp nhóm và tác nhân AI biết tính năng nào đã làm, đang làm hoặc chưa làm.
Mỗi khi hoàn thành một module, hãy cập nhật file này trước khi giao việc tiếp.

Trạng thái dùng chung:

- `Chưa làm`: chưa có code hoặc chỉ mới có ý tưởng.
- `Đang làm`: đã có code một phần, cần đọc source trước khi sửa tiếp.
- `Cần kiểm thử`: code có thể đã xong nhưng chưa đủ test hoặc chưa chạy demo.
- `Đã xong`: đã chạy được theo luồng demo và có test tối thiểu.
- `Tạm hoãn`: không thuộc ưu tiên hiện tại.

## Tổng quan hiện tại

Tại thời điểm lập docs, workspace chủ yếu mới có tài liệu. Nếu mã nguồn được tạo sau này,
hãy cập nhật bảng bên dưới theo đúng hiện trạng.

| Mã | Tính năng | Trạng thái | Ghi chú |
|---|---|---|---|
| F-01 | Đăng nhập, đăng xuất, tạo tài khoản | Chưa làm | Cần seed tài khoản demo và có màn hình tạo tài khoản dự phòng |
| F-02 | Mở ca, đóng ca | Chưa làm | Bắt buộc cho luồng bán hàng |
| F-03 | Chọn sản phẩm, giả lập máy quét, tìm kiếm | Chưa làm | Không dùng phần cứng thật |
| F-04 | Giỏ hàng | Chưa làm | Cần tính tiền và kiểm tra tồn kho |
| F-05 | Khuyến mãi theo code | Chưa làm | Cần cột `code` trong bảng `promotions` |
| F-06 | Thanh toán tiền mặt | Chưa làm | Cần trừ kho sau khi thành công |
| F-07 | Thanh toán VNPay | Chưa làm | Cần callback và kiểm tra chữ ký |
| F-08 | Hóa đơn và giả lập in | Chưa làm | Dùng màn hình xem trước và nút giả lập in thành công |
| F-09 | Khách hàng và điểm tích lũy | Chưa làm | Cần cho demo cuối |
| F-10 | Trả hàng và hoàn tiền | Chưa làm | Bắt buộc cho demo cuối |
| F-11 | Đồng bộ Inventory Manager | Chưa làm | POS và Inventory Manager chỉ nói chuyện qua API |
| F-12 | Báo cáo | Chưa làm | Ưu tiên báo cáo ca trước |
| F-13 | Nhật ký thao tác | Chưa làm | Cần cho sửa giá, thanh toán, trả hàng |

## Nhiệm vụ nên làm đầu tiên

1. Tạo solution và các project trong `src`.
2. Tạo `docker-compose.yml` với PostgreSQL.
3. Tạo cơ sở dữ liệu `smartpos` và `inventory_manager`.
4. Tạo entity và migration POS.
5. Tạo entity và migration Inventory Manager nếu tách project data riêng.
6. Tạo seed data cho tài khoản và sản phẩm mẫu.
7. Làm đăng nhập.
8. Làm mở ca.
9. Làm giỏ hàng và chọn sản phẩm.
10. Làm thanh toán tiền mặt.

## Trạng thái project

| Project | Trạng thái | Ghi chú |
|---|---|---|
| SmartPOS.Shared | Chưa làm | Tạo enum, DTO, exception và constant trước |
| SmartPOS.Data | Chưa làm | Tạo entity POS, DbContext, repository và migration |
| SmartPOS.Services | Chưa làm | Tạo service interface trước implementation |
| SmartPOS.WPF | Chưa làm | Tạo shell giao diện và điều hướng |
| SmartPOS.CallbackApi | Chưa làm | Làm sau khi có PaymentService |
| InventoryManager.Api | Chưa làm | Tạo API riêng và cơ sở dữ liệu riêng |
| SmartPOS.Tests | Chưa làm | Thêm test cho Service quan trọng |

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
- Đã cập nhật docs nếu có thay đổi về schema, API hoặc quy tắc.

## Mẫu cập nhật trạng thái

Khi cập nhật, ghi ngắn gọn:

```text
F-06 Thanh toán tiền mặt: Cần kiểm thử
Đã tạo PaymentService và OrderRepository.AddPaymentAsync.
Còn thiếu test trừ kho khi Inventory Manager lỗi.
```

## Rủi ro cần theo dõi

- VNPay cần ngrok và cấu hình callback đúng.
- Đồng bộ tồn kho dễ lỗi nếu API Inventory Manager chưa chạy.
- Trả hàng cần tính đúng số lượng đã trả trước đó.
- Khuyến mãi cần cột `code`, nếu thiếu sẽ không khớp luồng demo.
- Giả lập in phải không làm thất bại thanh toán nếu có lỗi.
- Tạo tài khoản mới cần giới hạn cho quản lý hoặc quản trị.

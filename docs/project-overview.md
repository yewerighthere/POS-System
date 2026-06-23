# Tổng quan dự án

Đọc file này trước khi viết mã. File này giải thích lý do chọn kiến trúc, phạm vi demo,
ưu tiên sản phẩm và các ràng buộc mà nhóm cần giữ.

## Bối cảnh

SmartPOS là ứng dụng bán hàng trên máy tính Windows, được xây dựng cho môn SWD392 tại FPT
University. Nhóm có 6 thành viên, trình độ mới, thời gian làm khoảng 15 tuần. Hệ thống
được trình bày trên máy tính cá nhân, không phải triển khai sản xuất.

Vì vậy mọi quyết định kỹ thuật phải đơn giản, dễ hiểu và dễ sửa lỗi. Khi có nhiều cách
làm, hãy chọn cách rõ ràng nhất mà cả nhóm có thể bảo trì.

## Hệ thống làm gì

Nhân viên bán hàng đăng nhập, mở ca, nhập tiền đầu ca, thêm sản phẩm vào giỏ hàng bằng giao
diện giả lập máy quét hoặc tìm kiếm thủ công, áp dụng khuyến mãi, nhận thanh toán tiền mặt
hoặc VNPay, xem trước hóa đơn và đóng ca cuối ngày.

Quản lý có thể xem báo cáo ca, quản lý danh mục sản phẩm, quản lý giá, quản lý khuyến mãi,
tạo tài khoản khi cần, duyệt trả hàng và xem nhật ký thao tác nhạy cảm.

POS tích hợp với Inventory Manager qua HTTP API. Hai hệ thống có cơ sở dữ liệu riêng.
POS không truy cập trực tiếp cơ sở dữ liệu của Inventory Manager, và Inventory Manager
không truy cập trực tiếp cơ sở dữ liệu của POS.

## Luồng demo bắt buộc

Đây là luồng ưu tiên cao nhất:

1. Quản lý đăng nhập.
2. Quản lý đồng bộ danh mục và tồn kho từ Inventory Manager.
3. Nhân viên đăng nhập.
4. Nhân viên mở ca và nhập tiền đầu ca.
5. Nhân viên chọn sản phẩm bằng giao diện giả lập máy quét hoặc tìm kiếm.
6. Nhân viên áp dụng mã khuyến mãi nếu có.
7. Nhân viên thanh toán VNPay bằng môi trường thử nghiệm.
8. Cổng nhận callback VNPay cập nhật trạng thái đơn hàng.
9. Hệ thống tạo hóa đơn và hiển thị bản xem trước thay cho máy in thật.
10. Nhân viên tạo đơn thứ hai và thanh toán tiền mặt.
11. Quản lý xem báo cáo ca gồm cả tiền mặt và VNPay.
12. Quản lý tạo và duyệt yêu cầu trả hàng.
13. POS gửi sự kiện nhập lại hàng sang Inventory Manager qua API.

Tính năng không nằm trong luồng trên được xem là ưu tiên thấp hơn.

## Không làm trong phạm vi demo

- Chế độ ngoại tuyến.
- Nhiều máy POS chạy đồng thời.
- Nhiều chi nhánh.
- Khuyến mãi phức tạp như mua kèm, bậc thang, giờ vàng.
- Gửi thư điện tử hoặc tin nhắn.
- Xuất báo cáo thành tập tin Excel hoặc PDF.
- Cổng thanh toán khác ngoài VNPay môi trường thử nghiệm và tiền mặt.
- Tích hợp phần cứng thật cho máy quét hoặc máy in.

## Ranh giới hệ thống

| Thành phần | Nhóm xây dựng | Công nghệ | Địa chỉ chạy demo |
|---|---|---|---|
| Ứng dụng POS | Nhóm này | .NET 8 WPF | máy tính cá nhân |
| Cổng nhận callback VNPay | Nhóm này | ASP.NET Core | `localhost:5000` |
| Inventory Manager | Nhóm này | ASP.NET Core | `localhost:5145` |
| Cơ sở dữ liệu POS | Nhóm này | PostgreSQL 16 | `localhost:5433/smartpos` |
| Cơ sở dữ liệu Inventory | Nhóm này | PostgreSQL 16 | `localhost:5433/inventory_manager` |
| VNPay | Bên ngoài | Môi trường thử nghiệm | internet |

## Phân công module

| Thành viên | Module |
|---|---|
| Thành viên 1 | Cấu trúc solution, cơ sở dữ liệu, migration, Docker, CI, xác thực |
| Thành viên 2 | Ca làm việc, thanh toán tiền mặt, báo cáo |
| Thành viên 3 | Màn hình bán hàng, giỏ hàng, giả lập máy quét, tìm kiếm sản phẩm |
| Thành viên 4 | VNPay, callback, hóa đơn, giả lập in |
| Thành viên 5 | Khách hàng, điểm tích lũy, trả hàng, hoàn tiền |
| Thành viên 6 | Danh mục, giá, khuyến mãi, đồng bộ Inventory |

## Kế hoạch 15 tuần

### Đợt 1, tuần 1 đến 2

Tạo solution, Docker, cơ sở dữ liệu ban đầu, migration, cấu hình phụ thuộc, đăng nhập,
phân quyền và khung mở đóng ca.

### Đợt 2, tuần 3 đến 4

Giả lập máy quét, tìm sản phẩm, giỏ hàng, kiểm tra tồn kho, thanh toán tiền mặt và hóa đơn
cơ bản.

### Đợt 3, tuần 5 đến 6

VNPay, callback qua ngrok, khóa đơn hàng khi đang thanh toán, mở khóa khi thất bại hoặc hết
thời gian, xem trước hóa đơn.

### Đợt 4, tuần 7 đến 8

Quản lý danh mục, sản phẩm, giá, đồng bộ danh mục và tồn kho từ Inventory Manager.

### Đợt 5, tuần 9 đến 10

Khách hàng, điểm tích lũy, tạo yêu cầu trả hàng, duyệt trả hàng, gửi sự kiện nhập lại hàng.

### Đợt 6, tuần 11 đến 12

Khuyến mãi, phê duyệt khuyến mãi vượt ngưỡng, báo cáo ca và nhật ký thao tác.

### Đợt 7, tuần 13 đến 14

Kiểm thử luồng demo, sửa lỗi, tạo dữ liệu mẫu, chuẩn bị ngrok, làm mịn giao diện.

### Tuần 15

Demo và nộp bài.

## Ràng buộc cho tác nhân AI

- Không thêm kiến trúc phức tạp nếu không có lý do rõ.
- Luồng gọi phải là ViewModel đến Service đến Repository đến DbContext.
- ViewModel không chứa quy tắc nghiệp vụ.
- Service chứa quy tắc nghiệp vụ và trả về DTO.
- Repository làm việc với EF entity và DbContext.
- Tất cả thao tác có cơ sở dữ liệu, mạng hoặc tập tin phải dùng bất đồng bộ.
- Trong Service và Repository, dùng `ConfigureAwait(false)` sau mỗi `await`.
- Trong ViewModel, không dùng `ConfigureAwait(false)` vì cần quay lại luồng giao diện.
- Không cập nhật `ObservableCollection` từ luồng nền.
- Ưu tiên tính năng hoàn thành hơn mã nguồn quá đẹp nhưng chưa chạy được.

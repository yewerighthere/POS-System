# Tóm tắt dự án

## Tên dự án

SmartPOS là hệ thống bán hàng cho cửa hàng bán lẻ vừa và nhỏ.

## Loại dự án

Ứng dụng bán hàng trên máy tính Windows, có thêm cổng nhận callback thanh toán và hệ thống
quản lý tồn kho riêng.

## Mục tiêu chính

Xây dựng ứng dụng POS giúp cửa hàng quản lý luồng bán hàng căn bản: đăng nhập, mở ca,
chọn sản phẩm, giỏ hàng, khuyến mãi, thanh toán tiền mặt, thanh toán VNPay, hóa đơn, khách
hàng, điểm tích lũy, báo cáo ca, trả hàng và đồng bộ tồn kho với Inventory Manager.

## Người dùng

- Nhân viên: mở ca, đóng ca, bán hàng, nhận thanh toán, xem hóa đơn.
- Khách hàng: cung cấp thông tin thành viên, tích điểm, yêu cầu trả hàng.
- Quản lý: quản lý danh mục, giá, khuyến mãi, tài khoản, báo cáo, trả hàng và nhật ký.
- Quản trị: có quyền cao nhất, dùng khi cần cấu hình hoặc sửa dữ liệu demo.

## Vấn đề cần giải quyết

- Ca làm việc và dòng tiền mặt dễ sai nếu không có mở đóng ca rõ ràng.
- Tồn kho dễ lệch nếu POS không đồng bộ với Inventory Manager.
- Thanh toán VNPay cần khóa đơn hàng để tránh sửa giỏ hàng khi đang chờ callback.
- Thao tác nhạy cảm cần có nhật ký để truy vết.
- Trả hàng cần có luồng phê duyệt để tránh sai tiền và sai tồn kho.

## Tính năng chính

### Xác thực và ca làm việc

- Đăng nhập theo vai trò nhân viên, quản lý, quản trị.
- Đăng xuất và ghi lại phiên làm việc.
- Mở ca, nhập tiền đầu ca, đóng ca, tính tiền mặt dự kiến và chênh lệch.

Thông báo khóa tài khoản:

```text
Tài khoản đã bị khóa, vui lòng liên hệ quản lý
```

### Bán hàng và giỏ hàng

- Chọn sản phẩm bằng giao diện giả lập máy quét.
- Tìm sản phẩm theo tên, mã hàng hoặc mã vạch.
- Thêm, sửa số lượng, xóa sản phẩm trong giỏ.
- Kiểm tra tồn kho trước khi thêm vào giỏ.
- Tính lại tổng tiền sau mỗi thay đổi.

Thông báo hết hàng:

```text
Sản phẩm đã hết hàng
```

### Khuyến mãi

- Nhập mã khuyến mãi.
- Kiểm tra ngày hiệu lực, giá trị đơn hàng tối thiểu, sản phẩm áp dụng.
- Nếu giảm giá vượt ngưỡng, cần quản lý hoặc quản trị phê duyệt.
- Một đơn hàng chỉ áp dụng một khuyến mãi trong phạm vi demo.

### Thanh toán

- Tiền mặt: nhập tiền khách đưa, tính tiền thừa, xác nhận đơn hàng.
- VNPay: tạo đường dẫn thanh toán, hiện QR, nhận callback, cập nhật trạng thái.
- Khi thanh toán thành công, POS gửi sự kiện trừ tồn kho sang Inventory Manager.

Thông báo thanh toán thành công:

```text
Thanh toán thành công
```

Thông báo thanh toán thất bại:

```text
Thanh toán thất bại, vui lòng thử lại
```

### Hóa đơn và giả lập in

Nhóm không dùng máy in thật. Hệ thống hiển thị bản xem trước hóa đơn và cho phép bấm nút
giả lập in thành công.

Thông báo in thành công:

```text
In hóa đơn thành công
```

Thông báo lỗi in:

```text
Lỗi máy in, vui lòng kiểm tra cấu hình và thử lại
```

### Khách hàng và điểm tích lũy

- Đăng ký và cập nhật khách hàng.
- Số điện thoại và mã thành viên là duy nhất.
- Khách vãng lai được phép mua hàng.
- Điểm chỉ được cộng sau khi thanh toán thành công.
- Khi trả hàng được duyệt, điểm đã cộng từ đơn hàng đó sẽ bị trừ lại.

### Trả hàng và hoàn tiền

Tính năng này bắt buộc có trong demo cuối.

- Chỉ tạo trả hàng cho đơn đã thanh toán thành công.
- Bắt buộc nhập lý do.
- Số lượng trả không được vượt qua số lượng đã mua trừ số lượng đã trả trước đó.
- Quản lý hoặc quản trị duyệt thì mới hoàn tiền và gửi sự kiện nhập lại hàng.

### Danh mục, giá và sản phẩm

- Quản lý danh mục và sản phẩm.
- Không xóa cứng sản phẩm đã có giao dịch, chỉ chuyển sang ngừng hoạt động.
- Khi sửa giá, đơn hàng cũ vẫn giữ giá tại thời điểm bán.
- Sửa giá phải ghi nhật ký.

### Đồng bộ Inventory Manager

- POS gọi API Inventory Manager để lấy danh mục và tồn kho.
- POS gửi API trừ kho sau khi thanh toán thành công.
- POS gửi API nhập lại hàng sau khi trả hàng được duyệt.
- Mỗi lần đồng bộ phải ghi nhật ký đồng bộ.

### Báo cáo và nhật ký

- Báo cáo ca theo doanh thu, số đơn, tiền mặt, VNPay, tiền đầu ca, tiền cuối ca.
- Báo cáo doanh thu theo khoảng ngày.
- Nhật ký cho thao tác nhạy cảm như sửa giá, hủy đơn, duyệt hoàn tiền, đổi vai trò.

## Công nghệ chọn

- WPF .NET 8 cho ứng dụng Windows.
- CommunityToolkit.Mvvm cho MVVM.
- ModernWpfUI cho thành phần giao diện.
- ASP.NET Core cho callback VNPay và Inventory Manager.
- Entity Framework Core 8 cho truy cập cơ sở dữ liệu.
- PostgreSQL 16 chạy bằng Docker.
- BCrypt.Net-Next để băm mật khẩu.
- JWT để giữ thông tin đăng nhập trong phiên chạy ứng dụng.
- HttpClient để gọi API.
- Serilog để ghi nhật ký tập tin.
- xUnit và Moq để kiểm thử Service.

## Kiến trúc ưu tiên

```text
ViewModel -> Service -> Repository -> DbContext
```

Không bỏ qua tầng. ViewModel không gọi DbContext. Service không chứa mã giao diện.
Repository không chứa quy tắc nghiệp vụ.

## Phạm vi sản phẩm tối thiểu

Phạm vi đợt đầu tiên đến hết tuần 8:

- Đăng nhập, đăng xuất, phân quyền.
- Mở ca, đóng ca.
- Chọn sản phẩm bằng giao diện giả lập.
- Giỏ hàng.
- Thanh toán tiền mặt.
- Thanh toán VNPay.
- Hóa đơn và xem trước hóa đơn.
- Đồng bộ danh mục và tồn kho từ Inventory Manager.
- Quản lý danh mục, sản phẩm và giá.
- Báo cáo ca cơ bản.

Phạm vi demo cuối bổ sung:

- Khách hàng.
- Điểm tích lũy.
- Khuyến mãi.
- Trả hàng và hoàn tiền.
- Nhật ký thao tác.
- Tạo tài khoản mới khi giảng viên yêu cầu.

## Ngoài phạm vi

- Chế độ ngoại tuyến.
- Nhiều máy POS hoạt động đồng thời.
- Nhiều chi nhánh.
- Cổng thanh toán khác ngoài VNPay.
- Phần cứng thật cho máy quét và máy in.
- Gửi thư điện tử hoặc tin nhắn.
- Xuất Excel hoặc PDF.

Cập nhật lần cuối: tháng 6 năm 2026.

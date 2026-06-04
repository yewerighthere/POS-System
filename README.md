# SmartPOS

SmartPOS là ứng dụng bán hàng chạy trên máy tính Windows cho cửa hàng bán lẻ vừa và nhỏ.
Dự án được làm cho môn SWD392 tại FPT University, mục tiêu chính là hoàn thành luồng demo
rõ ràng, dễ chạy, dễ sửa lỗi và phù hợp với nhóm lập trình viên mới.

## Đọc trước khi viết mã

Thứ tự đọc khuyến nghị:

1. `docs/project-overview.md` để nắm mục tiêu, phạm vi demo và ràng buộc của dự án.
2. `docs/system-architecture.md` để nắm cấu trúc solution, các tầng và luồng dữ liệu.
3. `docs/feature-specifications.md` để nắm quy tắc nghiệp vụ của từng tính năng.
4. `docs/code-standards.md` để nắm quy ước viết mã.
5. `docs/ai-agent-guide.md` để nắm cách một tác nhân AI nên làm việc với mã nguồn.
6. `docs/implementation-status.md` để biết tính năng nào đã làm, đang làm hoặc chưa làm.

## Công nghệ sử dụng

- .NET 8
- WPF cho ứng dụng bán hàng trên Windows
- ASP.NET Core cho cổng nhận callback VNPay và Inventory Manager
- Entity Framework Core 8
- PostgreSQL 16
- Docker Desktop
- CommunityToolkit.Mvvm
- ModernWpfUI
- Serilog
- xUnit và Moq

## Cấu trúc dự kiến

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

## Cách thiết lập môi trường

### 1. Cài công cụ cần thiết

- Windows 10 hoặc Windows 11
- .NET SDK 8
- Docker Desktop
- PostgreSQL chạy bằng Docker
- Visual Studio 2022 hoặc Rider
- ngrok nếu cần demo thanh toán VNPay

### 2. Clone dự án

```powershell
git clone <duong-dan-repository>
cd POS_System
```

### 3. Chạy cơ sở dữ liệu

```powershell
docker compose up -d
```

Dự án nên có hai cơ sở dữ liệu riêng:

- `smartpos`: dữ liệu của hệ thống bán hàng.
- `inventory_manager`: dữ liệu của hệ thống quản lý tồn kho.

POS không đọc trực tiếp cơ sở dữ liệu của Inventory Manager. Inventory Manager cũng không
đọc trực tiếp cơ sở dữ liệu của POS. Hai hệ thống chỉ trao đổi với nhau qua HTTP API.

### 4. Cập nhật cơ sở dữ liệu

```powershell
dotnet ef database update --project src/SmartPOS.Data --startup-project src/SmartPOS.WPF
```

Nếu Inventory Manager có project migration riêng, chạy thêm lệnh cập nhật cho project đó.

### 5. Chạy dữ liệu mẫu

Dữ liệu mẫu cần có ít nhất:

- Một tài khoản quản trị.
- Một tài khoản quản lý.
- Một tài khoản nhân viên.
- Mười sản phẩm mẫu bên Inventory Manager.
- Tồn kho mẫu cho từng sản phẩm.
- Một mã khuyến mãi còn hạn.

Tài khoản demo đề xuất:

```text
quantri / 123456
quanly / 123456
nhanvien / 123456
```

Thông báo nếu đăng nhập sai nên là:

```text
Tên đăng nhập hoặc mật khẩu không đúng
```

### 6. Chạy các dịch vụ

Chạy Inventory Manager:

```powershell
dotnet run --project src/InventoryManager.Api
```

Địa chỉ dự kiến:

```text
http://localhost:5001
```

Chạy cổng nhận callback VNPay:

```powershell
dotnet run --project src/SmartPOS.CallbackApi
```

Địa chỉ dự kiến:

```text
http://localhost:5000
```

Chạy ứng dụng POS:

```powershell
dotnet run --project src/SmartPOS.WPF
```

### 7. Chạy ngrok cho VNPay

```powershell
ngrok http 5000
```

Lấy địa chỉ HTTPS của ngrok và cập nhật vào cấu hình callback VNPay.

### 8. Chạy kiểm thử

```powershell
dotnet test
```

## Luồng demo cuối

1. Quản lý đăng nhập và kiểm tra danh mục sản phẩm được đồng bộ từ Inventory Manager.
2. Nhân viên đăng nhập và mở ca.
3. Nhân viên nhập hoặc chọn mã sản phẩm bằng giao diện giả lập máy quét.
4. Nhân viên thêm sản phẩm vào giỏ hàng.
5. Nhân viên áp dụng mã khuyến mãi.
6. Nhân viên thanh toán VNPay bằng môi trường thử nghiệm.
7. Hệ thống nhận callback, cập nhật đơn hàng và hiển thị hóa đơn xem trước.
8. Nhân viên thanh toán tiền mặt cho đơn thứ hai.
9. Quản lý xem báo cáo ca.
10. Quản lý tạo và duyệt yêu cầu trả hàng, tồn kho được cập nhật qua API Inventory Manager.

## Lưu ý demo thiết bị

Nhóm không dùng phần cứng thật trong demo.

- Máy quét được thay bằng giao diện nhập hoặc chọn mã sản phẩm.
- Máy in được thay bằng màn hình xem trước hóa đơn và kết quả in giả lập.
- Thông báo in thành công nên là:

```text
In hóa đơn thành công
```

- Thông báo lỗi máy in nên là:

```text
Lỗi máy in: vui lòng kiểm tra cấu hình và thử lại
```

## Nguyên tắc quan trọng

- Ưu tiên hoàn thành luồng demo hơn thêm tính năng phụ.
- Không làm phức tạp kiến trúc khi chưa cần.
- ViewModel gọi Service.
- Service chứa quy tắc nghiệp vụ.
- Repository làm việc với cơ sở dữ liệu.
- Không để ViewModel truy cập trực tiếp DbContext.
- Tất cả thao tác có I/O phải dùng bất đồng bộ.
- Thông báo người dùng, nội dung in hóa đơn và nội dung nhật ký nghiệp vụ phải viết bằng tiếng Việt có dấu.

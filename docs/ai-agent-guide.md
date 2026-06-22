# Hướng dẫn cho tác nhân AI

File này dành cho tác nhân AI khi làm việc với dự án. Hãy đọc file này cùng
`system-architecture.md`, `feature-specifications.md`, `code-standards.md` và
`implementation-status.md` trước khi viết mã.

## Tư duy cần giữ

Dự án này ưu tiên demo chạy ổn định hơn kiến trúc phức tạp. Tất cả code nên rõ ràng, dễ đọc
và để thành viên mới sửa được.

Luồng chuẩn:

```text
View -> ViewModel -> Service -> Repository -> DbContext
```

Nếu đang viết quy tắc nghiệp vụ trong ViewModel, hãy chuyển sang Service. Nếu đang truy cập
DbContext ngoài Repository, hãy dừng lại và sửa thiết kế.

## Thứ tự đọc khi vào dự án

1. Đọc `README.md` để biết cách chạy dự án.
2. Đọc `docs/project-overview.md` để nắm mục tiêu và luồng demo.
3. Đọc `docs/system-architecture.md` để nắm cấu trúc solution.
4. Đọc `docs/feature-specifications.md` để nắm quy tắc nghiệp vụ.
5. Đọc `docs/code-standards.md` để nắm cách viết mã.
6. Đọc `docs/codebase-summary.md` để tìm nhanh nơi cần sửa.
7. Đọc `docs/implementation-status.md` để biết tính năng nào đã xong hoặc chưa xong.
8. Đọc `docs/development-task-list.md` để chọn task triển khai tiếp theo.
9. Đọc `docs/database-guide.md` nếu task có database, migration hoặc seed data.
10. Đọc `docs/team-workflow.md` nếu task cần phối hợp branch, PR hoặc cập nhật docs.

## Cách làm một tính năng

### Bước 1: Xác định phạm vi

Tìm tính năng trong `feature-specifications.md`. Nếu không thấy, dùng phạm vi demo trong
`project-overview.md` để quyết định mức ưu tiên.

### Bước 2: Kiểm tra trạng thái

Đọc `implementation-status.md` để biết tính năng đã có chưa. Nếu đã có một phần, sửa tiếp
theo hướng hiện tại, không tạo song song class mới trùng vai trò.

### Bước 3: Kiểm tra cơ sở dữ liệu

Nếu cần bảng hoặc cột mới cho POS, sửa entity trong `SmartPOS.Data/Entities`, sửa `AppDbContext`
nếu cần, sau đó tạo migration EF Core.

Nếu cần bảng hoặc cột mới cho Inventory Manager, sửa entity trong `src/InventoryManager.Api/Entities`,
sửa `InventoryDbContext`, sau đó tạo migration EF Core cho context `InventoryDbContext`.

Lệnh mẫu:

```powershell
dotnet ef migrations add TenMigration --project src/SmartPOS.Data --startup-project src/SmartPOS.WPF
```

### Bước 4: Sửa Repository

Thêm method vào interface trong `SmartPOS.Data/Repositories/Interfaces`, sau đó cài đặt trong
`SmartPOS.Data/Repositories/Implementations`.

Repository trả entity và dùng `ConfigureAwait(false)`.

### Bước 5: Sửa Service

Thêm method vào interface trong `SmartPOS.Services/Interfaces`. Cài đặt quy tắc nghiệp vụ
trong `SmartPOS.Services/Implementations`.

Service trả DTO, ghi nhật ký, ném `BusinessException` khi vi phạm quy tắc nghiệp vụ.

### Bước 6: Đăng ký DI

Thêm interface và class mới vào `App.xaml.cs` của WPF và `Program.cs` của API nếu cần.

### Bước 7: Sửa ViewModel

ViewModel inject Service qua constructor. Dùng `[ObservableProperty]` cho trạng thái và
`[RelayCommand]` cho hành động.

Thông báo hiển thị cho người dùng phải viết tiếng Việt có dấu.

### Bước 8: Sửa View

View chỉ bind dữ liệu và command. Không đưa quy tắc nghiệp vụ vào XAML code-behind nếu không
thật sự cần.

### Bước 9: Thêm kiểm thử

Ưu tiên kiểm thử Service cho:

- Luồng đúng.
- Quy tắc nghiệp vụ quan trọng.
- Lỗi dễ xảy ra trong demo.

## Cách đọc mã nguồn

1. Đọc enum trong `SmartPOS.Shared/Enums` để nắm ngôn ngữ nghiệp vụ.
2. Đọc entity trong `SmartPOS.Data/Entities` để nắm dữ liệu.
3. Đọc service interface trong `SmartPOS.Services/Interfaces` để nắm khả năng hệ thống.
4. Đọc ViewModel của màn hình liên quan để nắm cách UI gọi Service.

## Nguyên tắc thông báo và nhật ký

Tất cả thông báo cho người dùng, nội dung in hóa đơn và message trong nhật ký nghiệp vụ phải
viết tiếng Việt có dấu.

Ví dụ đúng:

```text
Thanh toán thành công
Sản phẩm đã hết hàng
Đã ghi nhận thanh toán tiền mặt
Không kết nối được Inventory Manager
```

Không viết thông báo bằng tiếng Anh.

Tên class, method, package, project và hằng số có thể giữ theo quy ước kỹ thuật của dự án.

## VNPay

Những lỗi dễ gặp:

- `vnp_Amount` phải bằng tổng tiền VND nhân 100.
- `vnp_TxnRef` dùng `orderId.ToString("N")`.
- Tham số phải sắp xếp theo tên trước khi ký.
- Không đưa `vnp_SecureHash` vào chuỗi ký lại.
- Chữ ký dùng HMAC-SHA512 với secret hash key.
- Mã `"00"` là thành công.
- Mỗi callback phải lưu payload vào `payments.vnpay_response`.

Thông báo liên quan VNPay:

```text
Đang chờ thanh toán VNPay
Thanh toán VNPay thành công
Thanh toán VNPay thất bại
Chữ ký VNPay không hợp lệ
```

## Inventory Manager

Inventory Manager có cơ sở dữ liệu riêng. POS chỉ gọi API, không truy cập cơ sở dữ liệu của
Inventory Manager.

Khi thanh toán thành công:

```text
POS -> POST /api/stock/deduct
```

Khi trả hàng được duyệt:

```text
POS -> POST /api/stock/restock
```

Nếu Inventory Manager lỗi, không làm hỏng đơn hàng đã thanh toán. Phải ghi nhật ký đồng bộ
thất bại để xử lý lại.

Thông báo:

```text
Không kết nối được Inventory Manager, vui lòng thử lại
Đồng bộ tồn kho thất bại, vui lòng kiểm tra nhật ký
```

## Giả lập thiết bị

Demo không dùng phần cứng thật.

- Máy quét: dùng ô nhập mã sản phẩm hoặc danh sách sản phẩm.
- Máy in: hiển thị bản xem trước hóa đơn và nút giả lập in.
- Vẫn giữ service `IDeviceService` để code có ranh giới rõ.

Thông báo:

```text
In hóa đơn thành công
Lỗi máy in, vui lòng kiểm tra cấu hình và thử lại
```

## Checklist trước demo

- Docker đang chạy.
- Cơ sở dữ liệu `smartpos` đã cập nhật migration.
- Cơ sở dữ liệu `inventory_manager` đã có dữ liệu mẫu.
- Inventory Manager chạy ở `localhost:5001`.
- Callback VNPay chạy ở `localhost:5000`.
- ngrok trỏ về cổng 5000.
- Cấu hình callback VNPay đúng địa chỉ HTTPS của ngrok.
- Có tài khoản quản trị, quản lý và nhân viên.
- Có sản phẩm mẫu, tồn kho mẫu và mã khuyến mãi.
- Luồng tiền mặt chạy được.
- Luồng VNPay chạy được.
- Luồng trả hàng và nhập lại hàng chạy được.
- Hóa đơn xem trước hiển thị đúng.

## Việc không nên làm

- Không quét toàn bộ mã nguồn nếu docs đã chỉ rõ nơi cần sửa.
- Không thêm thư viện mới nếu không cần.
- Không tạo abstraction mới khi chỉ cần class rõ ràng.
- Không sửa ngoài phạm vi tính năng được giao.
- Không đổi kiến trúc 3 tầng.
- Không viết thông báo tiếng Anh cho người dùng.

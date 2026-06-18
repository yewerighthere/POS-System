# Database Guide

File này hướng dẫn cách dùng database trong dự án SmartPOS: chạy PostgreSQL bằng Docker, cấu hình connection string, tạo migration, update database, seed data và xử lý lỗi thường gặp.

## Tổng Quan

Dự án có hai database theo thiết kế:

```text
smartpos
inventory_manager
```

Vai trò:

- `smartpos`: lưu dữ liệu của POS như user, ca làm việc, sản phẩm local, đơn hàng, thanh toán, hóa đơn, trả hàng và audit log.
- `inventory_manager`: lưu dữ liệu của hệ thống quản lý tồn kho riêng.

Theo kiến trúc, POS và Inventory Manager không đọc trực tiếp database của nhau. Hai hệ thống giao tiếp với nhau qua HTTP API.

## Cấu Hình Hiện Tại

PostgreSQL chạy bằng Docker và được map ra host port `5433` để tránh đụng PostgreSQL local ở port `5432`.

Thông tin kết nối hiện tại:

```text
Host: localhost
Port: 5433
Username: postgres
Password: 1
POS database: smartpos
Inventory database: inventory_manager
```

Connection string POS:

```text
Host=localhost;Port=5433;Database=smartpos;Username=postgres;Password=1
```

Connection string Inventory Manager:

```text
Host=localhost;Port=5433;Database=inventory_manager;Username=postgres;Password=1
```

Các file cấu hình chính:

```text
src/SmartPOS.WPF/appsettings.json
src/SmartPOS.CallbackApi/appsettings.json
src/InventoryManager.Api/appsettings.json
src/SmartPOS.Data/AppDbContextFactory.cs
```

## Docker Compose

File `docker-compose.yml` hiện dùng PostgreSQL 16:

```yaml
services:
  postgres:
    image: postgres:16
    ports:
      - "5433:5432"
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: "1"
      POSTGRES_DB: smartpos
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

Ý nghĩa:

- Bên trong container, PostgreSQL vẫn chạy ở port `5432`.
- Máy host truy cập qua port `5433`.
- Database mặc định được tạo là `smartpos`.
- Dữ liệu được lưu trong Docker volume `postgres_data`.

## Chạy PostgreSQL

Từ root repository:

```powershell
docker compose up -d
```

Kiểm tra container:

```powershell
docker compose ps
```

Xem log:

```powershell
docker compose logs --tail 50 postgres
```

Dừng container:

```powershell
docker compose down
```

Dừng container và xóa sạch dữ liệu local:

```powershell
docker compose down -v
```

Chỉ dùng `docker compose down -v` khi muốn reset database local.

## Kiểm Tra Database

Mở psql trong container:

```powershell
docker exec -it clone-postgres-1 psql -U postgres -d smartpos
```

Liệt kê bảng:

```sql
\dt
```

Thoát psql:

```sql
\q
```

Kiểm tra bằng một lệnh:

```powershell
docker exec clone-postgres-1 psql -U postgres -d smartpos -c "\dt"
```

## Tạo Database Inventory Manager

`docker-compose.yml` hiện tạo sẵn database `smartpos`. Nếu cần database `inventory_manager`, tạo thêm bằng psql:

```powershell
docker exec -it clone-postgres-1 psql -U postgres
```

Trong psql:

```sql
CREATE DATABASE inventory_manager;
```

Nếu database đã tồn tại, PostgreSQL sẽ báo lỗi. Khi đó có thể bỏ qua.

## EF Core CLI

Kiểm tra EF Core CLI:

```powershell
dotnet ef --version
```

Nếu chưa có:

```powershell
dotnet tool install --global dotnet-ef
```

Nếu muốn update:

```powershell
dotnet tool update --global dotnet-ef
```

## Migration POS

Migration của POS nằm trong project:

```text
src/SmartPOS.Data
```

Project này có `AppDbContextFactory` để EF Core tạo được `AppDbContext` khi chạy migration.

## Tạo Migration Đầu Tiên

Migration đầu tiên đã được tạo:

```text
src/SmartPOS.Data/Migrations/20260614115153_InitialCreate.cs
```

Migration bổ sung hiện có:

```text
src/SmartPOS.Data/Migrations/20260618183830_AddUserContactFields.cs
```

Migration `AddUserContactFields` thêm `Email` và `PhoneNumber` cho bảng `Users`.

Lệnh đã dùng:

```powershell
dotnet ef migrations add InitialCreate --project src/SmartPOS.Data --startup-project src/SmartPOS.Data --context AppDbContext --output-dir Migrations
```

## Tạo Migration Mới Khi Sửa Schema

Khi sửa entity hoặc `AppDbContext`, tạo migration mới:

```powershell
dotnet ef migrations add AddCustomerLoyaltyPoints --project src/SmartPOS.Data --startup-project src/SmartPOS.Data --context AppDbContext --output-dir Migrations
```

Quy tắc đặt tên migration:

- Dùng PascalCase.
- Tên mô tả thay đổi.
- Không đặt tên chung chung như `Update1`, `FixDb`, `NewMigration`.

Ví dụ tốt:

```text
AddPromotionCode
CreateAuditLogTable
UpdatePaymentFields
AddReturnResolvedAt
```

## Update Database

Apply migration vào database `smartpos`:

```powershell
dotnet ef database update --project src/SmartPOS.Data --startup-project src/SmartPOS.Data --context AppDbContext
```

Sau khi update, kiểm tra bảng:

```powershell
docker exec clone-postgres-1 psql -U postgres -d smartpos -c "\dt"
```

Hiện tại sau migration `InitialCreate`, database `smartpos` có các bảng chính:

```text
Users
UserSessions
Shifts
Categorys
Products
Promotions
Customers
Orders
OrderItems
Payments
Invoices
Returns
ReturnItems
InventorySyncLogs
Devices
DeviceLogs
AuditLogs
__EFMigrationsHistory
```

Lưu ý: tên `Categorys` là do skeleton DbSet hiện tại đặt tự động. Sau này nếu muốn đúng tiếng Anh là `Categories`, cần sửa DbSet/config và tạo migration mới.

## Xem Danh Sách Migration

```powershell
dotnet ef migrations list --project src/SmartPOS.Data --startup-project src/SmartPOS.Data --context AppDbContext
```

## Rollback Database

Rollback về migration cụ thể:

```powershell
dotnet ef database update <MigrationName> --project src/SmartPOS.Data --startup-project src/SmartPOS.Data --context AppDbContext
```

Rollback về trạng thái chưa apply migration nào:

```powershell
dotnet ef database update 0 --project src/SmartPOS.Data --startup-project src/SmartPOS.Data --context AppDbContext
```

Không rollback database chung của nhóm nếu chưa thống nhất.

## Xóa Migration

Chỉ xóa migration khi migration đó chưa được push hoặc chia sẻ cho nhóm.

Xóa migration mới nhất:

```powershell
dotnet ef migrations remove --project src/SmartPOS.Data --startup-project src/SmartPOS.Data --context AppDbContext
```

Nếu migration đã push lên branch chung, không xóa. Hãy tạo migration mới để sửa schema.

## Seed Data Demo

Dữ liệu demo tối thiểu cần có:

- 1 tài khoản Admin.
- 1 tài khoản Manager.
- 1 tài khoản Staff.
- Ít nhất 10 sản phẩm.
- Tồn kho cho từng sản phẩm.
- Ít nhất 1 mã khuyến mãi còn hiệu lực.

Tài khoản demo đề xuất:

```text
quantri / 123456
quanly / 123456
nhanvien / 123456
```

Seed data có thể làm bằng một trong hai cách:

- Seed trong code khi app start nếu database trống.
- Tạo SQL script riêng cho demo.

Với dự án lớp học, seed trong code thường dễ demo hơn, nhưng cần tránh tạo trùng dữ liệu.

## Quy Tắc Khi Sửa Entity

Khi sửa entity:

1. Sửa class trong `src/SmartPOS.Data/Entities`.
2. Sửa `AppDbContext` nếu cần quan hệ hoặc config đặc biệt.
3. Tạo migration mới.
4. Chạy update database.
5. Build solution.
6. Cập nhật docs nếu schema thay đổi.
7. Cập nhật `docs/implementation-status.md` nếu feature liên quan đổi trạng thái.

Không sửa trực tiếp migration cũ nếu migration đó đã được chia sẻ.

## Inventory Manager Database

Inventory Manager đã tách khỏi `SmartPOS.Data` và dùng `InventoryDbContext` riêng trong project `src/InventoryManager.Api`.

Các file nền đã tạo:

```text
InventoryDbContext
InventoryProduct
InventoryCategory
StockItem
StockTransaction
```

Migration đầu tiên của Inventory Manager đã được tạo:

```text
src/InventoryManager.Api/Migrations/20260614152546_InitialInventoryCreate.cs
```

Lệnh đã dùng:

```powershell
dotnet ef migrations add InitialInventoryCreate --project src/InventoryManager.Api --startup-project src/InventoryManager.Api --context InventoryDbContext --output-dir Migrations
```

Update database:

```powershell
dotnet ef database update --project src/InventoryManager.Api --startup-project src/InventoryManager.Api --context InventoryDbContext
```

## Lỗi Thường Gặp

### Docker engine chưa chạy

Lỗi thường gặp:

```text
permission denied while trying to connect to the docker API
```

Hoặc:

```text
open //./pipe/dockerDesktopLinuxEngine: The system cannot find the file specified
```

Cách xử lý:

- Mở Docker Desktop.
- Đợi Docker báo engine đang chạy.
- Chạy lại `docker compose ps`.

### PostgreSQL không start

Kiểm tra log:

```powershell
docker compose logs --tail 50 postgres
```

Nếu log báo thiếu password, kiểm tra `POSTGRES_PASSWORD` trong `docker-compose.yml`.

### EF update nhầm database local

Nếu máy có PostgreSQL local ở port `5432`, EF có thể update nhầm database local nếu connection string dùng `Port=5432`.

Trong dự án này, Docker PostgreSQL đang dùng host port `5433`, nên connection string phải dùng:

```text
Port=5433
```

### `dotnet ef` không tìm thấy DbContext

Đảm bảo command có đủ:

```powershell
--project src/SmartPOS.Data
--startup-project src/SmartPOS.Data
--context AppDbContext
```

Và đảm bảo file này tồn tại:

```text
src/SmartPOS.Data/AppDbContextFactory.cs
```

### Muốn reset database local

Nếu chỉ là dữ liệu local và có thể xóa:

```powershell
docker compose down -v
docker compose up -d
dotnet ef database update --project src/SmartPOS.Data --startup-project src/SmartPOS.Data --context AppDbContext
```

## Checklist Trước Khi Demo

- [ ] Docker Desktop đang chạy.
- [ ] PostgreSQL container đang `Up`.
- [ ] Container map port `5433->5432`.
- [ ] Database `smartpos` tồn tại.
- [ ] Migration đã được apply.
- [ ] Bảng đã được tạo trong `smartpos`.
- [ ] Seed data đã có.
- [ ] Login được bằng tài khoản demo.
- [ ] Inventory API chạy được.
- [ ] Callback API chạy được.
- [ ] WPF app chạy được.

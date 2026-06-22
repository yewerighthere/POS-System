## SmartPOS - SWD392 FPT University

SmartPOS là solution .NET 8 cho hệ thống bán hàng POS demo. Hiện tại project đang ở dạng skeleton: đã có cấu trúc layer, DTO, entity, repository, service, WPF shell, API và test stub. Business logic chưa được implement.

## Cấu Trúc Project

```text
SmartPOS.sln
src/
  SmartPOS.Shared/
  SmartPOS.Data/
  SmartPOS.Services/
  SmartPOS.WPF/
  SmartPOS.CallbackApi/
  InventoryManager.Api/
tests/
  SmartPOS.Tests/
docs/
docker-compose.yml
```

Vai trò từng project:

- `SmartPOS.Shared`: DTO, enum, constant và exception dùng chung.
- `SmartPOS.Data`: EF Core `AppDbContext`, entity, repository interface và repository implementation.
- `SmartPOS.Services`: service interface và service implementation.
- `SmartPOS.WPF`: ứng dụng desktop POS, ViewModel, View và cấu hình DI.
- `SmartPOS.CallbackApi`: API nhận callback thanh toán VNPay.
- `InventoryManager.Api`: API quản lý tồn kho.
- `SmartPOS.Tests`: test stub cho service layer.

## Tài Liệu Quan Trọng

- `docs/project-overview.md`: mục tiêu dự án, phạm vi demo và kế hoạch 15 tuần.
- `docs/system-architecture.md`: kiến trúc solution, project dependency và database schema.
- `docs/feature-specifications.md`: đặc tả chi tiết từng tính năng.
- `docs/code-standards.md`: quy chuẩn code, DI, ViewModel, Service và Repository.
- `docs/development-task-list.md`: checklist task cần làm từ skeleton đến demo cuối.
- `docs/implementation-status.md`: trạng thái hiện tại của feature/project.
- `docs/database-guide.md`: hướng dẫn Docker PostgreSQL, migration, update database và seed data.
- `docs/team-workflow.md`: cách chia task, branch, commit, review và merge trong nhóm.

## Yêu Cầu Môi Trường

Cài trước khi chạy project:

- .NET 8 SDK
- Docker Desktop
- EF Core CLI
- Visual Studio 2022 hoặc Rider nếu muốn chạy/debug WPF thuận tiện hơn

Kiểm tra version:

```powershell
dotnet --version
docker --version
dotnet ef --version
```

Nếu chưa có `dotnet ef`:

```powershell
dotnet tool install --global dotnet-ef
```

Nếu đã có nhưng muốn update:

```powershell
dotnet tool update --global dotnet-ef
```

## Setup Sau Khi Clone

Clone repository và đi vào thư mục source:

```powershell
git clone <repository-url>
cd <repository-folder>
```

Restore package và build solution:

```powershell
dotnet restore SmartPOS.sln
dotnet build SmartPOS.sln
```

Chạy PostgreSQL bằng Docker:

```powershell
docker compose up -d
```

Kiểm tra container:

```powershell
docker compose ps
```

File cấu hình connection string:

- `src/SmartPOS.WPF/appsettings.json`
- `src/SmartPOS.CallbackApi/appsettings.json`
- `src/InventoryManager.Api/appsettings.json`

Database mặc định trong config:

```text
smartpos
inventory_manager
```

Lưu ý: `docker-compose.yml` hiện tại chỉ khai báo container PostgreSQL tối thiểu. Nếu database/user chưa được tạo tự động, cần cập nhật thêm biến môi trường cho PostgreSQL hoặc tạo database thủ công theo connection string trong `appsettings.json`.

## Migration Và Update Database

Migration của POS nằm trong project `SmartPOS.Data`.

### Tạo Migration Đầu Tiên

Chạy từ root repository:

```powershell
dotnet ef migrations add InitialCreate --project src/SmartPOS.Data --startup-project src/SmartPOS.WPF --context AppDbContext --output-dir Migrations
```

Sau khi chạy xong, migration sẽ nằm tại:

```text
src/SmartPOS.Data/Migrations/
```

### Tạo Migration Khi Entity Thay Đổi

Khi sửa entity hoặc `AppDbContext`, tạo migration mới với tên mô tả thay đổi:

```powershell
dotnet ef migrations add AddPromotionCode --project src/SmartPOS.Data --startup-project src/SmartPOS.WPF --context AppDbContext --output-dir Migrations
```

Ví dụ tên migration:

```text
AddPromotionCode
AddCustomerLoyaltyPoints
UpdatePaymentFields
CreateAuditLogTable
```

### Update Database POS

Apply migration vào database `smartpos`:

```powershell
dotnet ef database update --project src/SmartPOS.Data --startup-project src/SmartPOS.WPF --context AppDbContext
```

### Xem Danh Sách Migration

```powershell
dotnet ef migrations list --project src/SmartPOS.Data --startup-project src/SmartPOS.WPF --context AppDbContext
```

### Rollback Migration

Rollback database về migration cụ thể:

```powershell
dotnet ef database update <MigrationName> --project src/SmartPOS.Data --startup-project src/SmartPOS.WPF --context AppDbContext
```

Rollback về trạng thái chưa có migration nào:

```powershell
dotnet ef database update 0 --project src/SmartPOS.Data --startup-project src/SmartPOS.WPF --context AppDbContext
```

Chỉ rollback hoặc xoá migration khi chắc chắn migration đó chưa được chia sẻ cho cả nhóm.

### Inventory Manager Database

Trong skeleton hiện tại, `InventoryManager.Api` vẫn reference `SmartPOS.Data`. Khi team tách riêng DbContext cho Inventory Manager, migration của Inventory Manager nên dùng API project làm startup project, ví dụ:

```powershell
dotnet ef migrations add InitialInventoryCreate --project src/InventoryManager.Api --startup-project src/InventoryManager.Api --context <InventoryDbContext> --output-dir Migrations
dotnet ef database update --project src/InventoryManager.Api --startup-project src/InventoryManager.Api --context <InventoryDbContext>
```

Hiện tại, nếu chỉ dùng `AppDbContext` scaffold sẵn, dùng lệnh migration của POS ở trên.

## Chạy Project

Chạy WPF app:

```powershell
dotnet run --project src/SmartPOS.WPF
```

Chạy Callback API:

```powershell
dotnet run --project src/SmartPOS.CallbackApi
```

Chạy Inventory Manager API:

```powershell
dotnet run --project src/InventoryManager.Api
```

Endpoint chính:

```text
POST /api/vnpay/callback
GET  /api/sync/catalog
GET  /api/sync/stock
POST /api/stock/deduct
POST /api/stock/restock
```

## Chạy Test

Chạy toàn bộ test:

```powershell
dotnet test SmartPOS.sln
```

Chạy riêng project test:

```powershell
dotnet test tests/SmartPOS.Tests/SmartPOS.Tests.csproj
```

Hiện tại test class chỉ là placeholder. Khi implement service logic, thêm test thật vào `tests/SmartPOS.Tests`.

## Lệnh Hay Dùng

Clean solution:

```powershell
dotnet clean SmartPOS.sln
```

Restore và build lại:

```powershell
dotnet restore SmartPOS.sln
dotnet build SmartPOS.sln
```

Dừng PostgreSQL:

```powershell
docker compose down
```

Dừng PostgreSQL và xoá volume:

```powershell
docker compose down -v
```

## Quy Ước Khi Dev

- Đọc `docs/system-architecture.md` trước khi thêm project, folder hoặc class mới.
- Đọc `docs/code-standards.md` trước khi implement logic.
- Flow bắt buộc: `View -> ViewModel -> Service -> Repository -> DbContext`.
- ViewModel không gọi trực tiếp repository hoặc DbContext.
- Service không gọi trực tiếp DbContext.
- Service trả DTO, không trả EF entity cho UI.
- Repository trả entity, không trả DTO.
- Method async phải kết thúc bằng `Async`.
- Skeleton hiện tại cố ý để method body là `throw new NotImplementedException();`.
- ViewModel command hiện tại chỉ để `// TODO` cho đến khi implement UI flow thật.

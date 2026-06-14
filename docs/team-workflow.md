# Team Workflow

File này mô tả cách nhóm làm việc với SmartPOS: chia task, đặt branch, commit, review, migration và cập nhật docs. Mục tiêu là giữ workflow đơn giản, phù hợp dự án lớp học và tránh lỗi kiểu mỗi người làm một kiểu.

## Nguyên Tắc Chung

- Ưu tiên demo chạy ổn định hơn code quá phức tạp.
- Mỗi người nên làm theo task trong `docs/development-task-list.md`.
- Trước khi code, đọc feature tương ứng trong `docs/feature-specifications.md`.
- Sau khi hoàn thành task, cập nhật `docs/implementation-status.md`.
- Nếu thay đổi database, đọc `docs/database-guide.md`.
- Không tự ý thêm package hoặc kiến trúc mới nếu chưa thống nhất.

## Thứ Tự Đọc Docs Khi Nhận Task

1. `README.md` để biết cách setup và chạy project.
2. `docs/project-overview.md` để hiểu mục tiêu demo.
3. `docs/development-task-list.md` để biết task cần làm.
4. `docs/implementation-status.md` để biết trạng thái hiện tại.
5. `docs/feature-specifications.md` để hiểu business rule.
6. `docs/code-standards.md` để viết code đúng convention.
7. `docs/database-guide.md` nếu task đụng tới database/migration.

## Chia Việc Theo Module

Gợi ý chia việc cho nhóm 6 người:

| Thành viên | Module chính |
|---|---|
| Thành viên 1 | Database, migration, Docker, seed data, Auth |
| Thành viên 2 | Shift, Cash Payment, Report |
| Thành viên 3 | Sales screen, Cart, Product search, Scanner emulator |
| Thành viên 4 | VNPay, Callback API, Invoice, Fake print |
| Thành viên 5 | Customer, Loyalty points, Return/Refund |
| Thành viên 6 | Catalog, Product price, Promotion, Inventory Sync |

Một người có thể hỗ trợ module khác, nhưng cần báo trước để tránh sửa cùng file.

## Quy Trình Làm Một Task

1. Chọn task trong `docs/development-task-list.md`.
2. Đánh dấu task đang làm bằng `[~]` nếu nhóm muốn tracking trực tiếp trong docs.
3. Tạo branch riêng.
4. Implement code theo đúng layer.
5. Chạy build/test liên quan.
6. Cập nhật docs nếu có thay đổi.
7. Tạo pull request hoặc merge request.
8. Nhờ ít nhất một thành viên review.
9. Merge sau khi build ổn và không conflict.
10. Cập nhật `docs/implementation-status.md`.

## Quy Tắc Branch

Format branch đề xuất:

```text
feature/<task-id>-<short-name>
fix/<task-id>-<short-name>
docs/<short-name>
```

Ví dụ:

```text
feature/task-0105-auth-login
feature/task-0202-open-shift
feature/task-0503-cash-payment
fix/payment-status-update
docs/database-guide
```

Không nên commit trực tiếp lên branch chính nếu nhóm đang làm song song.

## Quy Tắc Commit

Format commit đơn giản:

```text
type: short message
```

Type đề xuất:

```text
feat
fix
docs
test
refactor
chore
```

Ví dụ:

```text
feat: implement auth login
fix: prevent duplicate open shift
docs: add database guide
test: add cash payment tests
chore: update dependency references
```

Commit nên nhỏ vừa đủ. Không gom quá nhiều feature không liên quan vào một commit.

## Quy Tắc Pull Request

PR nên có:

- Task ID liên quan.
- Tóm tắt đã làm gì.
- Cách test.
- Có migration hay không.
- Có thay đổi docs hay không.

Mẫu PR:

```text
## Task
TASK-0105 Auth login

## Changes
- Implement AuthService.LoginAsync
- Add user lookup by username
- Update LoginViewModel command

## Test
- dotnet build SmartPOS.sln
- dotnet test tests/SmartPOS.Tests/SmartPOS.Tests.csproj

## Notes
- No migration
```

## Quy Tắc Review

Người review nên kiểm tra:

- Code có đúng flow `View -> ViewModel -> Service -> Repository -> DbContext` không.
- ViewModel có chứa business logic không.
- Service có gọi trực tiếp DbContext không.
- Repository có chứa business rule không.
- Async method có kết thúc bằng `Async` không.
- Có test cho business rule quan trọng không.
- Có cập nhật docs khi schema/API/config thay đổi không.

Không cần review quá nặng về style nhỏ nếu chưa ảnh hưởng demo.

## Quy Tắc Database Và Migration

Khi task thay đổi entity/schema:

1. Báo cho nhóm biết trước khi tạo migration.
2. Sửa entity và `AppDbContext`.
3. Tạo migration mới.
4. Chạy update database local.
5. Build solution.
6. Commit migration cùng code liên quan.

Không làm:

- Không sửa migration cũ đã push.
- Không xóa migration của người khác.
- Không tự đổi connection string chung mà không báo nhóm.
- Không dùng chung một migration cho nhiều thay đổi không liên quan.

Nếu migration conflict:

1. Pull code mới nhất.
2. Kiểm tra migration nào đã merge.
3. Nếu migration của mình chưa push, có thể remove và tạo lại.
4. Nếu migration đã push, tạo migration mới để sửa.

## Quy Tắc Package

Chỉ dùng package đã thống nhất trong docs/codebase. Nếu cần package mới:

1. Ghi rõ lý do cần package.
2. Báo nhóm trước.
3. Cập nhật `.csproj`.
4. Cập nhật docs nếu package ảnh hưởng setup hoặc architecture.

Không thêm package chỉ vì tiện nếu có thể làm đơn giản bằng .NET hoặc package hiện có.

## Quy Tắc Cập Nhật Docs

Cập nhật docs khi:

- Thêm/sửa database schema.
- Thêm/sửa API endpoint.
- Đổi business rule.
- Đổi setup command.
- Đổi package.
- Đổi flow demo.
- Hoàn thành hoặc chuyển trạng thái feature.

File cần cập nhật thường gặp:

- `docs/implementation-status.md`: khi feature đổi trạng thái.
- `docs/development-task-list.md`: khi task hoàn thành hoặc phát sinh task mới.
- `docs/database-guide.md`: khi migration/database setup thay đổi.
- `README.md`: khi cách clone/setup/run thay đổi.
- `docs/feature-specifications.md`: khi business rule thay đổi.

## Quy Tắc Test

Tối thiểu trước khi đưa code lên:

```powershell
dotnet build SmartPOS.sln
```

Nếu task có service logic:

```powershell
dotnet test tests/SmartPOS.Tests/SmartPOS.Tests.csproj
```

Nếu task có UI:

- Chạy WPF app.
- Mở màn hình liên quan.
- Kiểm tra command chính không crash.
- Kiểm tra thông báo lỗi cơ bản.

Nếu task có API:

- Chạy API project.
- Test endpoint bằng Swagger, Postman hoặc file `.http`.

## Quy Tắc Merge

Chỉ merge khi:

- Build pass.
- Không còn conflict.
- Reviewer đã đồng ý.
- Migration không phá database local của nhóm.
- Docs đã cập nhật nếu cần.

Nếu gần ngày demo, ưu tiên merge các fix nhỏ, ít rủi ro. Không refactor lớn vào sát ngày demo.

## Checklist Trước Mỗi Buổi Làm Nhóm

- [ ] Pull code mới nhất.
- [ ] Chạy `dotnet restore SmartPOS.sln`.
- [ ] Chạy `dotnet build SmartPOS.sln`.
- [ ] Chạy Docker PostgreSQL nếu task cần database.
- [ ] Kiểm tra `docs/development-task-list.md`.
- [ ] Chọn task rõ ràng.
- [ ] Báo với nhóm file/module mình sẽ sửa.

## Checklist Trước Demo

- [ ] PostgreSQL chạy bằng Docker.
- [ ] Migration đã apply.
- [ ] Seed data đã có.
- [ ] Login được 3 tài khoản demo.
- [ ] InventoryManager.Api chạy.
- [ ] SmartPOS.CallbackApi chạy.
- [ ] SmartPOS.WPF chạy.
- [ ] Luồng bán hàng tiền mặt chạy.
- [ ] Luồng VNPay demo chạy hoặc có fallback.
- [ ] Hóa đơn preview được.
- [ ] Return/refund demo được.
- [ ] Báo cáo ca xem được.
- [ ] Không còn lỗi nghiêm trọng trong luồng demo chính.

## Khi Có Lỗi Khó

Ghi lại ngắn gọn:

```text
Lỗi:
Đang làm task:
File đã sửa:
Lệnh đã chạy:
Kết quả lỗi:
Hướng nghi ngờ:
```

Sau đó nhờ người khác xem. Đừng âm thầm sửa nhiều hướng cùng lúc vì dễ làm code rối hơn.


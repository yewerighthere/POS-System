# Đặc tả tính năng

File này là nguồn tham chiếu chính cho business requirements và business rules của từng
tính năng. Khi triển khai một tính năng, hãy đọc phần tương ứng trước khi sửa mã.

Mỗi tính năng dưới đây giữ cùng cấu trúc:

- Mục tiêu
- Business Requirements
- Business Rules
- Thông báo hiển thị nếu cần
- Chữ ký Service định hướng implementation

Các quyết định đã chốt:

- POS và Inventory Manager dùng cơ sở dữ liệu riêng, chỉ giao tiếp qua HTTP API.
- Demo không dùng thiết bị vật lý; dùng giao diện giả lập hoặc fallback UI.
- Return/Refund là bắt buộc trong demo cuối.
- Promotion phải có `code`.
- VNPay dùng `GetOrderPaymentStatusAsync` làm tên method duy nhất để kiểm tra trạng thái thanh toán.

---

## F-01 Xác thực

### Mục tiêu

Cho phép Staff, Manager và Admin đăng nhập bằng tài khoản hợp lệ, xác định vai trò sau khi
đăng nhập, ghi nhận phiên đăng nhập và giới hạn quyền truy cập theo vai trò.

### Business Requirements

- Hệ thống cho phép Staff, Manager và Admin đăng nhập bằng thông tin hợp lệ.
- Hệ thống xác định vai trò người dùng sau khi đăng nhập thành công.
- Hệ thống tạo và lưu phiên đăng nhập cho mỗi lần đăng nhập thành công.
- Hệ thống cho phép người dùng đã xác thực đăng xuất.
- Hệ thống giới hạn chức năng POS theo vai trò và quyền của người dùng.
- Hệ thống cần có dữ liệu seed cho tài khoản demo.
- Hệ thống nên có chức năng tạo tài khoản mới để phòng trường hợp giảng viên yêu cầu trong demo.

### Business Rules

- Chỉ người dùng có trạng thái `Active` mới được đăng nhập.
- Người dùng `Locked` hoặc `Inactive` không được đăng nhập, mở ca hoặc thực hiện bán hàng.
- Mỗi lần đăng nhập thành công phải được ghi vào `user_sessions`.
- Người dùng phải được xác thực trước khi truy cập màn hình được bảo vệ.
- Hệ thống phải kiểm tra username, password, role và account status trước khi cấp quyền truy cập.
- Nếu đã có phiên đăng nhập cũ trên cùng máy, phiên cũ phải được đóng trước khi tạo phiên mới.
- Chỉ Manager hoặc Admin được tạo tài khoản mới.

### Thông báo hiển thị

```text
Tên đăng nhập hoặc mật khẩu không đúng
Tài khoản đã bị khóa, vui lòng liên hệ quản lý
Tài khoản đã ngừng hoạt động
Bạn không có quyền truy cập chức năng này
```

### Chữ ký Service

```csharp
Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
Task LogoutAsync(Guid userId);
Task<UserDto> CreateUserAsync(CreateUserDto request, Guid createdByUserId);
Task<bool> ValidateTokenAsync(string token);
```

---

## F-02 Quản lý ca

### Mục tiêu

Quản lý vòng đời ca làm việc của nhân viên, bao gồm mở ca, đóng ca, ghi nhận tiền đầu ca,
tính tiền mặt dự kiến và tạo báo cáo tóm tắt ca.

### Business Requirements

- Hệ thống cho phép Staff, Manager hoặc Admin mở ca làm việc.
- Hệ thống yêu cầu nhập tiền đầu ca trước khi bắt đầu bán hàng.
- Hệ thống chỉ cho phép truy cập màn hình bán hàng sau khi ca đã được mở.
- Hệ thống ghi nhận thời gian mở ca, người mở ca, tiền đầu ca và thông tin thiết bị giả lập nếu có.
- Hệ thống cho phép Staff, Manager hoặc Admin đóng ca đang mở.
- Hệ thống yêu cầu nhập tiền mặt thực tế khi đóng ca.
- Hệ thống tính tiền mặt dự kiến dựa trên doanh thu tiền mặt trong ca.
- Hệ thống tính chênh lệch tiền mặt.
- Hệ thống tạo tóm tắt ca sau khi đóng ca.

### Business Rules

- Mỗi người dùng chỉ được có một ca đang mở tại một thời điểm.
- Staff không được tạo đơn bán hàng nếu chưa có ca đang mở.
- Tiền đầu ca phải lớn hơn hoặc bằng 0.
- Người dùng bị khóa không được mở ca.
- Mỗi ca đang mở phải liên kết với người dùng đang đăng nhập.
- Chỉ ca có trạng thái `Open` mới được đóng.
- Phải nhập tiền cuối ca trước khi đóng ca.
- Công thức chênh lệch: `cash_difference = actual_cash - expected_cash`.
- Sau khi ca đóng, không được thêm đơn bán hàng mới vào ca đó.
- Tóm tắt ca phải gồm tiền đầu ca, tiền mặt dự kiến, tiền mặt thực tế, tổng doanh thu và chênh lệch tiền mặt.
- Nếu ứng dụng tắt giữa ca, lần đăng nhập sau phải tự tìm ca đang mở và cho phép tiếp tục.

### Thông báo hiển thị

```text
Bạn đang có ca làm việc đang mở
Vui lòng mở ca trước khi bán hàng
Tiền đầu ca không được âm
Vui lòng nhập tiền cuối ca trước khi đóng ca
Ca làm việc đã được đóng
```

### Chữ ký Service

```csharp
Task<ShiftDto> OpenShiftAsync(Guid userId, decimal openingCash);
Task<ShiftSummaryDto> CloseShiftAsync(Guid shiftId, decimal closingCash);
Task<ShiftDto?> GetOpenShiftAsync(Guid userId);
Task<ShiftSummaryDto> GetShiftSummaryAsync(Guid shiftId);
```

---

## F-03 Bán hàng: chọn sản phẩm, tìm kiếm, giỏ hàng, tính tiền

### Mục tiêu

Cho phép Staff chọn sản phẩm bằng giao diện giả lập máy quét hoặc tìm kiếm thủ công, quản lý
giỏ hàng, kiểm tra tồn kho và tính tổng tiền trước khi thanh toán.

### Business Requirements

- Hệ thống cho phép Staff nhập hoặc chọn mã sản phẩm bằng giao diện giả lập scanner.
- Hệ thống nhận diện sản phẩm theo barcode, QR code, SKU hoặc Data Matrix code nếu có.
- Hệ thống tự động thêm sản phẩm hợp lệ vào giỏ hàng.
- Hệ thống cảnh báo khi mã sản phẩm không hợp lệ.
- Hệ thống cho phép Staff tìm sản phẩm thủ công.
- Hệ thống hỗ trợ tìm kiếm theo tên sản phẩm, SKU, barcode hoặc QR code.
- Hệ thống chỉ hiển thị sản phẩm đang hoạt động trong kết quả tìm kiếm.
- Hệ thống cho phép Staff chọn sản phẩm từ kết quả tìm kiếm và thêm vào giỏ.
- Hệ thống cho phép Staff thêm sản phẩm, cập nhật số lượng và xóa sản phẩm khỏi giỏ.
- Hệ thống tự tính lại tạm tính, giảm giá, thuế và tổng tiền khi giỏ hàng thay đổi.
- Hệ thống hiển thị tên sản phẩm, đơn giá, số lượng, tạm tính, giảm giá và thành tiền trong giỏ.

### Business Rules

- Scanner trong demo là giao diện giả lập một chiều, không yêu cầu phần cứng thật.
- Chỉ sản phẩm `Active` mới được thêm vào giỏ hàng.
- Sản phẩm có `LocalStockQuantity = 0` không được thêm vào giỏ hàng.
- Nếu mã quét không khớp sản phẩm nào, hệ thống phải hiển thị cảnh báo.
- Nếu sản phẩm inactive, hệ thống phải chặn thêm vào giỏ.
- Nếu sản phẩm hết hàng, hệ thống phải chặn thêm vào giỏ.
- Kết quả tìm kiếm không được bao gồm sản phẩm inactive.
- Phải kiểm tra tồn kho trước khi thêm sản phẩm vào giỏ.
- Số lượng trong giỏ không được vượt `LocalStockQuantity`.
- Sản phẩm inactive không được checkout.
- Nếu giá sản phẩm thay đổi trước checkout, hệ thống phải cảnh báo Staff.
- Mọi thay đổi trong giỏ hàng phải kích hoạt tính lại tổng tiền.
- Giỏ hàng trống không được đi đến thanh toán.
- Đơn hàng bị khóa không được sửa giỏ hàng.
- Tổng tiền phải dùng đơn giá tại thời điểm mua.
- Đơn hàng cũ không bị ảnh hưởng bởi thay đổi giá sau này.
- Thuế phải tính theo cấu hình thuế hiện hành.
- Quy tắc làm tròn phải theo cấu hình cửa hàng.
- Tổng tiền không được âm.

### Công thức tính tiền

```text
tạm_tính_dòng = đơn_giá * số_lượng - giảm_giá_dòng
tạm_tính_đơn = tổng tạm_tính_dòng
giảm_giá_đơn = giảm giá cấp đơn hàng
tiền_tính_thuế = tạm_tính_đơn - giảm_giá_đơn
tiền_thuế = tiền_tính_thuế * thuế_suất
tổng_tiền = tiền_tính_thuế + tiền_thuế
```

Mặc định làm tròn bằng:

```csharp
Math.Round(value, 2, MidpointRounding.AwayFromZero)
```

### Thông báo hiển thị

```text
Không tìm thấy sản phẩm, vui lòng kiểm tra mã hoặc đồng bộ danh mục
Sản phẩm đã ngừng kinh doanh
Sản phẩm đã hết hàng
Số lượng vượt quá tồn kho hiện có
Giỏ hàng đang trống, vui lòng thêm sản phẩm trước khi thanh toán
Giá sản phẩm đã thay đổi, vui lòng kiểm tra lại trước khi thanh toán
Đơn hàng đang bị khóa, không thể sửa giỏ hàng
```

### Chữ ký Service

```csharp
Task<ProductDto?> FindByBarcodeAsync(string barcode);
Task<List<ProductSearchResultDto>> SearchProductsAsync(string query);
CartSummaryDto AddItem(Guid productId, int quantity, CartSummaryDto currentCart);
CartSummaryDto UpdateItemQuantity(Guid productId, int newQuantity, CartSummaryDto currentCart);
CartSummaryDto RemoveItem(Guid productId, CartSummaryDto currentCart);
CartSummaryDto RecalculateTotals(CartSummaryDto cart);
```

---

## F-04 Khuyến mãi và giảm giá

### Mục tiêu

Cho phép Staff áp dụng mã khuyến mãi hợp lệ vào đơn hàng, đồng thời yêu cầu Manager hoặc
Admin phê duyệt các khoản giảm giá vượt ngưỡng cấu hình.

### Business Requirements

- Hệ thống cho phép Staff nhập và áp dụng khuyến mãi hợp lệ vào đơn hàng.
- Hệ thống cho phép Manager hoặc Admin phê duyệt giảm giá vượt ngưỡng cấu hình.
- Hệ thống kiểm tra điều kiện khuyến mãi trước khi áp dụng.
- Hệ thống cập nhật tổng tiền giỏ hàng sau khi áp dụng khuyến mãi.
- Hệ thống hỗ trợ khuyến mãi theo đơn hàng hoặc theo sản phẩm.

### Business Rules

- Bảng `promotions` bắt buộc có cột `code`.
- Chỉ khuyến mãi `Active` mới được áp dụng.
- Khuyến mãi phải nằm trong khoảng `start_date` và `end_date`.
- Điều kiện khuyến mãi phải khớp với sản phẩm hoặc đơn hàng được chọn.
- Nếu khuyến mãi có `min_order_amount`, đơn hàng phải đạt giá trị tối thiểu.
- Giảm giá vượt `requires_approval_threshold` phải được Manager hoặc Admin phê duyệt.
- Khuyến mãi chồng chéo phải được kiểm tra để tránh xung đột.
- Khuyến mãi không hợp lệ hoặc hết hạn không được áp dụng.
- Một đơn hàng chỉ áp dụng một khuyến mãi trong phạm vi demo.

### Thông báo hiển thị

```text
Mã khuyến mãi không tồn tại hoặc đã hết hạn
Đơn hàng chưa đủ điều kiện áp dụng khuyến mãi
Cần quản lý phê duyệt khuyến mãi này
Khuyến mãi đã được áp dụng
Khuyến mãi bị trùng với khuyến mãi đang áp dụng
```

### Chữ ký Service

```csharp
Task<PromotionValidationResultDto> ValidatePromotionAsync(string promoCode, CartSummaryDto cart);
Task<bool> RequestManagerApprovalAsync(Guid promotionId, Guid requestingUserId);
CartSummaryDto ApplyPromotion(PromotionDto promotion, CartSummaryDto cart);
```

---

## F-05 Thanh toán tiền mặt

### Mục tiêu

Ghi nhận thanh toán tiền mặt cho đơn hàng hợp lệ, tính tiền thừa, cập nhật trạng thái thanh
toán, liên kết thanh toán với ca hiện tại và kích hoạt các side effect sau thanh toán.

### Business Requirements

- Hệ thống cho phép Staff ghi nhận thanh toán tiền mặt.
- Hệ thống yêu cầu Staff nhập số tiền nhận từ khách hàng.
- Hệ thống tự động tính tiền thừa.
- Hệ thống lưu thông tin thanh toán tiền mặt vào đơn hàng.
- Hệ thống cập nhật trạng thái thanh toán sau khi thanh toán tiền mặt thành công.

### Business Rules

- Thanh toán tiền mặt chỉ được phép khi đơn hàng không bị khóa.
- `amount_received` phải lớn hơn hoặc bằng `total_amount`.
- Tiền thừa được tính bằng `amount_received - total_amount`.
- Không được ghi nhận thanh toán cho giỏ hàng trống.
- Thanh toán đã hoàn tất không được ghi nhận trùng.
- Thanh toán tiền mặt phải liên kết với ca hiện tại.
- Sau khi thanh toán thành công, `payment_status = Success`.
- Sau khi thanh toán thành công, `order.status = Confirmed`.
- Sau khi lưu thanh toán thành công, POS gửi sự kiện trừ kho sang Inventory Manager.
- Sau khi thanh toán thành công, hệ thống tạo hóa đơn.
- Hệ thống ghi audit log `CASH_PAYMENT`.

### Thông báo hiển thị

```text
Số tiền khách đưa không đủ để thanh toán
Thanh toán tiền mặt thành công
Không thể thanh toán đơn hàng trống
Đơn hàng đã được thanh toán, không thể thanh toán lại
```

### Chữ ký Service

```csharp
Task<PaymentResultDto> RecordCashPaymentAsync(Guid orderId, decimal amountReceived, Guid userId);
```

---

## F-06 Thanh toán VNPay

### Mục tiêu

Tạo yêu cầu thanh toán điện tử qua VNPay, khóa đơn hàng trong thời gian chờ thanh toán và
đảm bảo không tạo hóa đơn trước khi thanh toán thành công.

### Business Requirements

- Hệ thống cho phép Staff tạo yêu cầu thanh toán điện tử.
- Hệ thống gửi số tiền thanh toán và mã tham chiếu đơn hàng đến cổng thanh toán.
- Hệ thống khóa đơn hàng trong quá trình xử lý thanh toán.
- Hệ thống ngăn Staff sửa giỏ hàng khi thanh toán đang chờ xử lý.
- Hệ thống cập nhật phiên thanh toán dựa trên kết quả từ cổng thanh toán.
- Hệ thống hiển thị QR hoặc đường dẫn thanh toán VNPay cho Staff.

### Business Rules

- Đơn hàng phải được khóa trước khi gửi yêu cầu thanh toán đến VNPay.
- Đơn hàng bị khóa không được sửa.
- Nếu thanh toán thất bại, hết thời gian hoặc bị hủy, hệ thống phải mở khóa đơn hàng.
- Yêu cầu thanh toán phải có mã tham chiếu đơn hàng hợp lệ.
- Số tiền thanh toán phải khớp với tổng tiền đơn hàng.
- Hệ thống không được tạo hóa đơn trước khi thanh toán thành công.
- `vnp_Amount` phải bằng tổng tiền VND nhân 100.
- `vnp_TxnRef` dùng `orderId.ToString("N")`.
- Tham số VNPay phải được sắp xếp theo tên trước khi tạo chữ ký.
- Chữ ký dùng HMAC-SHA512 với secret hash key.
- Nếu quá 5 phút chưa có kết quả, Staff có thể hủy phiên thanh toán và mở khóa đơn.

### Thông báo hiển thị

```text
Đang chờ thanh toán VNPay
Thanh toán VNPay đã được tạo
Thanh toán quá thời gian chờ, đơn hàng đã được mở khóa
Đơn hàng đang bị khóa, không thể sửa giỏ hàng
```

### Chữ ký Service

```csharp
Task<VNPayRequestDto> CreateVNPayRequestAsync(Guid orderId);
Task<PaymentStatus> GetOrderPaymentStatusAsync(Guid orderId);
Task CancelVNPayPaymentAsync(Guid orderId);
```

---

## F-07 Callback thanh toán

### Mục tiêu

Nhận callback từ VNPay, kiểm tra dữ liệu callback, cập nhật trạng thái thanh toán, chặn xử lý
trùng callback và mở khóa đơn hàng khi thanh toán thất bại hoặc hết thời gian.

### Business Requirements

- Hệ thống nhận callback thanh toán từ VNPay.
- Hệ thống kiểm tra dữ liệu callback trước khi xử lý.
- Hệ thống cập nhật trạng thái thanh toán dựa trên kết quả callback.
- Hệ thống ngăn xử lý trùng callback đã xử lý.
- Hệ thống mở khóa đơn hàng nếu thanh toán thất bại hoặc hết thời gian.
- Hệ thống lưu raw callback payload để hỗ trợ debug demo.

### Business Rules

- Chỉ callback hợp lệ mới được chấp nhận.
- Callback order reference phải khớp với một đơn hàng đang tồn tại.
- Callback đã xử lý không được xử lý lại.
- Callback thành công chuyển `payment_status` sang `Success`.
- Callback thất bại hoặc hết thời gian chuyển trạng thái thanh toán tương ứng và mở khóa đơn hàng.
- Callback không hợp lệ phải bị từ chối và ghi log.
- Phải bỏ `vnp_SecureHash` khỏi bộ tham số trước khi tính lại chữ ký.
- Nếu `vnp_ResponseCode` là `"00"`, thanh toán thành công.
- Khi callback thành công, hệ thống tạo hóa đơn và gửi sự kiện trừ kho sang Inventory Manager.
- Khi callback thành công, hệ thống ghi audit log `VNPAY_PAYMENT_SUCCESS`.
- Mọi callback phải lưu payload vào `payments.vnpay_response`.

### Thông báo hiển thị

```text
Thanh toán VNPay thành công
Thanh toán VNPay thất bại
Chữ ký VNPay không hợp lệ
Callback thanh toán đã được xử lý trước đó
```

### Chữ ký Service

```csharp
Task HandleVNPayCallbackAsync(VNPayCallbackDto callback);
```

---

## F-08 Hóa đơn và giả lập in

### Mục tiêu

Tạo hóa đơn sau khi thanh toán thành công, cho phép xem chi tiết hóa đơn và giả lập in hóa
đơn trong demo mà không yêu cầu thiết bị in vật lý.

### Business Requirements

- Hệ thống tạo hóa đơn sau khi thanh toán thành công.
- Hệ thống gán số hóa đơn duy nhất cho mỗi hóa đơn.
- Hệ thống lưu chi tiết hóa đơn gồm tổng tiền, trạng thái thanh toán, thời điểm phát hành và thông tin đơn hàng.
- Hệ thống cho phép Staff, Manager hoặc Admin xem chi tiết hóa đơn.
- Hệ thống cho phép Staff giả lập in hóa đơn sau khi hóa đơn được phát hành.
- Hệ thống cho phép in lại nếu lần in giả lập thất bại.
- Hệ thống ghi nhận trạng thái in và kết quả in.
- Hệ thống thông báo cho Staff nếu có lỗi giả lập in.

### Business Rules

- Hóa đơn chỉ được tạo khi `payment_status = Success`.
- Mỗi `invoice_number` phải duy nhất.
- Định dạng số hóa đơn: `INV-{yyyyMMdd}-{sequence:D4}`.
- Tổng tiền hóa đơn phải lưu theo số tiền tại thời điểm phát hành.
- Không được tạo hóa đơn cho thanh toán failed, pending hoặc cancelled.
- Đơn hàng đã hoàn tất không được tạo hóa đơn trùng.
- Chỉ hóa đơn đã phát hành mới được in.
- Demo không dùng thiết bị in vật lý; `PrintReceiptAsync` thực hiện xem trước và giả lập kết quả in.
- Lỗi giả lập in phải được ghi vào `DEVICE_LOGS`.
- Lỗi in không được hủy giao dịch đã hoàn tất.
- Mọi hành động in hoặc in lại nên được ghi log.

### Nội dung hóa đơn mẫu

```text
CỬA HÀNG SMARTPOS
Hóa đơn: INV-20260604-0001
Ngày: 04/06/2026 14:32
Nhân viên: Nguyễn Văn A
Ca: 0042
--------------------------------
Sữa tươi                 x2  50000
Bánh mì                  x1  30000
--------------------------------
Tạm tính:                 80000
Giảm giá:                -5000
Thuế:                     7500
Tổng tiền:               82500
--------------------------------
Thanh toán: VNPay
Mã giao dịch: VNP123456789
Cảm ơn quý khách
```

### Thông báo hiển thị

```text
In hóa đơn thành công
Lỗi máy in, vui lòng kiểm tra cấu hình và thử lại
Hóa đơn đã được tạo
Không thể tạo hóa đơn cho thanh toán chưa thành công
```

### Chữ ký Service

```csharp
Task<InvoiceDto> GenerateInvoiceAsync(Guid orderId);
Task<InvoiceDto?> GetInvoiceByOrderAsync(Guid orderId);
Task<InvoiceDto> GetByIdAsync(Guid invoiceId);
Task PrintReceiptAsync(InvoiceDto invoice);
Task ReprintReceiptAsync(Guid invoiceId);
```

---

## F-09 Khách hàng và điểm tích lũy

### Mục tiêu

Quản lý thông tin khách hàng, hỗ trợ khách vãng lai, tìm kiếm khách hàng, cộng điểm sau
thanh toán và trừ điểm khi hoàn trả ảnh hưởng đến điểm đã cộng.

### Business Requirements

- Hệ thống cho phép Staff, Manager hoặc Admin đăng ký khách hàng mới.
- Hệ thống cho phép Staff, Manager hoặc Admin cập nhật thông tin khách hàng.
- Hệ thống hỗ trợ khách vãng lai không cần tài khoản khách hàng.
- Hệ thống kiểm tra thông tin khách hàng trước khi lưu.
- Hệ thống hỗ trợ tìm khách hàng bằng số điện thoại hoặc mã thành viên.
- Hệ thống tính điểm tích lũy sau khi thanh toán thành công.
- Hệ thống cho phép khách hàng dùng điểm hiện có.
- Hệ thống cập nhật số dư điểm sau khi cộng hoặc dùng điểm.
- Hệ thống ghi nhận giao dịch điểm tích lũy.
- Hệ thống trừ điểm khi đơn hoàn trả ảnh hưởng đến điểm đã cộng.

### Business Rules

- Số điện thoại phải duy nhất nếu có.
- Mã thành viên phải duy nhất nếu có.
- Thông tin khách hàng bắt buộc phải được kiểm tra trước khi lưu.
- Khách vãng lai được phép mua hàng với `customer_id = null`.
- Dữ liệu khách hàng không hợp lệ không được lưu.
- Điểm chỉ được cộng sau khi thanh toán thành công.
- Điểm đã dùng không được vượt số dư hiện tại của khách hàng.
- Khách vãng lai không được dùng điểm tích lũy.
- Đơn hoàn tiền phải trừ điểm đã cộng tương ứng nếu cần.
- Giao dịch điểm phải liên kết với khách hàng và đơn hàng.
- Mặc định cộng điểm theo công thức `floor(total_amount / 10000)`.
- Mặc định mỗi điểm có giá trị 1000 VND khi dùng điểm.

### Thông báo hiển thị

```text
Số điện thoại đã tồn tại
Mã thành viên đã tồn tại
Không tìm thấy khách hàng
Khách vãng lai không thể dùng điểm tích lũy
Số điểm sử dụng vượt quá số điểm hiện có
```

### Chữ ký Service

```csharp
Task<CustomerDto> RegisterCustomerAsync(CreateCustomerDto dto);
Task<CustomerDto> UpdateCustomerAsync(Guid id, CreateCustomerDto dto);
Task<CustomerDto?> FindByPhoneAsync(string phone);
Task<CustomerDto?> FindByMemberCodeAsync(string memberCode);
Task CreditLoyaltyPointsAsync(Guid customerId, Guid orderId);
Task RedeemLoyaltyPointsAsync(Guid customerId, int points, Guid orderId);
Task DeductLoyaltyPointsOnReturnAsync(Guid customerId, Guid orderId);
```

---

## F-10 Trả hàng và hoàn tiền

### Mục tiêu

Cho phép Staff tạo yêu cầu trả hàng từ đơn gốc, cho phép Manager hoặc Admin duyệt hoặc từ
chối, tính tiền hoàn, cập nhật tồn kho và ghi audit log.

Tính năng này bắt buộc có trong demo cuối.

### Business Requirements

- Hệ thống cho phép khách hàng yêu cầu trả hàng hoặc hoàn tiền thông qua Staff.
- Hệ thống cho phép Staff tìm đơn hàng gốc.
- Hệ thống cho phép Staff chọn sản phẩm trả và số lượng trả.
- Hệ thống yêu cầu nhập lý do trả hàng.
- Hệ thống ghi nhận yêu cầu trả hàng.
- Hệ thống cho phép Manager hoặc Admin duyệt hoặc từ chối yêu cầu trả hàng.
- Hệ thống tính số tiền hoàn dựa trên sản phẩm trả hợp lệ.
- Hệ thống cập nhật trạng thái trả hàng sau khi duyệt hoặc từ chối.
- Hệ thống gửi sự kiện nhập lại hàng sang Inventory Manager sau khi trả hàng được duyệt nếu cần.
- Hệ thống cập nhật bản ghi thanh toán hoặc hoàn tiền sau khi hoàn tiền được xử lý.

### Business Rules

- Chỉ đơn đã thanh toán thành công mới được trả hàng.
- Số lượng trả không được vượt số lượng đã mua trừ số lượng đã trả trước đó.
- Lý do trả hàng là bắt buộc.
- Yêu cầu trả hàng phải liên kết với đơn hàng gốc.
- Đơn không hợp lệ hoặc chưa thanh toán không được trả hàng.
- Chỉ Manager hoặc Admin được duyệt trả hàng hoặc hoàn tiền.
- Hoàn tiền hoặc nhập lại hàng chỉ được thực hiện sau khi duyệt.
- Tiền hoàn không được vượt giá trị sản phẩm trả hợp lệ.
- Sản phẩm trả chỉ được nhập lại kho sau khi duyệt.
- Yêu cầu bị từ chối không được cập nhật tồn kho hoặc số tiền hoàn.
- Hành động duyệt trả hàng hoặc hoàn tiền phải ghi audit log.
- Sau khi duyệt, POS cập nhật tồn cục bộ và gửi restock event sang Inventory Manager.
- Nếu đơn có khách hàng, hệ thống trừ lại điểm đã cộng từ đơn hàng đó nếu cần.

### Thông báo hiển thị

```text
Chỉ có thể trả hàng cho đơn đã thanh toán thành công
Vui lòng nhập lý do trả hàng
Số lượng trả vượt quá số lượng có thể trả
Yêu cầu trả hàng đã được duyệt
Yêu cầu trả hàng đã bị từ chối
Bạn không có quyền duyệt yêu cầu trả hàng
```

### Chữ ký Service

```csharp
Task<ReturnDto> CreateReturnRequestAsync(ReturnRequestDto dto, Guid requestedByUserId);
Task<ReturnDto> ApproveReturnAsync(Guid returnId, Guid approvedByUserId);
Task<ReturnDto> RejectReturnAsync(Guid returnId, Guid approvedByUserId);
Task<ReturnDto?> GetByOrderIdAsync(Guid orderId);
```

---

## F-11 Danh mục sản phẩm

### Mục tiêu

Cho phép Manager hoặc Admin quản lý danh mục và sản phẩm, đảm bảo SKU, barcode, QR code duy
nhất và bảo toàn lịch sử sản phẩm cho các giao dịch cũ.

### Business Requirements

- Hệ thống cho phép Manager hoặc Admin quản lý danh mục sản phẩm.
- Hệ thống cho phép Manager hoặc Admin tạo, cập nhật và ngừng hoạt động sản phẩm.
- Hệ thống lưu SKU, barcode, QR code, tên, giá, thuế và trạng thái sản phẩm.
- Hệ thống cho phép sản phẩm được tìm kiếm và chọn trong quá trình bán hàng.
- Hệ thống bảo toàn lịch sử sản phẩm cho giao dịch cũ.

### Business Rules

- SKU phải duy nhất.
- Barcode phải duy nhất nếu có.
- QR code phải duy nhất nếu có.
- Sản phẩm đã có giao dịch không được xóa vĩnh viễn.
- Sản phẩm đã có lịch sử giao dịch phải chuyển sang `Inactive` thay vì xóa.
- Sản phẩm inactive không được thêm vào đơn bán hàng mới.
- Chỉ Manager hoặc Admin được tạo, cập nhật hoặc ngừng hoạt động sản phẩm.
- Danh mục inactive không nên được chọn cho sản phẩm mới.

### Thông báo hiển thị

```text
SKU đã tồn tại
Mã vạch đã tồn tại
Mã QR đã tồn tại
Không thể xóa sản phẩm đã có giao dịch, vui lòng ngừng hoạt động sản phẩm
Sản phẩm đã được cập nhật
```

### Chữ ký Service

```csharp
Task<List<CategoryDto>> GetCategoriesAsync();
Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto, Guid userId);
Task<CategoryDto> UpdateCategoryAsync(Guid id, CreateCategoryDto dto, Guid userId);
Task<List<ProductDto>> GetProductsAsync();
Task<ProductDto> CreateProductAsync(CreateProductDto dto, Guid userId);
Task<ProductDto> UpdateProductAsync(Guid id, CreateProductDto dto, Guid userId);
Task DeactivateProductAsync(Guid productId, Guid userId);
```

---

## F-12 Giá, thuế và khuyến mãi

### Mục tiêu

Cho phép Manager hoặc Admin quản lý giá, thuế và khuyến mãi; đảm bảo giá lịch sử của đơn
cũ không bị thay đổi và thuế/khuyến mãi được áp dụng đúng khi checkout.

### Business Requirements

- Hệ thống cho phép Manager hoặc Admin quản lý giá sản phẩm.
- Hệ thống cho phép Manager hoặc Admin cấu hình thuế.
- Hệ thống cho phép Manager hoặc Admin tạo và quản lý khuyến mãi.
- Hệ thống áp dụng đúng giá, thuế và khuyến mãi khi checkout.
- Hệ thống bảo toàn giá lịch sử đã dùng trong đơn hàng cũ.

### Business Rules

- Thay đổi giá không được ảnh hưởng đơn hàng cũ.
- Dòng hàng đã bán phải giữ đơn giá tại thời điểm mua.
- Khuyến mãi chồng chéo phải được kiểm tra.
- Khoảng ngày khuyến mãi phải hợp lệ.
- Thuế phải áp dụng theo cấu hình cửa hàng.
- Xung đột khuyến mãi không hợp lệ phải bị chặn.
- Sửa giá phải ghi audit log `PRICE_UPDATED`.
- Thay đổi cấu hình thuế nên ghi audit log.

### Thông báo hiển thị

```text
Giá sản phẩm đã được cập nhật
Khoảng ngày khuyến mãi không hợp lệ
Khuyến mãi bị trùng với khuyến mãi khác
Cấu hình thuế không hợp lệ
```

### Chữ ký Service

```csharp
Task<ProductDto> UpdatePriceAsync(UpdatePriceDto dto, Guid userId);
Task<PromotionDto> CreatePromotionAsync(CreatePromotionDto dto, Guid userId);
Task<PromotionDto> UpdatePromotionAsync(Guid promotionId, UpdatePromotionDto dto, Guid userId);
Task<TaxConfigurationDto> UpdateTaxConfigurationAsync(UpdateTaxConfigurationDto dto, Guid userId);
```

---

## F-13 Đồng bộ Inventory Manager

### Mục tiêu

Đồng bộ danh mục và tồn kho từ Inventory Manager, gửi sự kiện trừ kho sau bán hàng thành
công, gửi sự kiện nhập lại hàng sau khi trả hàng được duyệt và ghi nhận trạng thái đồng bộ.

### Business Requirements

- Hệ thống đồng bộ danh mục sản phẩm từ Inventory Manager.
- Hệ thống map external inventory product ID với local product ID.
- Hệ thống cập nhật thông tin sản phẩm cục bộ sau khi đồng bộ thành công.
- Hệ thống ghi nhận kết quả đồng bộ.
- Hệ thống cho phép retry khi đồng bộ thất bại.
- Hệ thống đồng bộ tồn kho cục bộ từ Inventory Manager.
- Hệ thống lưu `LocalStockQuantity` như bản sao tồn kho từ hệ thống kho.
- Hệ thống cập nhật `last_synced_at` sau khi đồng bộ thành công.
- Hệ thống hiển thị tồn kho cục bộ hiện tại cho Staff khi bán hàng.
- Hệ thống phát hiện dữ liệu tồn kho cũ hoặc đồng bộ thất bại.
- Hệ thống gửi stock deduction event sang Inventory Manager sau khi thanh toán thành công.
- Hệ thống gửi restock event sang Inventory Manager sau khi trả hàng được duyệt nếu cần.
- Hệ thống ghi nhận trạng thái từng inventory event.
- Hệ thống cho phép retry nếu gửi event thất bại.
- Hệ thống ngăn trừ kho cho thanh toán thất bại.

### Business Rules

- Đồng bộ phải thực hiện qua HTTP API, không đọc trực tiếp cơ sở dữ liệu Inventory Manager.
- POS và Inventory Manager dùng cơ sở dữ liệu riêng.
- `external_inventory_id` phải map đúng với local `product_id`.
- Lỗi đồng bộ phải được ghi vào `inventory_sync_logs`.
- Bản ghi đồng bộ thất bại phải retry được.
- Dữ liệu danh mục từ Inventory Manager không được tạo trùng sản phẩm cục bộ.
- `LocalStockQuantity` là bản sao cục bộ của tồn kho từ Inventory Manager.
- Mỗi bản ghi đồng bộ phải có `last_synced_at`.
- Mỗi bản ghi đồng bộ phải có `sync_status`.
- Dữ liệu tồn kho quá cũ cần được phát hiện.
- Sản phẩm có tồn kho cục bộ bằng 0 không được thêm vào giỏ.
- Stock deduction chỉ gửi sau khi `payment_status = Success`.
- Nếu thanh toán failed hoặc timeout, không được trừ kho.
- Restock event chỉ gửi sau khi return được duyệt.
- Mỗi inventory event phải liên kết với sale hoặc return.
- Inventory event thất bại phải được ghi nhận và retry được.

### Hợp đồng API

```text
GET  /api/sync/catalog
GET  /api/sync/stock
POST /api/stock/deduct
POST /api/stock/restock
```

### Thông báo hiển thị

```text
Đồng bộ danh mục thành công
Đồng bộ tồn kho thành công
Đồng bộ tồn kho thất bại, vui lòng kiểm tra nhật ký
Không kết nối được Inventory Manager, vui lòng thử lại
Sự kiện tồn kho đã được ghi nhận để thử lại
```

### Chữ ký Service

```csharp
Task<SyncResultDto> SyncCatalogAsync();
Task<SyncResultDto> SyncStockAsync();
Task SendStockDeductionAsync(List<OrderItemDto> items, Guid orderId);
Task SendRestockAsync(List<ReturnItemDto> items, Guid returnId);
Task RetryFailedInventoryEventAsync(Guid eventId);
```

---

## F-14 Thiết bị giả lập và nhật ký thiết bị

### Mục tiêu

Quản lý cấu hình thiết bị giả lập dùng cho demo, ghi nhận sự kiện quét/in và lỗi thiết bị
giả lập để hỗ trợ kiểm tra luồng demo mà không cần thiết bị vật lý.

### Business Requirements

- Hệ thống cho phép Manager hoặc Admin cấu hình scanner và printer giả lập.
- Hệ thống lưu device type, connection type, serial number và status.
- Hệ thống chỉ cho phép thiết bị `Active` được dùng.
- Hệ thống cho phép Manager hoặc Admin bật hoặc tắt thiết bị giả lập.
- Hệ thống hiển thị cấu hình thiết bị cho người dùng có quyền.
- Hệ thống ghi nhận sự kiện scanner và printer.
- Hệ thống ghi nhận lỗi scanner và printer.
- Hệ thống hiển thị lỗi in giả lập cho Staff.
- Hệ thống cho phép Manager hoặc Admin xem device logs.
- Hệ thống hỗ trợ xử lý sự cố dựa trên lịch sử device logs.

### Business Rules

- Demo không yêu cầu phần cứng thật.
- Scanner được thay bằng ô nhập mã, nút chọn sản phẩm hoặc danh sách sản phẩm.
- Printer được thay bằng màn hình xem trước hóa đơn và nút giả lập in.
- Chỉ Manager hoặc Admin được cấu hình thiết bị.
- Mỗi thiết bị giả lập nên có serial number duy nhất nếu có cấu hình serial.
- Device type là bắt buộc.
- Connection type là bắt buộc, nhưng với demo có thể dùng giá trị `Emulator`.
- Chỉ thiết bị `Active` mới được dùng.
- Thiết bị bị tắt không được dùng cho quét hoặc in giả lập.
- Mọi lỗi quét phải ghi vào `DEVICE_LOGS`.
- Mọi lỗi in phải ghi vào `DEVICE_LOGS`.
- Trạng thái lỗi in như hết giấy, kẹt giấy hoặc cảnh báo chỉ là trạng thái giả lập trong demo.
- Device logs phải gồm device ID, event type, message, timestamp và user ID nếu có.
- Lỗi thiết bị không được bị bỏ qua âm thầm.
- Lỗi in không được hủy giao dịch đã thanh toán thành công.
- Thay đổi cấu hình thiết bị phải ghi audit log `DEVICE_CONFIGURED`.

### Thông báo hiển thị

```text
Thiết bị giả lập đã được cập nhật
In hóa đơn thành công
Lỗi máy in, vui lòng kiểm tra cấu hình và thử lại
Không tìm thấy thiết bị giả lập đang hoạt động
```

### Chữ ký Service

```csharp
Task PrintReceiptAsync(InvoiceDto invoice);
Task ReprintReceiptAsync(Guid invoiceId);
Task LogDeviceEventAsync(DeviceLogDto dto);
Task<List<DeviceDto>> GetActiveDevicesAsync(DeviceType deviceType);
Task<DeviceDto> ConfigureDeviceAsync(ConfigureDeviceDto dto, Guid userId);
Task SetDeviceStatusAsync(Guid deviceId, bool isActive, Guid userId);
```

---

## F-15 Báo cáo, audit log và cấu hình cửa hàng

### Mục tiêu

Cho phép Manager hoặc Admin xem báo cáo vận hành, xem audit logs và cấu hình thông tin cửa
hàng dùng trong checkout, hóa đơn và báo cáo.

### Business Requirements

- Hệ thống cho phép Manager hoặc Admin xem báo cáo bán hàng.
- Hệ thống cho phép Manager hoặc Admin xem báo cáo thanh toán.
- Hệ thống cho phép Manager hoặc Admin xem báo cáo liên quan tồn kho.
- Hệ thống cho phép Manager hoặc Admin xem báo cáo ca.
- Hệ thống cho phép lọc báo cáo theo khoảng ngày, nhân viên, ca và trạng thái thanh toán.
- Hệ thống ghi nhận các thao tác quan trọng của người dùng.
- Hệ thống cho phép Manager hoặc Admin xem audit logs.
- Hệ thống hiển thị audit information gồm user ID, action, timestamp và affected entity.
- Hệ thống hỗ trợ theo dõi thao tác nhạy cảm về bảo mật và nghiệp vụ.
- Hệ thống giúp quản lý điều tra thao tác đáng ngờ hoặc sai lệch.
- Hệ thống cho phép Manager hoặc Admin cấu hình thông tin cửa hàng.
- Hệ thống lưu tên cửa hàng, địa chỉ, tiền tệ, múi giờ, cấu hình thuế và quy tắc làm tròn.
- Hệ thống áp dụng cấu hình cửa hàng khi checkout.
- Hệ thống áp dụng cấu hình cửa hàng khi tạo hóa đơn và nội dung in.
- Hệ thống cho phép Manager hoặc Admin cập nhật cấu hình khi cần.

### Business Rules

- Chỉ Manager hoặc Admin được xem báo cáo.
- Báo cáo phải lọc được theo khoảng ngày.
- Báo cáo phải lọc được theo nhân viên.
- Báo cáo phải lọc được theo ca.
- Báo cáo phải lọc được theo trạng thái thanh toán.
- Số liệu tiền mặt trong báo cáo phải đối soát được với bản ghi ca.
- Báo cáo không được lộ dữ liệu không được phép cho Staff.
- Các thao tác quan trọng phải được ghi vào audit logs.
- Sửa giá phải được audit.
- Hủy đơn phải được audit.
- Duyệt hoàn tiền phải được audit.
- Thay đổi cấu hình thiết bị phải được audit.
- Thay đổi quyền phải được audit.
- Thay đổi cấu hình cửa hàng phải được audit.
- Audit logs phải gồm user ID và timestamp.
- Chỉ Manager hoặc Admin được xem audit logs.
- Chỉ Manager hoặc Admin được cập nhật cấu hình cửa hàng.
- Quy tắc làm tròn phải được áp dụng nhất quán khi tính tổng tiền.
- Tiền tệ phải được cấu hình trước khi bán hàng.
- Cấu hình thuế phải hợp lệ trước khi áp dụng thuế.
- Thay đổi cấu hình cửa hàng nên được ghi vào audit logs.

### Thông báo hiển thị

```text
Bạn không có quyền xem báo cáo
Không có dữ liệu trong khoảng thời gian đã chọn
Cấu hình cửa hàng đã được cập nhật
Cấu hình thuế không hợp lệ
Quy tắc làm tròn không hợp lệ
```

### Chữ ký Service

```csharp
Task<ShiftReportDto> GetShiftReportAsync(Guid shiftId);
Task<SalesReportDto> GetSalesReportAsync(SalesReportFilterDto filter);
Task<List<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterDto filter);
Task LogAsync(string action, string entity, Guid? entityId, object? oldValue, object? newValue, Guid userId);
Task<StoreConfigurationDto> GetStoreConfigurationAsync();
Task<StoreConfigurationDto> UpdateStoreConfigurationAsync(UpdateStoreConfigurationDto dto, Guid userId);
```

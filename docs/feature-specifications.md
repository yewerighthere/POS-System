# Đặc tả tính năng

File này là nơi tra cứu quy tắc nghiệp vụ. Khi làm một tính năng, hãy đọc phần tương ứng
trước khi sửa mã.

## F-01 Đăng nhập và đăng xuất

### Luồng đúng

Người dùng nhập tên đăng nhập và mật khẩu. Hệ thống kiểm tra mật khẩu, tạo token, ghi phiên
đăng nhập và đưa người dùng đến màn hình phù hợp với vai trò.

### Quy tắc

- Tài khoản `Locked` bị từ chối bất kể mật khẩu đúng hay sai.
- Tài khoản `Inactive` bị từ chối.
- Mỗi lần đăng nhập thành công phải tạo bản ghi `user_sessions`.
- Đăng xuất phải cập nhật `logout_at`.
- Nếu đã có phiên cũ trên cùng máy, đăng nhập mới phải đóng phiên cũ.
- Cần seed sẵn tài khoản demo, nhưng vẫn nên có màn hình tạo tài khoản cho trường hợp giảng
  viên yêu cầu tạo mới.

Thông báo:

```text
Tên đăng nhập hoặc mật khẩu không đúng
Tài khoản đã bị khóa, vui lòng liên hệ quản lý
Tài khoản đã ngừng hoạt động
```

Chữ ký Service:

```csharp
Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
Task LogoutAsync(Guid userId);
Task<UserDto> CreateUserAsync(CreateUserDto request);
```

## F-02 Mở ca và đóng ca

### Quy tắc

- Mỗi người dùng chỉ có một ca đang mở.
- Tiền đầu ca phải lớn hơn hoặc bằng 0.
- Không vào màn hình bán hàng nếu chưa có ca đang mở.
- Khi đóng ca, tính:

```text
tiền_dự_kiến = tiền_đầu_ca + tổng_tiền_mặt_trong_ca
chênh_lệch = tiền_cuối_ca - tiền_dự_kiến
```

- Chênh lệch khác 0 vẫn cho đóng ca, chỉ ghi nhận để báo cáo.
- Nếu ứng dụng tắt giữa ca, lần đăng nhập sau phải tự tìm ca đang mở và tiếp tục.

Thông báo:

```text
Bạn đang có ca làm việc đang mở
Tiền đầu ca không được âm
Vui lòng mở ca trước khi bán hàng
```

Chữ ký Service:

```csharp
Task<ShiftDto> OpenShiftAsync(Guid userId, decimal openingCash);
Task<ShiftSummaryDto> CloseShiftAsync(Guid shiftId, decimal closingCash);
Task<ShiftDto?> GetOpenShiftAsync(Guid userId);
```

## F-03 Chọn sản phẩm và tìm sản phẩm

### Luồng demo

Nhóm không dùng máy quét thật. Màn hình bán hàng cần có ô nhập mã và danh sách sản phẩm để
nhân viên chọn nhanh. Logic xử lý vẫn giống như khi nhận được mã từ máy quét.

### Quy tắc

- Sản phẩm ngừng hoạt động không được thêm vào giỏ.
- Sản phẩm hết hàng không được thêm vào giỏ.
- Tìm kiếm chỉ trả sản phẩm đang hoạt động.
- Tìm theo tên, SKU, mã vạch hoặc QR.
- Nếu không tìm thấy, gợi ý đồng bộ danh mục.

Thông báo:

```text
Không tìm thấy sản phẩm, vui lòng kiểm tra mã hoặc đồng bộ danh mục
Sản phẩm đã ngừng kinh doanh
Sản phẩm đã hết hàng
```

Chữ ký Service:

```csharp
Task<ProductDto?> FindByBarcodeAsync(string barcode);
Task<List<ProductSearchResultDto>> SearchProductsAsync(string query);
```

## F-04 Giỏ hàng

### Quy tắc

- Nếu thêm sản phẩm đã có trong giỏ, tăng số lượng lên 1.
- Số lượng trong giỏ không được vượt tồn kho tại POS.
- Nếu giá sản phẩm thay đổi trước khi thanh toán, cần cảnh báo nhân viên.
- Mỗi thay đổi trong giỏ phải tính lại toàn bộ tổng tiền.
- Xóa hết sản phẩm không hủy đơn, đơn vẫn ở trạng thái nhập.

Công thức:

```text
tạm_tính_dòng = đơn_giá * số_lượng - giảm_giá_dòng
tạm_tính_đơn = tổng tạm_tính_dòng
giảm_giá_đơn = giảm giá cấp đơn hàng
tiền_tính_thuế = tạm_tính_đơn - giảm_giá_đơn
tiền_thuế = tiền_tính_thuế * thuế_suất
tổng_tiền = tiền_tính_thuế + tiền_thuế
```

Làm tròn tiền bằng:

```csharp
Math.Round(value, 2, MidpointRounding.AwayFromZero)
```

Chữ ký Service:

```csharp
CartSummaryDto AddItem(Guid productId, int quantity, CartSummaryDto currentCart);
CartSummaryDto UpdateItemQuantity(Guid productId, int newQuantity, CartSummaryDto currentCart);
CartSummaryDto RemoveItem(Guid productId, CartSummaryDto currentCart);
CartSummaryDto RecalculateTotals(CartSummaryDto cart);
```

## F-05 Khuyến mãi

### Luồng đúng

Nhân viên nhập `code` của khuyến mãi. Hệ thống kiểm tra tính hợp lệ, tính số tiền giảm và áp
dụng vào giỏ hàng. Nếu vượt ngưỡng, quản lý hoặc quản trị phải phê duyệt.

### Quy tắc

- Bảng `promotions` bắt buộc có cột `code`.
- Mã khuyến mãi phải đang hoạt động.
- Ngày hiện tại nằm trong khoảng bắt đầu và kết thúc.
- Đơn hàng phải đạt giá trị tối thiểu nếu có cấu hình.
- Khuyến mãi theo sản phẩm chỉ áp dụng cho sản phẩm đó.
- Nếu tiền giảm vượt `requires_approval_threshold`, cần phê duyệt.
- Một đơn hàng chỉ áp dụng một khuyến mãi trong demo.

Thông báo:

```text
Mã khuyến mãi không tồn tại hoặc đã hết hạn
Đơn hàng chưa đủ điều kiện áp dụng khuyến mãi
Cần quản lý phê duyệt khuyến mãi này
Khuyến mãi đã được áp dụng
```

Chữ ký Service:

```csharp
Task<PromotionValidationResultDto> ValidatePromotionAsync(string promoCode, CartSummaryDto cart);
Task<bool> RequestManagerApprovalAsync(Guid promotionId, Guid requestingUserId);
CartSummaryDto ApplyPromotion(PromotionDto promotion, CartSummaryDto cart);
```

## F-06 Thanh toán tiền mặt

### Quy tắc

- Đơn hàng không được đang bị khóa.
- Tiền khách đưa phải lớn hơn hoặc bằng tổng tiền.
- Tiền thừa bằng tiền khách đưa trừ tổng tiền.
- Sau khi thanh toán, đơn hàng chuyển sang `Confirmed`, thanh toán chuyển sang `Success`.
- Sau khi lưu thành công, gửi sự kiện trừ kho sang Inventory Manager.
- Tạo hóa đơn.
- Ghi nhật ký `CASH_PAYMENT`.

Thông báo:

```text
Số tiền khách đưa không đủ để thanh toán
Thanh toán tiền mặt thành công
```

Chữ ký Service:

```csharp
Task<PaymentResultDto> RecordCashPaymentAsync(Guid orderId, decimal amountReceived, Guid userId);
```

## F-07 Thanh toán VNPay

### Tạo yêu cầu thanh toán

- Đơn hàng phải ở trạng thái chờ thanh toán.
- Đơn hàng không được đang bị khóa.
- Đặt `is_locked = true` để ngăn sửa giỏ hàng.
- Tạo tham số VNPay đúng quy tắc môi trường thử nghiệm.
- `vnp_Amount` bằng tổng tiền VND nhân 100.
- `vnp_TxnRef` dùng `orderId.ToString("N")`.
- Sắp xếp tham số theo tên trước khi tạo chữ ký.
- Chữ ký dùng HMAC-SHA512 với secret hash key.

### Callback

- Nhận tất cả tham số `vnp_*`.
- Bỏ `vnp_SecureHash` ra khỏi bộ tham số trước khi tính lại chữ ký.
- Nếu chữ ký sai, trả lời lỗi và không cập nhật đơn hàng.
- Tìm đơn hàng bằng `vnp_TxnRef`.
- Nếu `vnp_ResponseCode` là `"00"`, cập nhật thành công, mở khóa đơn, tạo hóa đơn và trừ kho.
- Mã khác `"00"` là thất bại, cập nhật thất bại và mở khóa đơn.
- Lưu payload callback vào `payments.vnpay_response`.

### WPF kiểm tra trạng thái

Trong hộp thoại VNPay, WPF gọi `GetOrderPaymentStatusAsync` mỗi 2 giây. Nếu sau 5 phút chưa
có kết quả, cho phép hủy thanh toán và mở khóa đơn.

Thông báo:

```text
Đang chờ thanh toán VNPay
Thanh toán VNPay thành công
Thanh toán VNPay thất bại
Thanh toán quá thời gian chờ, đơn hàng đã được mở khóa
Chữ ký VNPay không hợp lệ
```

Chữ ký Service:

```csharp
Task<VNPayRequestDto> CreateVNPayRequestAsync(Guid orderId);
Task HandleVNPayCallbackAsync(VNPayCallbackDto callback);
Task<PaymentStatus> GetOrderPaymentStatusAsync(Guid orderId);
Task CancelVNPayPaymentAsync(Guid orderId);
```

## F-08 Hóa đơn và giả lập in

### Quy tắc

- Chỉ tạo hóa đơn khi thanh toán thành công.
- `invoice_number` là duy nhất.
- Định dạng số hóa đơn: `INV-{yyyyMMdd}-{sequence:D4}`.
- Hóa đơn lưu tổng tiền tại thời điểm phát hành.
- Được in lại hóa đơn cũ.
- Demo không dùng máy in thật, chỉ hiển thị bản xem trước và nút giả lập in.

Nội dung hóa đơn mẫu:

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

Thông báo:

```text
In hóa đơn thành công
Lỗi máy in, vui lòng kiểm tra cấu hình và thử lại
```

Chữ ký Service:

```csharp
Task<InvoiceDto> GenerateInvoiceAsync(Guid orderId);
Task<InvoiceDto?> GetInvoiceByOrderAsync(Guid orderId);
Task PrintReceiptAsync(InvoiceDto invoice);
Task ReprintReceiptAsync(Guid invoiceId);
```

## F-09 Khách hàng và điểm tích lũy

### Quy tắc

- Số điện thoại là duy nhất nếu có.
- Mã thành viên là duy nhất nếu có.
- Cho phép khách vãng lai.
- Chỉ cộng điểm sau khi thanh toán thành công.
- Cộng điểm theo công thức `floor(total_amount / 10000)`.
- Đổi điểm không được vượt số điểm hiện có.
- Mỗi điểm đổi được 1000 VND giảm giá.
- Khi trả hàng được duyệt, trừ điểm đã cộng từ đơn hàng đó.

Chữ ký Service:

```csharp
Task<CustomerDto> RegisterCustomerAsync(CreateCustomerDto dto);
Task<CustomerDto> UpdateCustomerAsync(Guid id, CreateCustomerDto dto);
Task<CustomerDto?> FindByPhoneAsync(string phone);
Task<CustomerDto?> FindByMemberCodeAsync(string memberCode);
Task CreditLoyaltyPointsAsync(Guid customerId, Guid orderId);
Task RedeemLoyaltyPointsAsync(Guid customerId, int points, Guid orderId);
Task DeductLoyaltyPointsOnReturnAsync(Guid customerId, Guid orderId);
```

## F-10 Trả hàng và hoàn tiền

Tính năng này bắt buộc có trong demo cuối.

### Quy tắc

- Chỉ trả hàng cho đơn đã thanh toán thành công.
- Lý do trả hàng bắt buộc có.
- Tổng số lượng trả của mỗi dòng hàng không được vượt số lượng đã mua trừ số lượng đã trả.
- Hoàn tiền và nhập lại hàng chỉ xảy ra sau khi quản lý hoặc quản trị duyệt.
- Tiền hoàn không được vượt giá trị hàng trả.
- Sau khi duyệt, cập nhật tồn POS và gửi sự kiện nhập lại hàng sang Inventory Manager.
- Nếu có khách hàng, trừ lại điểm đã cộng.

Thông báo:

```text
Chỉ có thể trả hàng cho đơn đã thanh toán thành công
Vui lòng nhập lý do trả hàng
Số lượng trả vượt quá số lượng có thể trả
Yêu cầu trả hàng đã được duyệt
Yêu cầu trả hàng đã bị từ chối
```

Chữ ký Service:

```csharp
Task<ReturnDto> CreateReturnRequestAsync(ReturnRequestDto dto, Guid requestedByUserId);
Task<ReturnDto> ApproveReturnAsync(Guid returnId, Guid approvedByUserId);
Task<ReturnDto> RejectReturnAsync(Guid returnId, Guid approvedByUserId);
```

## F-11 Đồng bộ Inventory Manager

### Quy tắc

- Mỗi lần đồng bộ phải ghi `inventory_sync_logs` dù thành công hay thất bại.
- Trừ kho chỉ gửi sau khi thanh toán chuyển sang `Success`.
- Nhập lại hàng chỉ gửi sau khi trả hàng chuyển sang `Approved`.
- Liên kết sản phẩm bằng `external_inventory_id`.
- POS không đọc trực tiếp cơ sở dữ liệu Inventory Manager.
- Inventory Manager không đọc trực tiếp cơ sở dữ liệu POS.

Hợp đồng API:

```text
GET  /api/sync/catalog
GET  /api/sync/stock
POST /api/stock/deduct
POST /api/stock/restock
```

Chữ ký Service:

```csharp
Task<SyncResultDto> SyncCatalogAsync();
Task<SyncResultDto> SyncStockAsync();
Task SendStockDeductionAsync(List<OrderItemDto> items, Guid orderId);
Task SendRestockAsync(List<ReturnItemDto> items, Guid returnId);
```

## F-12 Báo cáo

### Báo cáo ca

Chỉ quản lý và quản trị được xem.

Cần hiển thị:

- Số đơn hoàn tất.
- Tổng doanh thu.
- Doanh thu tiền mặt.
- Doanh thu VNPay.
- Tiền đầu ca.
- Tiền cuối ca.
- Tiền mặt dự kiến.
- Chênh lệch tiền mặt.

### Báo cáo doanh thu

Cần lọc theo ngày, nhân viên và trạng thái thanh toán. Cần hiển thị số đơn, doanh thu, giá
trị trung bình mỗi đơn và 5 sản phẩm bán nhiều nhất.

## F-13 Nhật ký thao tác

Service phải ghi nhật ký cho các thao tác sau:

| Hằng số | Khi nào ghi |
|---|---|
| `PRICE_UPDATED` | Sửa giá sản phẩm |
| `ORDER_CANCELLED` | Hủy đơn hàng |
| `REFUND_APPROVED` | Duyệt trả hàng |
| `DEVICE_CONFIGURED` | Thêm hoặc sửa cấu hình thiết bị giả lập |
| `ROLE_CHANGED` | Đổi vai trò người dùng |
| `PROMOTION_APPLIED_WITH_APPROVAL` | Quản lý phê duyệt khuyến mãi vượt ngưỡng |
| `CASH_PAYMENT` | Thanh toán tiền mặt |
| `VNPAY_PAYMENT_SUCCESS` | Callback VNPay thành công |

Nội dung nhật ký nghiệp vụ phải viết tiếng Việt có dấu. Ví dụ:

```text
Đã cập nhật giá sản phẩm
Đã duyệt yêu cầu trả hàng
Đã ghi nhận thanh toán tiền mặt
```

Chữ ký Service:

```csharp
Task LogAsync(string action, string entity, Guid? entityId, object? oldValue, object? newValue, Guid userId);
```

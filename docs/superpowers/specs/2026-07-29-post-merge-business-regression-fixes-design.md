# Thiết kế sửa lỗi nghiệp vụ sau merge

**Ngày:** 2026-07-29  
**Phạm vi:** 7 lỗi nghiệp vụ phát hiện khi kiểm thử toàn bộ session scope, cộng lỗi layout quản trị liên quan trực tiếp đến các form bị lỗi.

## Mục tiêu

- Chặn tạo đơn khi quán đang tạm nghỉ.
- Tích hợp khung giờ bán và quota ngày vào luồng tạo đơn; hoàn quota khi đơn bị hủy, từ chối hoặc hết hạn.
- Chỉ tính điểm thành viên trên giá trị món ăn sau giảm giá, không gồm phí giao hàng.
- Chuẩn hóa tài khoản `manager` từ role dư `4` về `AccountRole.Manager = 0`.
- Làm hai form quản trị nguyên liệu và tạm nghỉ hoạt động độc lập, hiển thị lỗi rõ ràng.
- Cho phép khiếu nại đối với đơn `Completed`, `Rejected`, `DeliveryFailed`; chỉ cho đánh giá sao đối với `Completed`.
- Không hiển thị khóa chính tuần tự của đơn trên giao diện khách; dùng `OrderCode`.
- Dùng đúng layout quản trị cho toàn bộ `/Admin`.

## Nguyên tắc kiến trúc

- Giữ kiến trúc PageModel → Service → `AppDbContext`.
- Không thêm Repository.
- Mọi dependency được inject qua constructor.
- SignalR tiếp tục được phát từ service qua publisher/`IHubContext`, không gọi Hub trực tiếp.
- Kiểm tra nghiệp vụ đặt đơn phải nằm ở service phía server, không chỉ ở giao diện.
- Ưu tiên thay đổi nhỏ, tương thích code sau merge, không tạo thêm role hoặc tầng điều phối mới.

## Thiết kế chi tiết

### 1. Quán tạm nghỉ và quota khi tạo đơn

`OrderService` nhận thêm `IStoreAvailabilityService` và `IMenuAvailabilityService`.

Trước khi lưu đơn:

1. Kiểm tra trạng thái quán.
2. Chuẩn hóa các dòng giỏ hàng.
3. Gọi `TryReserveAsync` để xác nhận món còn bán trong khung giờ và giữ quota.
4. Chỉ sau khi giữ quota thành công mới tạo đơn và commit mã giảm giá.

Nếu bất kỳ bước nào sau khi giữ quota thất bại, service gọi `ReleaseAsync` để bù lại quota. Khi một đơn đã tạo chuyển sang `Cancelled`, `Rejected` hoặc `Expired`, quota cũng được hoàn đúng một lần theo ngày đặt đơn. Không hoàn quota cho `Completed`, `DeliveryFailed` hoặc `NoShow` vì năng lực/nguyên liệu đã được sử dụng.

Thông báo từ service availability được chuyển thành `OrderValidationException` để Checkout hiển thị lỗi nghiệp vụ thay vì HTTP 500.

### 2. Điểm thành viên

Nguồn tính điểm thống nhất là `Order.Subtotal`, tức tổng sau giảm giá và trước phí giao hàng. Cả cộng điểm thực tế, thông báo dự kiến trên trang theo dõi và ghi chú sổ điểm đều dùng quy tắc này.

### 3. Role manager

- Giữ `AccountRole.Manager = 0`.
- Chạy câu SQL idempotent chỉ đổi tài khoản có username `manager` và role `4` về `0`.
- Lưu script sửa dữ liệu trong repo để máy thành viên khác có thể áp dụng.
- Trang login không ném exception khi gặp role ngoài enum; thay vào đó trả về lỗi đăng nhập/cấu hình dễ hiểu.
- Không sửa các migration schema đã áp dụng và không tạo role mới.

### 4. Form quản trị

Mỗi handler chỉ bind DTO của chính form đó:

- Tạo nguyên liệu không bị validation của form liên kết món can thiệp.
- Tạm nghỉ/mở lại quán không bị validation của form giờ hoạt động can thiệp.

Các trang có validation summary để người dùng nhìn thấy nguyên nhân khi dữ liệu không hợp lệ.

Thêm `/Pages/Admin/_ViewStart.cshtml` để mọi trang con mặc định dùng `_AdminLayout`; trang login tiếp tục tự chọn layout xác thực.

### 5. Phản hồi và mã đơn công khai

Tách hai điều kiện:

- Rating: chỉ `Completed`.
- Complaint: `Completed`, `Rejected`, `DeliveryFailed`.

Điều kiện sở hữu bằng số điện thoại và giới hạn thời gian phản hồi vẫn giữ nguyên. Link phản hồi xuất hiện trên trang theo dõi đúng với từng trạng thái.

Các trang tài khoản, phản hồi và ghi chú tích điểm mới dùng `OrderCode` thay cho `Order.Id`. ID nội bộ vẫn có thể dùng trong màn hình vận hành/admin.

## Xử lý lỗi và tính nhất quán

- Quota reservation dùng cơ chế transaction hiện có của `MenuAvailabilityService`.
- Vì tạo đơn và reservation hiện chưa dùng chung một transaction, `OrderService` dùng compensation có kiểm soát: đã reserve nhưng tạo đơn thất bại thì release.
- Release chỉ chạy sau một state transition hợp lệ để tránh hoàn quota lặp.
- Dữ liệu role sửa bằng script có điều kiện nên chạy lại không gây tác dụng phụ.

## Kiểm thử

Trước khi sửa, bổ sung các kiểm tra hồi quy tập trung cho:

- Store pause và quota được gọi trong luồng tạo đơn.
- Quota được hoàn ở đúng trạng thái.
- Điểm dùng subtotal.
- Role không hợp lệ không làm login crash.
- Hai form độc lập.
- Ma trận trạng thái rating/complaint.
- Giao diện khách dùng `OrderCode`.
- Layout Admin được kế thừa.

Sau khi code qua build và kiểm tra tự động, chạy lại end-to-end bằng Chrome profile `Codex` theo `BA-Session-Scope-v2.md`, được phép tạo/sửa dữ liệu trong database. Dữ liệu cấu hình tạm dùng để kiểm thử phải được khôi phục khi hoàn tất.

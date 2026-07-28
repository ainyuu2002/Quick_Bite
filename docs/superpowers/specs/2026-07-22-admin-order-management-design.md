# Thiết kế hoàn thiện quản lý đơn hàng Admin

## 1. Mục tiêu

Hoàn thiện phần việc Dev A cho UC08, FR-16 và FR-18 trên nền nhánh `dev` đã có đầy đủ luồng đặt hàng phía khách. Trang `/Admin/Orders` phải cho phép admin xem chi tiết đơn, lọc theo trạng thái và chuyển trạng thái đúng máy trạng thái nghiệp vụ.

Phạm vi không bao gồm SignalR (FR-17, FR-19), Dashboard (FR-20), thay đổi schema hoặc EF Core migration.

## 2. Kiến trúc và ranh giới

- Giữ một Razor Page `/Pages/Admin/Orders/Index` theo Page Model pattern.
- `IndexModel` nhận `AppDbContext` và `OrderService` qua constructor injection.
- `OnGetAsync` chịu trách nhiệm truy vấn đọc: lọc trạng thái, tìm kiếm, sắp xếp, phân trang và tải `Order.Items` cùng tên `MenuItem`.
- `OnPostChangeStatusAsync` chỉ tiếp nhận yêu cầu HTTP, gọi `OrderService.ChangeStatusAsync` và thực hiện Post/Redirect/Get.
- `OrderService.ChangeStatusAsync` là nơi duy nhất kiểm tra và cập nhật máy trạng thái.
- `AppDbContext` tiếp tục đóng vai trò Repository/Unit of Work; không thêm lớp Repository riêng.
- Không sửa `/Hubs`, JavaScript SignalR hoặc trang khách hàng.

## 3. Quy tắc chuyển trạng thái

Service tìm đơn theo ID và xử lý theo BR-01:

- Cho phép tiến đúng một bước: `Pending → Accepted → Preparing → Ready → Completed`, sử dụng `OrderStatus.CanTransitionTo` có sẵn.
- Cho phép chuyển sang `Cancelled` từ `Pending`, `Accepted`, `Preparing` hoặc `Ready`.
- Từ chối hủy `Completed`.
- Từ chối mọi thay đổi đối với `Cancelled`.
- Từ chối giá trị enum không hợp lệ, chuyển lùi hoặc chuyển vượt cấp.
- Báo không tìm thấy nếu ID đơn không tồn tại.
- Chỉ gọi `SaveChangesAsync` sau khi toàn bộ kiểm tra thành công.

Lần triển khai này chỉ lưu trạng thái vào DB. Dev D sẽ gắn việc phát `OrderStatusChanged` qua `IHubContext<OrderHub>` trong phần SignalR riêng.

## 4. Giao diện danh sách và chi tiết

Trang giữ các chức năng hiện có: tab trạng thái, tìm kiếm theo mã đơn/tên/SĐT, sắp xếp và phân trang.

Mỗi dòng hiển thị thông tin tóm tắt và một phần `<details>` mở rộng không phụ thuộc JavaScript. Chi tiết gồm:

- Địa chỉ giao hàng.
- Phương thức thanh toán.
- Ghi chú; nếu trống hiển thị trạng thái rõ ràng.
- Danh sách món với tên, số lượng, đơn giá và thành tiền.
- Tổng cộng để đối chiếu.

Giá lịch sử luôn lấy từ `OrderItem.UnitPrice`; không dùng `MenuItem.Price`. Quan hệ tới `MenuItem` chỉ được dùng để lấy tên món.

Khu hành động hiển thị:

- Một nút chuyển sang trạng thái hợp lệ kế tiếp khi đơn chưa kết thúc.
- Một nút hủy khi đơn chưa `Completed` hoặc `Cancelled`.
- Không hiển thị nút thao tác đối với đơn đã hoàn tất hoặc đã hủy.

## 5. Luồng POST và xử lý lỗi

Form POST sử dụng anti-forgery mặc định của Razor Pages và gửi `orderId`, `nextStatus` cùng trạng thái giao diện cần khôi phục.

1. `OnPostChangeStatusAsync` gọi `OrderService.ChangeStatusAsync`.
2. Thành công: ghi thông báo qua `TempData`, sau đó redirect về tab của trạng thái mới.
3. Lỗi nghiệp vụ hoặc không tìm thấy đơn: ghi thông báo lỗi qua `TempData`, sau đó redirect về tab hiện tại.
4. Các tham số tìm kiếm, sắp xếp và trang được chuẩn hóa trước khi đưa vào redirect để tránh URL hoặc trạng thái giao diện không hợp lệ.

Service vẫn kiểm tra toàn bộ quy tắc ở server; việc ẩn nút trên giao diện không được xem là biện pháp bảo vệ nghiệp vụ.

## 6. Kiểm thử và tiêu chí hoàn thành

Các trường hợp cần được kiểm chứng:

- `Pending → Accepted` thành công.
- `Pending → Ready` bị từ chối và DB không thay đổi.
- Hủy từ `Pending`, `Accepted`, `Preparing` và `Ready` thành công.
- Không thể hủy `Completed` hoặc thay đổi `Cancelled`.
- ID không tồn tại được xử lý an toàn.
- Giá chi tiết lấy từ `OrderItem.UnitPrice`, không bị ảnh hưởng bởi giá hiện tại của món.
- GET lọc đúng tab và tải đủ chi tiết món.
- POST redirect đúng trạng thái, giữ các tham số giao diện hợp lệ và hiển thị thông báo.
- Giá trị `OrderStatus` không hợp lệ bị từ chối ở server.

Hoàn thành khi solution build thành công, toàn bộ kiểm thử hiện có và kiểm thử mới đều qua, diff với `dev` chỉ chứa thay đổi cần thiết cho Dev A cùng tài liệu và test, đồng thời không có migration hoặc phần SignalR mới.

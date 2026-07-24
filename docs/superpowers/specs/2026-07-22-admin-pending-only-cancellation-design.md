# Thiết kế giới hạn hủy đơn Admin ở Pending

## Mục tiêu

Thay đổi chính sách hủy đơn phía admin: admin chỉ được hủy đơn khi trạng thái hiện tại là `Pending`. Từ `Accepted` trở đi, thao tác hủy phải biến mất trên giao diện và bị từ chối tại server nếu gửi POST thủ công.

## Quy tắc nghiệp vụ

- `Pending → Cancelled`: hợp lệ với admin.
- `Accepted → Cancelled`: không hợp lệ.
- `Preparing → Cancelled`: không hợp lệ.
- `Ready → Cancelled`: không hợp lệ.
- `Completed → Cancelled`: không hợp lệ.
- `Cancelled → Cancelled`: không hợp lệ.
- Luồng tiến `Pending → Accepted → Preparing → Ready → Completed` không thay đổi.
- Chính sách khách hàng không thay đổi; khách vốn chỉ được hủy ở `Pending`.

Quy tắc này thay thế hành vi admin đã triển khai trước đó cho phép hủy từ mọi trạng thái chưa kết thúc. Tài liệu SRS/SDS do Dev D sở hữu không nằm trong phạm vi sửa lần này.

## Thiết kế

`OrderStatusExtensions.CanBeCancelledByStaff()` tiếp tục là nguồn chính sách dùng chung giữa UI và service, nhưng chỉ trả `true` khi trạng thái là `Pending`.

`OrderService.ChangeStatusAsync()` không thay đổi cấu trúc: yêu cầu chuyển sang `Cancelled` chỉ được chấp nhận khi `CanBeCancelledByStaff()` trả `true`. Vì vậy request giả từ `Accepted`, `Preparing` hoặc `Ready` bị từ chối trước khi gọi `SaveChangesAsync`.

Trang `/Admin/Orders` tiếp tục dùng cùng extension để quyết định render form hủy. Khi đơn đã `Accepted`, nút hủy không xuất hiện.

## Kiểm thử

Domain test phải chứng minh:

- `Pending.CanBeCancelledByStaff()` là `true`.
- `Accepted`, `Preparing`, `Ready`, `Completed` và `Cancelled` đều là `false`.

Regression test xác nhận service vẫn dùng `CanBeCancelledByStaff()` và view vẫn dùng cùng policy để render form. Build toàn project phải thành công và không có thay đổi ở SignalR, schema hoặc trang khách hàng.

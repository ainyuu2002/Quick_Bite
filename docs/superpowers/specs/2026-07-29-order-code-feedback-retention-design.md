# Khôi phục phản ánh khách vãng lai và gợi ý đăng ký

## Mục tiêu

Khôi phục hai hành vi bị mất sau khi hợp nhất `origin/dev` vào nhánh Order Core v2:

1. Khách vãng lai có thể đánh giá hoặc gửi phản ánh cho đơn đã hoàn tất bằng mã tra cứu.
2. Khách vừa đặt đơn thành công được gợi ý đăng ký tài khoản để nhận điểm và đổi voucher.

## Phạm vi

- Dùng `Order.OrderCode` làm thông tin chứng minh quyền truy cập của khách vãng lai.
- Giữ nguyên cơ chế tra cứu và hủy đơn hiện tại bằng mã đơn.
- Giữ nguyên `ComplaintService`; PageModel phải tra đơn bằng mã rồi truyền `Order.Id` và `Order.Phone` lấy từ server vào service.
- Không thay đổi schema, migration hoặc bộ test hiện có.
- Loại bỏ `OrderTrackingSession` sau khi không còn nơi sử dụng.

## Luồng theo dõi và gợi ý đăng ký

Checkout tiếp tục chuyển hướng tới:

`/Orders/Track?code={OrderCode}&created=true`

`TrackModel` tải đúng một đơn bằng `OrderService.GetByCodeAsync`. Khi `created=true`, hệ thống chỉ hiển thị gợi ý đăng ký nếu:

- tìm thấy đơn;
- khách chưa đăng nhập tài khoản;
- số điện thoại của đơn chưa thuộc tài khoản khách hàng nào.

Banner hiển thị số điểm dự kiến bằng `LoyaltyService.PointsFor(order.Total)` và liên kết tới `/Account/Register` với số điện thoại được điền sẵn. Banner chỉ xuất hiện ngay sau khi đặt hàng, không xuất hiện ở mọi lần tra cứu mã.

## Luồng đánh giá và phản ánh

Khi đơn ở trạng thái `Completed`, trang Track hiển thị liên kết:

`/Orders/Feedback?code={OrderCode}`

Trang lịch sử đơn của thành viên dùng cùng liên kết theo mã đơn thay cho `orderId`.

`FeedbackModel` nhận `code` trên GET và POST. Mỗi request chuẩn hóa mã bằng cách trim và chuyển sang chữ hoa, rồi tải đơn theo `OrderCode`. Nếu mã trống hoặc không tồn tại, chuyển về trang Track.

Các form đánh giá và phản ánh chỉ gửi trường ẩn `code`. PageModel không nhận `orderId` hoặc số điện thoại do client cung cấp. Sau khi tải đơn thành công, PageModel truyền `order.Id` và `order.Phone` lấy từ bản ghi server vào `ComplaintService`.

Sau POST, trang chuyển hướng lại `/Orders/Feedback?code={OrderCode}` để giữ ngữ cảnh. Nút quay lại Track cũng giữ mã đơn.

## An toàn và xử lý lỗi

- Mã đơn đóng vai trò như bearer token, thống nhất với quyền xem và hủy đơn hiện tại.
- Không để client chọn `orderId` hoặc số điện thoại khi gửi phản ánh.
- Các quy tắc nghiệp vụ hiện có của `ComplaintService` vẫn giữ nguyên: chỉ đơn hoàn tất, trong thời hạn 7 ngày, mỗi đơn một đánh giá và một phản ánh.
- Mã không hợp lệ không làm lộ việc đơn thuộc số điện thoại nào; người dùng được đưa về màn hình tra cứu.

## Kiểm thử

Kiểm thử hồi quy tối thiểu phải chứng minh:

1. Trang Track của đơn vừa tạo hiển thị gợi ý đăng ký cho khách chưa có tài khoản.
2. Gợi ý không xuất hiện khi số điện thoại đã đăng ký hoặc khách đã đăng nhập.
3. Link Feedback truyền `OrderCode`, không truyền `orderId`.
4. Feedback tải và gửi dữ liệu bằng mã đơn; `orderId` và số điện thoại dùng cho service đều được lấy từ đơn phía server.
5. Không còn tham chiếu đến `OrderTrackingSession`.

Việc xác minh cuối gồm build toàn solution và chạy các kiểm thử hồi quy liên quan. Không chạy thêm migration hoặc `Update-Database`.

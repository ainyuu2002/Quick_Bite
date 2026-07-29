# QuickBite OrderCore Admin UX Redesign

## Mục tiêu

Cải thiện phân cấp thị giác và khả năng thao tác của các màn hình OrderCore
trong khu Admin, tập trung sâu vào `/Admin/Orders`, `/Admin/Kitchen` và
`/Admin/Shipper`, sau đó áp dụng một lượt nhất quán nhẹ cho các trang Admin còn
lại thông qua hệ thống CSS dùng chung.

Kết quả phải rõ ràng trong cả hai trạng thái: có đơn và không có đơn.

## Phạm vi

### Trong phạm vi

- Cấu trúc trình bày Razor của:
  - `Pages/Admin/Orders/Index.cshtml`
  - `Pages/Admin/Kitchen/Index.cshtml`
  - `Pages/Admin/Shipper/Index.cshtml`
- Hệ thống CSS Admin dùng chung trong `wwwroot/css/admin.css`.
- Bổ sung token cần thiết vào `wwwroot/css/tokens.css` nếu token hiện tại chưa
  diễn đạt được bề mặt, khoảng cách hoặc trạng thái tương tác.
- Cập nhật `design.md` để ghi nhận quy tắc workboard, empty state và action
  hierarchy cho OrderCore.
- Kiểm tra nhất quán trực quan các trang Món ăn, Nhân viên, Doanh thu và SĐT
  cảnh báo; chỉ sửa markup riêng khi CSS dùng chung không đủ.
- Kiểm thử trực tiếp bằng Chrome với tài khoản seed và dữ liệu tạo qua luồng
  người dùng của ứng dụng.

### Ngoài phạm vi

- Không thay đổi `PageModel`, handler, service, SignalR hub, state machine,
  authorization hoặc schema database.
- Không thêm transition mới hoặc viết lại `OrderStatus.CanTransitionTo()`.
- Không thay đổi nội dung nghiệp vụ của lý do hủy, từ chối hoặc giao thất bại.
- Không sửa hoặc ghi đè thay đổi chưa commit trong
  `wwwroot/js/admin-orders.js`.
- Không xóa route, page, component hoặc file production.

## Hệ thống thiết kế

Tiếp tục dùng hệ thống đã khóa trong `design.md`:

- Genre: modern-minimal.
- App macrostructure: Workbench.
- Màu nhấn indigo chỉ dành cho navigation state, focus và hành động chính.
- Manrope cho tiêu đề, Inter cho nội dung và dữ liệu số.
- Spacing theo thang 4px.
- Primary CTA cao tối thiểu 44px; nút nhỏ chỉ dành cho tiện ích phụ không phải
  hành động chuyển trạng thái.
- Destructive action dùng danger-quiet khi chưa xác nhận và chỉ chuyển sang
  danger-solid ở bước xác nhận cuối.
- Chuyển động giới hạn ở opacity, color và transform, có reduced-motion.

## Thiết kế trang Đơn hàng

### Điều hướng trạng thái

Thay dải tab ngang dài bằng ba nhóm có nhãn:

- Đang xử lý: PendingReview, Pending, Confirmed, Preparing, Ready, Delivering.
- Hoàn thành: Completed.
- Ngoại lệ: Cancelled, Rejected, Expired, DeliveryFailed, NoShow.

Tất cả trạng thái vẫn là link GET tới handler và query string hiện tại. Không
thêm logic lọc mới. Trên màn hình nhỏ, các nhóm wrap thành nhiều dòng thay vì
buộc người dùng kéo một thanh cuộn ngang khó nhận biết.

### Toolbar và trạng thái realtime

- Search giữ nguyên GET form và các hidden field hiện tại.
- Tổng số kết quả của trạng thái đang chọn, live indicator và số staff online
  được gom thành một status strip có hierarchy rõ.
- Realtime IDs/data attributes hiện có phải được giữ nguyên để
  `admin-orders.js` tiếp tục hoạt động.

### Trạng thái không có đơn

Nếu `Model.Result.Items` rỗng, không render header bảng và một hàng colspan.
Render một empty-state surface độc lập gồm:

- Biểu tượng CSS/SVG nội tuyến mang tính chức năng, không dùng ảnh stock.
- Tiêu đề theo trạng thái đang chọn.
- Mô tả ngắn rằng danh sách sẽ tự cập nhật khi có đơn phù hợp.
- Gợi ý chuyển sang trạng thái đang xử lý khác, nhưng không tạo CTA giả hoặc
  thay đổi query nghiệp vụ.

### Trạng thái có đơn

- Giữ table vì phù hợp việc quét nhanh trên laptop.
- Làm rõ mã đơn, tổng tiền, thời gian và badge trạng thái.
- Hàng chi tiết vẫn dùng native `details/summary`.
- Metadata và bảng món giữ nguyên dữ liệu nguồn; `UnitPrice` tiếp tục là giá
  chốt của `OrderItem`.

### Action hierarchy

- Transition không cần lý do là primary action, cao tối thiểu 44px.
- Sửa liên hệ là secondary action trong disclosure riêng.
- Từ chối, hủy do sự cố và transition cần lý do được đặt trong vùng
  "Thao tác ngoại lệ".
- Vùng ngoại lệ dùng native `details` để progressive enhancement: dropdown lý
  do và ô "Khác" chỉ hiện khi người dùng chủ động mở.
- `reason-other.js` tiếp tục điều khiển trường "Khác" bằng các data attribute
  hiện tại.
- Nút submit destructive bên trong disclosure là bước xác nhận cuối và được
  phép dùng danger-solid.

## Thiết kế trang Bếp

- Header có title, mô tả, live status và tổng số ticket hiện tại.
- Chia queue thành hai lane dựa trên dữ liệu đã tải:
  - Chờ bắt đầu: `Confirmed`.
  - Đang chuẩn bị: `Preparing`.
- Không thêm query hoặc PageModel property; Razor phân nhóm từ `Model.Queue`.
- Ticket ưu tiên mã đơn, danh sách món/số lượng, ghi chú và primary action.
- Không hiển thị tên, số điện thoại hoặc địa chỉ khách.
- Primary action nằm cuối ticket, có chiều cao tối thiểu 44px.

Nếu cả hai lane rỗng, hiển thị một workboard empty state có thông báo realtime.
Nếu chỉ một lane rỗng, lane đó dùng empty state compact và không chiếm chiều
cao ngang bằng lane có dữ liệu.

## Thiết kế trang Shipper

- Header có title, mô tả, live status và tổng số ticket hiện tại.
- Chia board thành:
  - Chờ nhận giao: `Ready`.
  - Đang giao: `Delivering`.
- Địa chỉ là dữ liệu nổi bật; tên và số điện thoại đứng sau; tổng tiền là dữ
  liệu hỗ trợ.
- `Nhận giao` hoặc `Giao thành công` là primary action.
- `Giao thất bại` nằm trong disclosure phụ. Lý do, trường "Khác" và nút
  danger-solid chỉ hiện sau khi disclosure được mở.

Empty state tuân theo cùng workboard pattern với Bếp nhưng dùng copy theo vai
trò Shipper.

## Nhất quán Admin dùng chung

CSS dùng chung sẽ chuẩn hóa:

- Button height, padding, gap và hierarchy.
- Empty-state surface và compact variant.
- Page header actions/status.
- Details/summary focus, hover và open state.
- Form action grouping.
- Responsive wrap cho toolbar, status navigation và ticket actions.

Các trang Admin ngoài OrderCore chỉ nhận thay đổi markup khi audit trực tiếp
cho thấy shared CSS không thể sửa đúng hierarchy. Không thực hiện refactor
ngoài mục tiêu.

## Responsive và accessibility

- Kiểm tra tại 320, 375, 414, 768 và viewport laptop hiện tại.
- Không có horizontal overflow ở page root.
- Click target chính tối thiểu 44px.
- Text trên button/link không bị wrap thành hai dòng.
- Focus-visible có ring tương phản và xuất hiện ngay lập tức.
- Native `details`, form controls và semantic headings được ưu tiên.
- Empty state dùng text thật, không phụ thuộc chỉ vào icon hoặc màu.
- `prefers-reduced-motion` tiếp tục được hỗ trợ.

## Kế hoạch kiểm thử

### Không có đơn

- Admin Orders ở một trạng thái không có kết quả.
- Kitchen khi không có `Confirmed`/`Preparing`.
- Shipper khi không có `Ready`/`Delivering`.
- Xác minh empty state, realtime copy, chiều cao và responsive.

### Có đơn

Tạo đơn test qua Menu → Cart → Checkout, không chèn dữ liệu giả trực tiếp:

1. Tạo ít nhất một đơn Delivery và một đơn Pickup.
2. Dùng Admin/Staff xác nhận đơn.
3. Kiểm tra ticket xuất hiện ở Kitchen.
4. Chuyển `Confirmed → Preparing → Ready`.
5. Với Delivery, kiểm tra ticket xuất hiện ở Shipper và chuyển
   `Ready → Delivering`.
6. Kiểm tra action chính và disclosure giao thất bại.
7. Kiểm tra một đơn cần từ chối/hủy để xác minh reason disclosure trên Orders.
8. Không submit destructive transition nếu việc đó không cần thiết để xác minh
   layout; nếu cần submit, chỉ dùng đơn test vừa tạo.

### Regression

- Build project.
- Chạy các regression test hiện có liên quan Order/Admin.
- Xác minh SignalR data hooks không bị mất.
- Xác minh `reason-other.js` vẫn ẩn/hiện trường lý do khác.
- Xác minh các trang Admin còn lại không bị vỡ bởi shared CSS.

## Tiêu chí hoàn thành

- Orders, Kitchen và Shipper đều có bố cục hữu ích khi có và không có đơn.
- Primary action nổi bật hơn secondary/destructive actions.
- Reason controls không hiển thị thường trực cạnh primary action.
- Không có thay đổi nghiệp vụ hoặc schema.
- Không ghi đè thay đổi hiện có trong `admin-orders.js`.
- Build và regression tests pass.
- Chrome xác minh desktop và các breakpoint mobile bắt buộc.

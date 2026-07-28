# Thiết kế đồng bộ PaymentMethod trong script Database First

## Mục tiêu

Cập nhật `D:/PRN222_Project/QuickBite-Data/script.sql` để database QuickBite được tạo mới có cột `Orders.PaymentMethod`, khớp với `Order.PaymentMethod`, `PaymentMethod` enum và cấu hình hiện có trong `AppDbContext`.

## Phạm vi

- Giữ nguyên hành vi hiện tại: script xóa bảng cũ, tạo lại schema và seed dữ liệu mẫu.
- Không bảo toàn dữ liệu của database đang tồn tại.
- Không tạo hoặc chạy EF Core migration.
- Không sửa model, DbContext hoặc mã ứng dụng.

## Thay đổi schema và seed

Trong `CREATE TABLE Orders`, thêm:

```sql
PaymentMethod INT NOT NULL DEFAULT 0
```

Quy ước giá trị được ghi ngay trong comment của schema:

- `0`: tiền mặt (`PaymentMethod.Cash`).
- `1`: chuyển khoản (`PaymentMethod.BankTransfer`).

Lệnh seed `Orders` bổ sung `PaymentMethod` vào danh sách cột và gán giá trị rõ ràng cho cả bảy đơn. Dữ liệu mẫu sử dụng cả `0` và `1` để hai phương thức đều có thể được kiểm tra trên giao diện.

## Xác minh

Phần kiểm tra cuối script bổ sung truy vấn xác nhận:

- `Orders.PaymentMethod` tồn tại.
- Không có giá trị ngoài `0` và `1`.
- Có dữ liệu mẫu cho cả tiền mặt và chuyển khoản.

Script được chạy toàn bộ trên SQL Server local. Sau khi chạy, trang `/Admin/Orders` phải truy vấn được dữ liệu mà không còn lỗi `Invalid column name 'PaymentMethod'`.

## Ranh giới lưu trữ

`QuickBite-Data/script.sql` nằm ngoài git repo con `Quick_Bite`. Tài liệu thiết kế được commit trên nhánh `dev-a/admin-orders-complete`, còn bản sửa script chỉ nằm trong workspace cha trừ khi người dùng đưa thư mục `QuickBite-Data` vào một repository được quản lý riêng.

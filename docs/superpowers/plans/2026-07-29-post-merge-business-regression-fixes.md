# Kế hoạch triển khai sửa lỗi nghiệp vụ sau merge

> **Yêu cầu thực thi:** áp dụng TDD theo từng nhóm nhỏ; luôn chạy kiểm tra đỏ trước khi sửa và chạy lại sau khi sửa.

**Mục tiêu:** Sửa 7 lỗi nghiệp vụ phát hiện qua Chrome QA và lỗi layout Admin liên quan, giữ nguyên kiến trúc service/PageModel hiện có.

**Kiến trúc:** `OrderService` điều phối kiểm tra trạng thái quán và quota qua hai interface Operations đã có. Các quy tắc thuần về trạng thái phản hồi/quota release được đặt gần domain để kiểm thử trực tiếp. Các PageModel chỉ nhận dữ liệu của đúng form đang submit. Dữ liệu role dư được chuẩn hóa bằng SQL idempotent và login xử lý phòng vệ.

**Công nghệ:** ASP.NET Core Razor Pages, EF Core SQL Server, SignalR, console domain regression tests, PowerShell source-regression checks, Chrome E2E.

---

## Task 1: Thêm kiểm tra hồi quy đỏ

**Files:**

- Modify: `tests/QuickBite.DomainTests/Program.cs`
- Create: `tests/PostMergeBusinessRegression.ps1`

**Steps:**

1. Thêm assertion cho:
   - `Completed` được rating và complaint.
   - `Rejected`/`DeliveryFailed` chỉ được complaint.
   - Các trạng thái còn lại không được feedback.
   - Quota chỉ được release với `Cancelled`, `Rejected`, `Expired`.
   - Điểm từ subtotal không bao gồm delivery fee.
2. Thêm source-regression check xác nhận:
   - `OrderService` inject/call store và menu availability.
   - Có compensation release khi create lỗi và release sau terminal transition hợp lệ.
   - Login không còn `throw` với role không hợp lệ.
   - Hai handler form không phụ thuộc ModelState của form còn lại.
   - Public views không còn `Đơn #@order.Id`.
   - `/Pages/Admin/_ViewStart.cshtml` tồn tại.
3. Chạy `dotnet run --project tests/QuickBite.DomainTests/QuickBite.DomainTests.csproj` và script PowerShell; xác nhận thất bại đúng nguyên nhân.

## Task 2: Tích hợp store availability và quota vào OrderService

**Files:**

- Modify: `QuickBite/Services/OrderService.cs`
- Modify: `QuickBite/Models/OrderStatus.cs`

**Steps:**

1. Thêm `ShouldReleaseQuota()` vào `OrderStatusExtensions`.
2. Inject `IStoreAvailabilityService` và `IMenuAvailabilityService`.
3. Trong `CreateOrderAsync`, kiểm tra `GetStatusAsync`; trả `OrderValidationException` chứa lý do tạm nghỉ/ngoài giờ.
4. Sau khi validate đầy đủ request và trước khi tạo order, gọi `TryReserveAsync`.
5. Bao phần tạo order/discount commit bằng `try/catch`; nếu đã reserve mà thất bại thì gọi `ReleaseAsync` theo ngày reserve rồi rethrow.
6. Load `Order.Items` trong `CancelByCodeAsync` và `ChangeStatusAsync`; release quota sau khi lưu transition sang `Cancelled`, `Rejected`, `Expired`.
7. Chạy test Task 1 và build.

## Task 3: Sửa quy tắc tích điểm và OrderCode

**Files:**

- Modify: `QuickBite/Services/LoyaltyService.cs`
- Modify: `QuickBite/Pages/Orders/Track.cshtml.cs`
- Modify: `QuickBite/Pages/Account/Orders.cshtml`
- Modify: `QuickBite/Pages/Orders/Feedback.cshtml`

**Steps:**

1. Đổi điểm cộng và điểm gợi ý đăng ký sang `PointsFor(order.Subtotal)`.
2. Đổi ghi chú ledger mới sang `Đơn {order.OrderCode} hoàn tất`.
3. Đổi tiêu đề lịch sử đơn và feedback sang `OrderCode`.
4. Chạy test và kiểm tra không còn public interpolation của `Order.Id`.

## Task 4: Sửa chính sách complaint/rating

**Files:**

- Modify: `QuickBite/Models/OrderStatus.cs`
- Modify: `QuickBite/Services/ComplaintService.cs`
- Modify: `QuickBite/Pages/Orders/Feedback.cshtml`
- Modify: `QuickBite/Pages/Orders/Track.cshtml`
- Modify: `QuickBite/Pages/Account/Orders.cshtml`

**Steps:**

1. Thêm `CanReceiveRating()` và `CanReceiveComplaint()` vào domain extension.
2. Tách load eligibility của ComplaintService theo loại tác vụ, vẫn kiểm tra số điện thoại và cửa sổ 7 ngày.
3. Hiển thị form rating chỉ khi `CanReceiveRating`.
4. Hiển thị form/link complaint khi `CanReceiveComplaint`.
5. Chạy domain và source regression tests.

## Task 5: Sửa role manager và dữ liệu

**Files:**

- Modify: `QuickBite/Pages/Admin/Login.cshtml.cs`
- Create: `QuickBite-Data/patches/2026-07-29-normalize-manager-role.sql`

**Steps:**

1. Tách mapping `AccountRole` → claim role bằng `Try...`; role không hợp lệ thêm ModelState error và không sign-in.
2. Viết SQL idempotent:

   ```sql
   UPDATE Accounts
   SET Role = 0
   WHERE Username = 'manager' AND Role = 4;
   ```

3. Chạy script trên database hiện tại bằng kết nối cấu hình của app.
4. Truy vấn xác nhận `manager.Role = 0`.
5. Đăng nhập lại manager bằng Chrome.

## Task 6: Sửa form quản trị và layout

**Files:**

- Modify: `QuickBite/Pages/Admin/Operations/Store.cshtml.cs`
- Modify: `QuickBite/Pages/Admin/Operations/Store.cshtml`
- Modify: `QuickBite/Pages/Admin/Ingredients/Index.cshtml.cs`
- Modify: `QuickBite/Pages/Admin/Ingredients/Index.cshtml`
- Create: `QuickBite/Pages/Admin/_ViewStart.cshtml`

**Steps:**

1. Đổi `OnPostPauseAsync` nhận đúng `pauseReason`; không validate `BusinessHours`.
2. Đổi `OnPostCreateAsync` nhận đúng `ingredientName`; không validate `Links`.
3. Giữ validation server, thêm validation summary/inline message.
4. Thêm Admin `_ViewStart`; xác nhận Login tiếp tục đặt `_AdminAuthLayout`.
5. Chạy test, build và submit cả hai form bằng Chrome.

## Task 7: Kiểm chứng toàn bộ

**Files:**

- Verify: `BA-Session-Scope-v2.md`
- Update: `QA-Session-Report-2026-07-29.md`

**Steps:**

1. Chạy:
   - domain regression console;
   - PowerShell regression scripts;
   - `dotnet build QuickBite/QuickBite.csproj`.
2. Chạy app bằng cấu hình Development.
3. Dùng Chrome profile `Codex` test:
   - quán pause chặn checkout;
   - sale window/quota chặn và reserve;
   - cancel/reject/expire hoàn quota;
   - delivery completed cộng điểm không gồm phí giao;
   - manager login;
   - tạo nguyên liệu;
   - pause/resume store;
   - complaint trên Completed/Rejected/DeliveryFailed, rating chỉ Completed;
   - public UI chỉ dùng OrderCode;
   - toàn bộ trang Admin dùng đúng layout.
4. Khôi phục store/quota/promotion và dữ liệu cấu hình tạm; giữ dữ liệu QA có ích.
5. Cập nhật báo cáo với bằng chứng pass/fail cuối cùng.

# Order-code Feedback and Retention Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Khôi phục gợi ý đăng ký sau checkout và cho phép khách vãng lai đánh giá/phản ánh đơn hoàn tất bằng mã tra cứu.

**Architecture:** `OrderCode` tiếp tục là bearer token duy nhất của luồng khách vãng lai. Razor Page tải dữ liệu từ server bằng mã đơn, không nhận `orderId` hoặc số điện thoại từ form; `ComplaintService` giữ nguyên và chỉ nhận dữ liệu đã được PageModel suy ra từ bản ghi đơn.

**Tech Stack:** ASP.NET Core 8 Razor Pages, EF Core 8, SQL Server, PowerShell HTTP smoke test.

## Global Constraints

- Không thay đổi schema, migration hoặc chạy `Update-Database`.
- Không sửa `ComplaintService`, `OrderService` hoặc bộ `tests/QuickBite.DomainTests`.
- Không thêm Repository; PageModel tiếp tục dùng DI qua constructor.
- Không đổi cơ chế SignalR `order-{OrderCode}` hiện tại.
- Dùng Visual Studio 2026 Enterprise nếu người dùng cần thao tác thủ công.

---

### Task 1: Chứng minh hai hồi quy trên ứng dụng hiện tại

**Files:**
- Không tạo hoặc sửa file.

**Interfaces:**
- Consumes: `GET /Orders/Track?code={code}&created=true`, `GET /Orders/Feedback?code={code}`.
- Produces: bằng chứng RED trước khi sửa.

- [ ] **Step 1: Lấy một mã đơn khách vãng lai đã hoàn tất**

Run:

```powershell
sqlcmd -S . -d QuickBite -E -Q "SET NOCOUNT ON; SELECT TOP (1) OrderCode FROM Orders WHERE Status = 4 AND CustomerId IS NULL ORDER BY Id DESC;"
```

Expected: nhận được một mã dạng `QB-XXXXXX`; trên máy hiện tại có `QB-K4GSYA`.

- [ ] **Step 2: Chạy ứng dụng ở cổng test riêng**

Run:

```powershell
dotnet run --project QuickBite/QuickBite.csproj --no-launch-profile --urls http://127.0.0.1:5088
```

Expected: ứng dụng lắng nghe tại `http://127.0.0.1:5088`.

- [ ] **Step 3: Xác nhận banner đăng ký đang thiếu**

Run trong PowerShell khác:

```powershell
$track = Invoke-WebRequest "http://127.0.0.1:5088/Orders/Track?code=QB-K4GSYA&created=true" -UseBasicParsing
if ($track.Content -notmatch "Tạo tài khoản") {
    throw "RED: trang Track chưa khôi phục gợi ý đăng ký."
}
```

Expected: FAIL với thông báo `RED: trang Track chưa khôi phục gợi ý đăng ký.`

- [ ] **Step 4: Xác nhận Feedback bằng mã đơn đang bị từ chối**

Run:

```powershell
$feedback = Invoke-WebRequest "http://127.0.0.1:5088/Orders/Feedback?code=QB-K4GSYA" -UseBasicParsing -MaximumRedirection 0 -SkipHttpErrorCheck
if ($feedback.StatusCode -ne 200) {
    throw "RED: Feedback chưa tải được bằng mã đơn."
}
```

Expected: FAIL vì handler hiện tại vẫn cần `orderId` và session số điện thoại.

---

### Task 2: Khôi phục gợi ý đăng ký trên trang Track

**Files:**
- Modify: `QuickBite/Pages/Orders/Track.cshtml.cs`
- Modify: `QuickBite/Pages/Orders/Track.cshtml`

**Interfaces:**
- Consumes: `CustomerAccountService.IsPhoneRegisteredAsync`, `CustomerAuth.GetCustomerIdAsync`, `LoyaltyService.PointsFor`.
- Produces: `SuggestRegistration`, `PotentialPoints`, `RegistrationPhone`.

- [ ] **Step 1: Inject dịch vụ tài khoản và thêm trạng thái trình bày**

Trong `TrackModel`, thêm dependency và các property:

```csharp
private readonly CustomerAccountService _accounts;

public TrackModel(OrderService orderService, CustomerAccountService accounts)
{
    _orderService = orderService;
    _accounts = accounts;
}

public bool SuggestRegistration { get; private set; }

public int PotentialPoints { get; private set; }

public string RegistrationPhone { get; private set; } = string.Empty;
```

- [ ] **Step 2: Tính gợi ý chỉ sau checkout thành công**

Ngay sau khi tải `Order`, thêm:

```csharp
if (Created && Order is not null)
{
    var isSignedIn = await CustomerAuth.GetCustomerIdAsync(HttpContext) is not null;
    var isRegistered = await _accounts.IsPhoneRegisteredAsync(
        Order.Phone,
        cancellationToken);

    if (!isSignedIn && !isRegistered)
    {
        SuggestRegistration = true;
        RegistrationPhone = Order.Phone;
        PotentialPoints = LoyaltyService.PointsFor(Order.Total);
    }
}
```

Không tính banner nếu không có `created=true`, mã không tồn tại, khách đã đăng nhập hoặc số điện thoại đã đăng ký.

- [ ] **Step 3: Khôi phục banner trong Razor view**

Đặt banner ngay sau thông báo đặt hàng thành công:

```cshtml
@if (Model.SuggestRegistration)
{
    <div class="alert alert-warning d-flex flex-wrap justify-content-between align-items-center gap-2">
        <span>
            ⭐ Đơn này đáng <strong>@Model.PotentialPoints điểm</strong> —
            tạo tài khoản để tích điểm, đổi voucher và đặt lại món chỉ với một chạm.
        </span>
        <a asp-page="/Account/Register"
           asp-route-phone="@Model.RegistrationPhone"
           class="btn btn-sm btn-primary">Tạo tài khoản</a>
    </div>
}
```

- [ ] **Step 4: Chạy lại smoke test banner**

Run lại lệnh ở Task 1 Step 3.

Expected: PASS; response chứa `Tạo tài khoản`.

- [ ] **Step 5: Kiểm tra nhánh không hiển thị**

Run:

```powershell
$ordinaryTrack = Invoke-WebRequest "http://127.0.0.1:5088/Orders/Track?code=QB-K4GSYA" -UseBasicParsing
if ($ordinaryTrack.Content -match "Tạo tài khoản") {
    throw "Banner không được xuất hiện ở lần tra cứu thông thường."
}
```

Expected: PASS.

---

### Task 3: Rewire Feedback hoàn toàn theo mã đơn

**Files:**
- Modify: `QuickBite/Pages/Orders/Feedback.cshtml.cs`
- Modify: `QuickBite/Pages/Orders/Feedback.cshtml`
- Modify: `QuickBite/Pages/Orders/Track.cshtml`
- Modify: `QuickBite/Pages/Account/Orders.cshtml`
- Delete: `QuickBite/Services/OrderTrackingSession.cs`

**Interfaces:**
- Consumes: `AppDbContext.Orders`, `ComplaintService.RateAsync(int, string, int, CancellationToken)`, `ComplaintService.SubmitAsync(int, string, ComplaintCategory, string, CancellationToken)`.
- Produces: `/Orders/Feedback?code={OrderCode}` cho GET và trường form `Code` cho POST.

- [ ] **Step 1: Thay quyền truy cập session bằng mã đơn trong `FeedbackModel`**

Thay constructor bằng:

```csharp
public FeedbackModel(AppDbContext db, ComplaintService complaints)
{
    _db = db;
    _complaints = complaints;
}
```

Thêm property:

```csharp
[BindProperty(SupportsGet = true)]
public string? Code { get; set; }
```

Đổi GET thành:

```csharp
public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
{
    return await LoadAsync(Code, cancellationToken)
        ? Page()
        : RedirectToPage("/Orders/Track");
}
```

- [ ] **Step 2: Tải đơn và dữ liệu phản hồi bằng mã phía server**

Thay `LoadAsync` bằng:

```csharp
private async Task<bool> LoadAsync(string? code, CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(code))
    {
        return false;
    }

    var normalizedCode = code.Trim().ToUpperInvariant();
    var order = await _db.Orders
        .AsNoTracking()
        .SingleOrDefaultAsync(
            item => item.OrderCode == normalizedCode,
            cancellationToken);
    if (order is null)
    {
        return false;
    }

    Code = order.OrderCode;
    Order = order;
    ExistingRating = await _db.InternalRatings
        .AsNoTracking()
        .SingleOrDefaultAsync(rating => rating.OrderId == order.Id, cancellationToken);
    ExistingComplaint = await _db.Complaints
        .AsNoTracking()
        .SingleOrDefaultAsync(complaint => complaint.OrderId == order.Id, cancellationToken);
    return true;
}
```

Xóa `CustomerAccountService`, `ResolvePhoneAsync` và mọi tham chiếu `OrderTrackingSession`.

- [ ] **Step 3: Sửa hai POST handler để không tin `orderId` hoặc số điện thoại từ client**

Handler đánh giá:

```csharp
public async Task<IActionResult> OnPostRateAsync(
    int stars,
    CancellationToken cancellationToken)
{
    if (!await LoadAsync(Code, cancellationToken))
    {
        return RedirectToPage("/Orders/Track");
    }

    try
    {
        await _complaints.RateAsync(
            Order.Id,
            Order.Phone,
            stars,
            cancellationToken);
        Message = "Cảm ơn bạn đã đánh giá!";
    }
    catch (CustomerFlowException exception)
    {
        ErrorMessage = exception.Message;
    }

    return RedirectToPage(new { code = Code });
}
```

Handler phản ánh:

```csharp
public async Task<IActionResult> OnPostComplainAsync(
    ComplaintCategory category,
    string description,
    CancellationToken cancellationToken)
{
    if (!await LoadAsync(Code, cancellationToken))
    {
        return RedirectToPage("/Orders/Track");
    }

    if (string.IsNullOrWhiteSpace(description))
    {
        ErrorMessage = "Vui lòng mô tả vấn đề bạn gặp phải.";
        return RedirectToPage(new { code = Code });
    }

    try
    {
        await _complaints.SubmitAsync(
            Order.Id,
            Order.Phone,
            category,
            description,
            cancellationToken);
        Message = "Đã gửi phản ánh. Quán sẽ liên hệ lại với bạn sớm nhất.";
    }
    catch (CustomerFlowException exception)
    {
        ErrorMessage = exception.Message;
    }

    return RedirectToPage(new { code = Code });
}
```

- [ ] **Step 4: Đổi toàn bộ route và hidden field sang `OrderCode`**

Trong `Feedback.cshtml`:

```cshtml
<a asp-page="/Orders/Track"
   asp-route-code="@Model.Order.OrderCode"
   class="btn btn-outline-secondary">Quay lại theo dõi đơn</a>

<input type="hidden" asp-for="Code" />
```

Hai form đều dùng hidden field `Code`; xóa hidden field `orderId`.

Trong `Track.cshtml`, sau phần hủy đơn:

```cshtml
@if (order.Status == OrderStatus.Completed)
{
    <div class="mb-4">
        <a asp-page="/Orders/Feedback"
           asp-route-code="@order.OrderCode"
           class="btn btn-outline-primary">Đánh giá / Phản ánh đơn này</a>
    </div>
}
```

Trong `Account/Orders.cshtml`:

```cshtml
<a asp-page="/Orders/Feedback"
   asp-route-code="@order.OrderCode"
   class="btn btn-sm btn-outline-primary">Đánh giá / Phản ánh</a>
```

- [ ] **Step 5: Xóa helper session đã lỗi thời**

Delete:

```text
QuickBite/Services/OrderTrackingSession.cs
```

- [ ] **Step 6: Chạy lại smoke test Feedback**

Run lại Task 1 Step 4.

Expected: PASS với HTTP 200 và response hiển thị trang đánh giá/phản ánh của `QB-K4GSYA`.

---

### Task 4: Xác minh toàn bộ thay đổi

**Files:**
- Verify all modified files from Tasks 2–3.

**Interfaces:**
- Consumes: project build, local SQL Server, customer Razor Pages.
- Produces: bằng chứng build và hai luồng hồi quy hoạt động.

- [ ] **Step 1: Xác nhận không còn contract session cũ**

Run:

```powershell
rg -n "OrderTrackingSession|TrackedOrderPhone|asp-route-orderId" QuickBite/Pages QuickBite/Services
```

Expected: không có kết quả liên quan tới Feedback hoặc tracking.

- [ ] **Step 2: Kiểm tra diff**

Run:

```powershell
git diff --check
git status --short
```

Expected: không có whitespace error; chỉ các file đúng phạm vi bị thay đổi.

- [ ] **Step 3: Build project**

Run:

```powershell
dotnet build QuickBite/QuickBite.csproj --no-restore
```

Expected: build thành công, 0 lỗi.

- [ ] **Step 4: Chạy hai smoke test xanh**

Chạy lại các HTTP assertion ở Task 2 Step 4–5 và Task 3 Step 6.

Expected: tất cả PASS; không ghi hoặc sửa dữ liệu đơn hàng.

- [ ] **Step 5: Commit implementation**

```powershell
git add QuickBite/Pages/Orders/Track.cshtml.cs QuickBite/Pages/Orders/Track.cshtml QuickBite/Pages/Orders/Feedback.cshtml.cs QuickBite/Pages/Orders/Feedback.cshtml QuickBite/Pages/Account/Orders.cshtml QuickBite/Services/OrderTrackingSession.cs
git commit -m "fix: restore guest feedback and signup prompt"
```

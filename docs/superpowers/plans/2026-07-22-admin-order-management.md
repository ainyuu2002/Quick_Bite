# Admin Order Management Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hoàn thiện UC08, FR-16 và FR-18 để admin xem chi tiết đơn và chuyển trạng thái an toàn từ `/Admin/Orders`.

**Architecture:** Razor Page xử lý HTTP và truy vấn đọc; `OrderService` là nơi duy nhất kiểm tra và ghi trạng thái. Chính sách hủy admin nằm trong `OrderStatusExtensions`, tái sử dụng `CanTransitionTo` cho luồng tiến một bước.

**Tech Stack:** ASP.NET Core 8 Razor Pages, EF Core 8 SQL Server, C# 12, PowerShell regression checks, HTML `<details>`, CSS Admin hiện có.

## Global Constraints

- Chỉ triển khai phần Dev A cho UC08, FR-16 và FR-18.
- Không sửa `/Hubs`, JavaScript SignalR, trang khách hàng, schema hoặc EF Core migration.
- Không thêm Repository; dùng `AppDbContext` như Repository/Unit of Work.
- Dependency đi qua constructor injection.
- Giá lịch sử lấy từ `OrderItem.UnitPrice`, không lấy `MenuItem.Price`.
- `OrderService.ChangeStatusAsync` kiểm tra toàn bộ quy tắc ở server.

## File map

- Modify `QuickBite/Models/OrderStatus.cs`: chính sách hủy admin.
- Modify `QuickBite/Services/OrderService.cs`: `ChangeStatusAsync`.
- Modify `QuickBite/Pages/Admin/Orders/Index.cshtml.cs`: tải chi tiết và POST handler.
- Modify `QuickBite/Pages/Admin/Orders/Index.cshtml`: chi tiết, thông báo, nút thao tác.
- Modify `QuickBite/wwwroot/css/admin.css`: style chi tiết responsive.
- Create `tests/QuickBite.DomainTests/*`: test domain không cần NuGet mới.
- Create `tests/AdminOrderManagementRegression.ps1`: regression wiring/UI.

---

### Task 1: Chính sách và service chuyển trạng thái

**Files:**
- Create: `tests/QuickBite.DomainTests/QuickBite.DomainTests.csproj`
- Create: `tests/QuickBite.DomainTests/Program.cs`
- Modify: `QuickBite/Models/OrderStatus.cs`
- Modify: `QuickBite/Services/OrderService.cs`

**Interfaces:**
- Consumes: `OrderStatus.CanTransitionTo(OrderStatus)` và `AppDbContext.Orders`.
- Produces: `CanBeCancelledByStaff()` và `ChangeStatusAsync(int, OrderStatus, CancellationToken)`.

- [ ] **Step 1: Viết test domain thất bại**

`QuickBite.DomainTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup>
  <ItemGroup><ProjectReference Include="../../QuickBite/QuickBite.csproj" /></ItemGroup>
</Project>
```

`Program.cs`:

```csharp
using QuickBite.Models;
static void Assert(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
Assert(OrderStatus.Pending.CanTransitionTo(OrderStatus.Accepted), "Pending -> Accepted phải hợp lệ.");
Assert(!OrderStatus.Pending.CanTransitionTo(OrderStatus.Ready), "Không được vượt cấp.");
foreach (var status in new[] { OrderStatus.Pending, OrderStatus.Accepted, OrderStatus.Preparing, OrderStatus.Ready })
    Assert(status.CanBeCancelledByStaff(), $"Admin phải hủy được {status}.");
Assert(!OrderStatus.Completed.CanBeCancelledByStaff(), "Không được hủy Completed.");
Assert(!OrderStatus.Cancelled.CanBeCancelledByStaff(), "Không được hủy lại Cancelled.");
Console.WriteLine("Admin order domain tests passed.");
```

- [ ] **Step 2: Xác nhận test fail**

Run: `dotnet run --project tests/QuickBite.DomainTests/QuickBite.DomainTests.csproj`

Expected: compile fail vì `CanBeCancelledByStaff` chưa tồn tại.

- [ ] **Step 3: Thêm chính sách hủy**

```csharp
public static bool CanBeCancelledByStaff(this OrderStatus current)
    => current is OrderStatus.Pending or OrderStatus.Accepted
        or OrderStatus.Preparing or OrderStatus.Ready;
```

- [ ] **Step 4: Xác nhận test pass**

Run: `dotnet run --project tests/QuickBite.DomainTests/QuickBite.DomainTests.csproj`

Expected: `Admin order domain tests passed.`

- [ ] **Step 5: Thêm service method**

```csharp
public async Task<Order> ChangeStatusAsync(int orderId, OrderStatus nextStatus,
    CancellationToken cancellationToken = default)
{
    if (!Enum.IsDefined(nextStatus))
        throw new OrderValidationException("Trạng thái đơn hàng không hợp lệ.");

    var order = await _db.Orders.SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken)
        ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");
    var valid = nextStatus == OrderStatus.Cancelled
        ? order.Status.CanBeCancelledByStaff()
        : order.Status.CanTransitionTo(nextStatus);
    if (!valid)
        throw new OrderValidationException($"Không thể chuyển đơn từ {order.Status.ToDisplayText()} sang {nextStatus.ToDisplayText()}.");

    order.Status = nextStatus;
    await _db.SaveChangesAsync(cancellationToken);
    return order;
}
```

- [ ] **Step 6: Build, test và commit**

Run: `dotnet build QuickBite/QuickBite.csproj --no-restore`

Run: `dotnet run --project tests/QuickBite.DomainTests/QuickBite.DomainTests.csproj`

Commit: `git commit -m "feat: add admin order status transitions"`

---

### Task 2: PageModel tải chi tiết và xử lý POST

**Files:**
- Create: `tests/AdminOrderManagementRegression.ps1`
- Modify: `QuickBite/Pages/Admin/Orders/Index.cshtml.cs`

**Interfaces:**
- Consumes: `OrderService.ChangeStatusAsync`.
- Produces: `OnPostChangeStatusAsync`; `Result.Items` có `Items.MenuItem`.

- [ ] **Step 1: Viết regression test thất bại**

```powershell
$ErrorActionPreference = "Stop"
function Assert-Match([string]$Text, [string]$Pattern, [string]$Message) { if ($Text -notmatch $Pattern) { throw $Message } }
$page = Get-Content -Raw "QuickBite/Pages/Admin/Orders/Index.cshtml.cs"
$view = Get-Content -Raw "QuickBite/Pages/Admin/Orders/Index.cshtml"
$service = Get-Content -Raw "QuickBite/Services/OrderService.cs"
Assert-Match $page '\.Include\(o\s*=>\s*o\.Items\)' "GET chưa tải Order.Items."
Assert-Match $page '\.ThenInclude\(item\s*=>\s*item\.MenuItem\)' "GET chưa tải MenuItem."
Assert-Match $page 'OnPostChangeStatusAsync' "Thiếu POST handler."
Assert-Match $page '_orderService\.ChangeStatusAsync' "Handler chưa gọi service."
Assert-Match $view 'asp-page-handler="ChangeStatus"' "View thiếu form đổi trạng thái."
Assert-Match $view 'item\.UnitPrice' "View chưa dùng UnitPrice đã chốt."
Assert-Match $service 'Enum\.IsDefined\(nextStatus\)' "Service chưa chặn enum giả."
Write-Output "Admin order management regression checks passed."
```

- [ ] **Step 2: Xác nhận test fail**

Run: `powershell -ExecutionPolicy Bypass -File tests/AdminOrderManagementRegression.ps1`

Expected: fail vì GET/handler/view chưa hoàn chỉnh.

- [ ] **Step 3: Inject service và tải navigation**

```csharp
private readonly AppDbContext _context;
private readonly OrderService _orderService;
public IndexModel(AppDbContext context, OrderService orderService)
{ _context = context; _orderService = orderService; }

var query = _context.Orders.AsNoTracking()
    .Include(o => o.Items).ThenInclude(item => item.MenuItem)
    .Where(o => o.Status == status);
```

- [ ] **Step 4: Thêm POST handler**

```csharp
public async Task<IActionResult> OnPostChangeStatusAsync(int orderId, OrderStatus nextStatus,
    OrderStatus currentStatus = OrderStatus.Pending, string? search = null,
    string sortBy = "date", string sortDir = "desc", int pageNumber = 1)
{
    currentStatus = Enum.IsDefined(currentStatus) ? currentStatus : OrderStatus.Pending;
    sortBy = sortBy == "total" ? "total" : "date";
    sortDir = sortDir == "asc" ? "asc" : "desc";
    pageNumber = Math.Max(1, pageNumber);
    try
    {
        var order = await _orderService.ChangeStatusAsync(orderId, nextStatus);
        TempData["SuccessMessage"] = $"Đơn #{order.Id} đã chuyển sang {order.Status.ToDisplayText()}.";
        return RedirectToPage(new { status = order.Status, search, sortBy, sortDir, pageNumber = 1 });
    }
    catch (Exception ex) when (ex is OrderValidationException or KeyNotFoundException)
    { TempData["ErrorMessage"] = ex.Message; }
    return RedirectToPage(new { status = currentStatus, search, sortBy, sortDir, pageNumber });
}
```

- [ ] **Step 5: Chạy regression tới lỗi view**

Expected: PageModel checks qua, view checks còn fail.

---

### Task 3: Chi tiết đơn và nút thao tác

**Files:**
- Modify: `QuickBite/Pages/Admin/Orders/Index.cshtml`
- Modify: `QuickBite/wwwroot/css/admin.css`

**Interfaces:**
- Consumes: `Order.Items`, `OrderItem.UnitPrice`, handler `ChangeStatus`.
- Produces: dòng `<details>`, form chuyển/hủy, success/error alerts.

- [ ] **Step 1: Render thông báo**

```cshtml
@if (TempData["SuccessMessage"] is string success) { <div class="alert alert--success" role="status">@success</div> }
@if (TempData["ErrorMessage"] is string error) { <div class="alert alert--danger" role="alert">@error</div> }
```

- [ ] **Step 2: Render dòng chi tiết sau mỗi dòng tóm tắt**

```cshtml
<tr class="order-details-row"><td colspan="6">
<details class="order-details"><summary>Xem chi tiết và xử lý</summary>
<div class="order-details__content">
<dl class="order-meta">
<div><dt>Địa chỉ</dt><dd>@order.Address</dd></div>
<div><dt>Thanh toán</dt><dd>@order.PaymentMethod.ToDisplayText()</dd></div>
<div><dt>Ghi chú</dt><dd>@(string.IsNullOrWhiteSpace(order.Note) ? "Không có" : order.Note)</dd></div>
</dl>
<table class="order-items-table"><thead><tr><th>Món</th><th>SL</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead><tbody>
@foreach (var item in order.Items) { <tr><td>@(item.MenuItem?.Name ?? $"Món #{item.MenuItemId}")</td><td>@item.Quantity</td><td>@item.UnitPrice.ToString("N0") đ</td><td>@item.LineTotal.ToString("N0") đ</td></tr> }
</tbody><tfoot><tr><th colspan="3">Tổng cộng</th><th>@order.Total.ToString("N0") đ</th></tr></tfoot></table>
<div class="order-actions">
@if (order.Status is >= OrderStatus.Pending and < OrderStatus.Completed) {
var next = (OrderStatus)((int)order.Status + 1);
<form method="post" asp-page-handler="ChangeStatus"><input type="hidden" name="orderId" value="@order.Id" /><input type="hidden" name="nextStatus" value="@next" /><input type="hidden" name="currentStatus" value="@Model.CurrentStatus" /><button class="btn btn--primary btn--sm">Chuyển sang @next.ToDisplayText()</button></form> }
@if (order.Status.CanBeCancelledByStaff()) {
<form method="post" asp-page-handler="ChangeStatus"><input type="hidden" name="orderId" value="@order.Id" /><input type="hidden" name="nextStatus" value="@OrderStatus.Cancelled" /><input type="hidden" name="currentStatus" value="@Model.CurrentStatus" /><button class="btn btn--danger-quiet btn--sm">Hủy đơn</button></form> }
</div></div></details></td></tr>
```

Trong cả hai form, đặt đủ các hidden input giữ trạng thái giao diện:

```cshtml
<input type="hidden" name="search" value="@Model.Search" />
<input type="hidden" name="sortBy" value="@Model.SortBy" />
<input type="hidden" name="sortDir" value="@Model.SortDir" />
<input type="hidden" name="pageNumber" value="@Model.Result.Page" />
```

- [ ] **Step 3: Thêm CSS hoàn chỉnh**

```css
.alert--success { color: var(--color-success); border-color: var(--color-success); background: var(--color-success-soft); }
.order-details-row > td { padding-top: 0; background: var(--color-paper-2); }
.order-details { border-top: 1px dashed var(--color-rule-strong); }
.order-details > summary { width: fit-content; padding: var(--space-sm) 0; color: var(--color-accent-strong); font-weight: 600; cursor: pointer; }
.order-details__content { display: grid; gap: var(--space-md); padding-bottom: var(--space-md); }
.order-meta { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: var(--space-md); margin: 0; }
.order-meta dt { color: var(--color-ink-2); font-size: var(--text-xs); }
.order-meta dd { margin: var(--space-2xs) 0 0; overflow-wrap: anywhere; }
.order-items-table { width: 100%; min-width: 560px; border-collapse: collapse; }
.order-items-table th, .order-items-table td { padding: var(--space-xs) var(--space-sm); border-bottom: 1px solid var(--color-rule); }
.order-actions { display: flex; flex-wrap: wrap; gap: var(--space-xs); align-items: center; }
.order-actions form { margin: 0; }
@media (max-width: 700px) { .order-meta { grid-template-columns: minmax(0, 1fr); } }
```

- [ ] **Step 4: Test, build và commit**

Run: `powershell -ExecutionPolicy Bypass -File tests/AdminOrderManagementRegression.ps1`

Run: `dotnet build QuickBite/QuickBite.csproj --no-restore`

Commit: `git commit -m "feat: complete admin order management"`

---

### Task 4: Xác minh cuối

**Files:** Verify only.

**Interfaces:** Consumes toàn bộ Task 1–3; produces bằng chứng build/test và diff sạch.

- [ ] **Step 1:** Run `dotnet run --project tests/QuickBite.DomainTests/QuickBite.DomainTests.csproj`; expected passed.
- [ ] **Step 2:** Run `powershell -ExecutionPolicy Bypass -File tests/AdminOrderManagementRegression.ps1`; expected passed.
- [ ] **Step 3:** Run `dotnet build QuickBite/QuickBite.csproj --no-restore`; expected 0 errors.
- [ ] **Step 4:** Run `git diff --check dev...HEAD` và `git diff --name-only dev...HEAD`; expected không có migration, `/Hubs`, SignalR JS hoặc trang khách.
- [ ] **Step 5:** Trong Visual Studio 2026 Enterprise, F5 và kiểm tra tab, chi tiết, tiến từng bước, hủy trước Completed, cùng POST giả `Pending → Ready` bị từ chối.

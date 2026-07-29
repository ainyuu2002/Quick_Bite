# OrderCore Admin UX Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cải thiện hierarchy, empty state và workboard của Orders/Kitchen/Shipper, đồng thời giữ nguyên toàn bộ nghiệp vụ OrderCore.

**Architecture:** Razor tiếp tục render dữ liệu server-side; các page chỉ phân nhóm collection đã tải và dùng native `details` cho progressive disclosure. `admin.css` cung cấp shared patterns, còn các hook SignalR và `reason-other.js` hiện hữu được giữ nguyên.

**Tech Stack:** ASP.NET Core 8 Razor Pages, EF Core/SQL Server, plain CSS, vanilla JavaScript, SignalR, PowerShell regression tests, Chrome browser control.

## Global Constraints

- Không sửa PageModel, service, hub, authorization, state machine hoặc schema.
- Không sửa `QuickBite/wwwroot/js/admin-orders.js`; file này đang có thay đổi chưa commit của người dùng.
- Không xóa file hoặc route.
- Dùng token từ `QuickBite/wwwroot/css/tokens.css`; không thêm màu/font inline.
- Primary transition có click target tối thiểu 44px.
- Reason controls chỉ xuất hiện khi người dùng mở disclosure ngoại lệ.
- Kiểm tra cả có đơn và không có đơn.
- Kiểm tra responsive tại 320, 375, 414, 768px và laptop.

---

## File Map

- Create `tests/OrderCoreAdminPresentationRegression.ps1`: characterization/regression contract cho markup và CSS.
- Modify `QuickBite/Pages/Admin/Orders/Index.cshtml`: grouped status navigation, true empty state, action hierarchy.
- Modify `QuickBite/Pages/Admin/Kitchen/Index.cshtml`: two-lane kitchen workboard.
- Modify `QuickBite/Pages/Admin/Shipper/Index.cshtml`: two-lane delivery workboard và failure disclosure.
- Modify `QuickBite/wwwroot/css/admin.css`: shared status groups, workboard, ticket, disclosure và responsive rules.
- Modify `design.md`: ghi nhận OrderCore workboard/action hierarchy.
- Create `.hallmark/preflight.json`: cache các tín hiệu thiết kế đã khảo sát.
- Create/modify `.hallmark/log.json`: một entry `scope: app` cho redesign nhiều trang.
- Do not modify `QuickBite/wwwroot/js/admin-orders.js`.
- Delete: none.

---

### Task 1: Khóa presentation contract bằng regression test

**Files:**
- Create: `tests/OrderCoreAdminPresentationRegression.ps1`
- Test: `tests/OrderCoreAdminPresentationRegression.ps1`

**Interfaces:**
- Consumes: Razor/CSS source files hiện tại.
- Produces: static contract bảo vệ các class/hook bắt buộc.

- [ ] **Step 1: Viết test thất bại**

```powershell
$ErrorActionPreference = "Stop"

function Assert-Match([string]$Text, [string]$Pattern, [string]$Message) {
    if ($Text -notmatch $Pattern) { throw $Message }
}

$orders = Get-Content -Raw "QuickBite/Pages/Admin/Orders/Index.cshtml"
$kitchen = Get-Content -Raw "QuickBite/Pages/Admin/Kitchen/Index.cshtml"
$shipper = Get-Content -Raw "QuickBite/Pages/Admin/Shipper/Index.cshtml"
$css = Get-Content -Raw "QuickBite/wwwroot/css/admin.css"

Assert-Match $orders 'status-groups' "Orders chưa nhóm trạng thái."
Assert-Match $orders 'empty-state--orders' "Orders chưa có empty state riêng."
Assert-Match $orders 'exception-disclosure' "Orders chưa tách thao tác ngoại lệ."
Assert-Match $orders 'id="liveBanner"' "Orders làm mất hook realtime."
Assert-Match $orders 'id="staffOnline"' "Orders làm mất staff-online hook."

Assert-Match $kitchen 'workboard-lane' "Kitchen chưa có workboard lane."
Assert-Match $kitchen 'OrderStatus\.Confirmed' "Kitchen thiếu lane Confirmed."
Assert-Match $kitchen 'OrderStatus\.Preparing' "Kitchen thiếu lane Preparing."
Assert-Match $kitchen 'data-role-board="kitchen"' "Kitchen làm mất SignalR hook."

Assert-Match $shipper 'workboard-lane' "Shipper chưa có workboard lane."
Assert-Match $shipper 'OrderStatus\.Ready' "Shipper thiếu lane Ready."
Assert-Match $shipper 'OrderStatus\.Delivering' "Shipper thiếu lane Delivering."
Assert-Match $shipper 'exception-disclosure' "Shipper chưa tách giao thất bại."
Assert-Match $shipper 'data-role-board="shipper"' "Shipper làm mất SignalR hook."

Assert-Match $css '\.status-groups' "CSS thiếu status groups."
Assert-Match $css '\.workboard-lane' "CSS thiếu workboard lane."
Assert-Match $css '\.exception-disclosure' "CSS thiếu exception disclosure."
Assert-Match $css '@media \(max-width: 540px\)' "CSS thiếu mobile breakpoint."

Write-Output "OrderCore admin presentation regression checks passed."
```

- [ ] **Step 2: Chạy test và xác nhận FAIL**

Run in Visual Studio Test/terminal integration:

```powershell
powershell -ExecutionPolicy Bypass -File tests/OrderCoreAdminPresentationRegression.ps1
```

Expected: FAIL tại `Orders chưa nhóm trạng thái.`

---

### Task 2: Redesign Orders presentation

**Files:**
- Modify: `QuickBite/Pages/Admin/Orders/Index.cshtml`
- Test: `tests/OrderCoreAdminPresentationRegression.ps1`

**Interfaces:**
- Consumes: `Model.CurrentStatus`, `Model.Result`, existing query parameters và POST handlers.
- Produces: `.status-groups`, `.empty-state--orders`, `.exception-disclosure`; giữ nguyên realtime IDs.

- [ ] **Step 1: Khai báo ba nhóm trạng thái ở Razor presentation scope**

```csharp
var activeStatuses = new[]
{
    OrderStatus.PendingReview, OrderStatus.Pending, OrderStatus.Confirmed,
    OrderStatus.Preparing, OrderStatus.Ready, OrderStatus.Delivering
};
var completedStatuses = new[] { OrderStatus.Completed };
var exceptionStatuses = new[]
{
    OrderStatus.Cancelled, OrderStatus.Rejected, OrderStatus.Expired,
    OrderStatus.DeliveryFailed, OrderStatus.NoShow
};
```

- [ ] **Step 2: Render grouped navigation bằng partial local loop**

Mỗi link giữ nguyên `status`, `search`, `sortBy`, `sortDir`, `pageNumber` và
`aria-current`. Ba group có heading ngắn và wrap tự nhiên.

- [ ] **Step 3: Giữ search/realtime hook và cải thiện status strip**

Giữ nguyên `id="liveBanner"`, `id="liveDot"`, `id="liveLabel"`,
`id="staffOnline"` và `id="newOrderBadge"` nếu đang tồn tại.

- [ ] **Step 4: Branch empty/data state**

```razor
@if (Model.Result.Items.Count == 0)
{
    <section class="empty-state empty-state--orders" aria-labelledby="orders-empty-title">
        <span class="empty-state__icon" aria-hidden="true"></span>
        <h2 id="orders-empty-title">Chưa có đơn ở trạng thái @Model.CurrentStatus.ToDisplayText()</h2>
        <p>Danh sách sẽ tự cập nhật khi có đơn phù hợp.</p>
    </section>
}
else
{
    <div class="data-surface">
        <!-- existing table and details rows -->
    </div>
}
```

- [ ] **Step 5: Tách reason actions vào native disclosure**

```razor
<details class="exception-disclosure">
    <summary>Thao tác ngoại lệ</summary>
    <div class="exception-disclosure__content">
        <!-- existing reason select, reasonOther and destructive submit -->
    </div>
</details>
```

Không đổi hidden fields, handler names hoặc `data-reason-*`.

- [ ] **Step 6: Bỏ `btn--sm` khỏi primary transition**

Primary transitions dùng `btn btn--primary`; secondary utility có thể giữ
`btn--sm`. Final destructive submit dùng `btn btn--danger`.

- [ ] **Step 7: Chạy presentation test**

Expected: Orders assertions PASS; Kitchen assertion là failure kế tiếp.

---

### Task 3: Redesign Kitchen và Shipper workboards

**Files:**
- Modify: `QuickBite/Pages/Admin/Kitchen/Index.cshtml`
- Modify: `QuickBite/Pages/Admin/Shipper/Index.cshtml`
- Test: `tests/OrderCoreAdminPresentationRegression.ps1`

**Interfaces:**
- Consumes: `Model.Queue`, `Model.Board`, existing `Advance` handlers.
- Produces: two-lane markup, full/compact empty states, unchanged `data-role-board`.

- [ ] **Step 1: Phân nhóm Kitchen collection trong Razor**

```csharp
var waitingOrders = Model.Queue.Where(o => o.Status == OrderStatus.Confirmed).ToList();
var preparingOrders = Model.Queue.Where(o => o.Status == OrderStatus.Preparing).ToList();
```

- [ ] **Step 2: Render Kitchen lanes**

Mỗi lane dùng heading, count badge và ticket loop riêng. Nếu cả collection
rỗng, render `.empty-state--workboard`; nếu một lane rỗng, render
`.empty-state--compact`.

- [ ] **Step 3: Chuẩn hóa Kitchen ticket**

Giữ item quantity/name và note; primary action cuối card dùng chiều cao 44px.
Không thêm customer data.

- [ ] **Step 4: Phân nhóm Shipper collection**

```csharp
var readyOrders = Model.Board.Where(o => o.Status == OrderStatus.Ready).ToList();
var deliveringOrders = Model.Board.Where(o => o.Status == OrderStatus.Delivering).ToList();
```

- [ ] **Step 5: Render Shipper lanes và ticket hierarchy**

Địa chỉ dùng `.ticket__address`; tên/SĐT/tổng tiền dùng metadata phụ. Primary
action đứng trước disclosure.

- [ ] **Step 6: Đưa failure form vào disclosure**

```razor
<details class="exception-disclosure exception-disclosure--ticket">
    <summary>Không giao được?</summary>
    <form method="post" asp-page-handler="Advance"
          class="ticket__fail exception-disclosure__content" data-reason-group>
        <!-- existing fields -->
        <button class="btn btn--danger" type="submit">Xác nhận giao thất bại</button>
    </form>
</details>
```

- [ ] **Step 7: Chạy presentation test**

Expected: toàn bộ test PASS sau khi CSS task hoàn tất; tại đây chỉ còn CSS
assertion failure.

---

### Task 4: Implement shared Admin visual system

**Files:**
- Modify: `QuickBite/wwwroot/css/admin.css`
- Modify only if needed: `QuickBite/wwwroot/css/tokens.css`
- Test: `tests/OrderCoreAdminPresentationRegression.ps1`

**Interfaces:**
- Consumes: existing design tokens.
- Produces: reusable status group, workboard, empty-state và disclosure styles.

- [ ] **Step 1: Thêm status group styles**

Tạo `.status-groups`, `.status-group`, `.status-group__label`,
`.status-group__links`; không dùng horizontal overflow làm default.

- [ ] **Step 2: Thêm empty-state variants**

Tạo `.empty-state__icon`, `.empty-state--orders`,
`.empty-state--workboard`, `.empty-state--compact`. Icon dùng CSS border/line
và token màu hiện tại.

- [ ] **Step 3: Thêm workboard/lane/ticket hierarchy**

Desktop dùng hai cột `repeat(2, minmax(0, 1fr))`; ticket grid dùng
`minmax(280px, 1fr)`. Card actions đẩy về cuối bằng `margin-top: auto`.

- [ ] **Step 4: Thêm disclosure interaction states**

`summary` có hover/focus/open state; content có border-top và spacing. Chỉ
animate opacity/transform, reduced-motion vẫn thắng.

- [ ] **Step 5: Responsive rules**

Tại 860px workboard về một cột; tại 540px action/form controls full-width,
button text không wrap, page không overflow.

- [ ] **Step 6: Chạy presentation test**

Expected: `OrderCore admin presentation regression checks passed.`

---

### Task 5: Cập nhật design memory và audit Admin phụ

**Files:**
- Modify: `design.md`
- Create: `.hallmark/preflight.json`
- Create or modify: `.hallmark/log.json`
- Modify Admin page markup only if browser audit proves shared CSS insufficient.

**Interfaces:**
- Produces: durable design rules cho lần sửa sau.

- [ ] **Step 1: Bổ sung OrderCore workboard/action hierarchy vào `design.md`**

Ghi rõ two-lane role boards, true empty states và reason disclosure.

- [ ] **Step 2: Ghi preflight cache**

Record framework Razor Pages, Manrope/Inter, OKLCH tokens, motion-cut và 4px
spacing.

- [ ] **Step 3: Append Hallmark app-scope log**

Entry newest-first:

```json
{
  "date": "2026-07-29",
  "scope": "app",
  "macrostructure": "Workbench",
  "theme": "custom-soft-indigo",
  "enrichment": "none",
  "brief": "OrderCore Admin operational workboards and action hierarchy"
}
```

- [ ] **Step 4: Audit Món ăn, Nhân viên, Doanh thu, Blacklist bằng Chrome**

Chỉ sửa page markup nếu có lỗi hierarchy rõ; ghi file phát sinh vào handoff.

---

### Task 6: Verification có đơn, không có đơn và regression

**Files:**
- Test: `tests/OrderCoreAdminPresentationRegression.ps1`
- Test: `tests/AdminOrderManagementRegression.ps1`
- Test: `tests/OrderServiceChangeStatusRegression.ps1`

- [ ] **Step 1: Chạy static regression tests**

```powershell
powershell -ExecutionPolicy Bypass -File tests/OrderCoreAdminPresentationRegression.ps1
powershell -ExecutionPolicy Bypass -File tests/AdminOrderManagementRegression.ps1
powershell -ExecutionPolicy Bypass -File tests/OrderServiceChangeStatusRegression.ps1
```

- [ ] **Step 2: Build**

Chạy **Build → Build Solution** trong Visual Studio; tương đương kiểm tra tự
động bằng project build hiện có. Expected: 0 errors.

- [ ] **Step 3: Chrome — không có đơn**

Xác minh Orders, Kitchen, Shipper ở desktop và 320/375/414/768px.

- [ ] **Step 4: Chrome — tạo dữ liệu thật qua UI**

Menu → Cart → Checkout tạo Delivery và Pickup test orders.

- [ ] **Step 5: Chrome — đẩy state machine**

Admin xác nhận → Kitchen bắt đầu/xong → Shipper nhận giao. Xác minh layout ở
từng bước và SignalR refresh.

- [ ] **Step 6: Chrome — reason disclosure**

Mở nhưng không bắt buộc submit destructive form; chọn `Khác…` và xác minh ô
`reasonOther` xuất hiện/required.

- [ ] **Step 7: Final diff safety check**

```powershell
git diff --check
git status --short
git diff -- QuickBite/wwwroot/js/admin-orders.js
```

Xác minh diff của `admin-orders.js` vẫn đúng phần thay đổi có trước, không có
edit mới từ task này.


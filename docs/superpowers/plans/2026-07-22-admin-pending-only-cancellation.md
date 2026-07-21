# Admin Pending-Only Cancellation Implementation Plan

> **For Codex:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Chặn admin hủy đơn sau khi đơn đã được nhận; admin chỉ được hủy đơn ở trạng thái `Pending`.

**Architecture:** Giữ `OrderStatus.CanBeCancelledByStaff()` làm nguồn chính sách duy nhất. Giao diện admin và `OrderService.ChangeStatusAsync()` đã cùng gọi policy này, nên chỉ cần siết policy và cập nhật kiểm thử miền; không thêm logic trùng lặp vào PageModel hay Razor View.

**Tech Stack:** ASP.NET Core Razor Pages, C#, .NET, kiểm thử console hiện có, PowerShell regression tests.

---

### Task 1: Siết chính sách hủy đơn của admin

**Files:**
- Modify: `tests/QuickBite.DomainTests/Program.cs`
- Modify: `QuickBite/Models/OrderStatus.cs`

**Step 1: Viết kiểm thử thất bại**

Thay các assertion về `CanBeCancelledByStaff()` bằng:

```csharp
Assert(
    OrderStatus.Pending.CanBeCancelledByStaff(),
    "Admin phải được hủy đơn Pending.");

foreach (var status in new[]
         {
             OrderStatus.Accepted,
             OrderStatus.Preparing,
             OrderStatus.Ready,
             OrderStatus.Completed,
             OrderStatus.Cancelled
         })
{
    Assert(
        !status.CanBeCancelledByStaff(),
        $"Admin không được hủy đơn ở trạng thái {status}.");
}
```

**Step 2: Chạy kiểm thử để xác nhận đỏ**

Chạy project `tests/QuickBite.DomainTests/QuickBite.DomainTests.csproj` với `--no-build --no-restore`.

Kỳ vọng: FAIL tại trạng thái `Accepted`, chứng minh hành vi cũ vẫn cho phép admin hủy sau khi nhận đơn.

**Step 3: Sửa tối thiểu policy dùng chung**

Trong `QuickBite/Models/OrderStatus.cs`, đổi thành:

```csharp
/// <summary>
/// Admin chỉ được hủy đơn trước khi nhận đơn.
/// </summary>
public static bool CanBeCancelledByStaff(this OrderStatus current)
    => current == OrderStatus.Pending;
```

Không sửa `CanBeCancelledByCustomer()`, `CanTransitionTo()`, SignalR, schema DB, `OrderService` hoặc Razor View.

**Step 4: Chạy kiểm thử để xác nhận xanh**

Chạy lại project domain tests.

Kỳ vọng: PASS toàn bộ kiểm thử trạng thái đơn.

**Step 5: Chạy hồi quy và build**

Chạy lần lượt:

- `tests/OrderServiceChangeStatusRegression.ps1`
- `tests/AdminOrderManagementRegression.ps1`
- Build `QuickBite/QuickBite.csproj` với `--no-restore`

Kỳ vọng: các regression test PASS; build 0 lỗi. Vì giao diện và service đều dùng `CanBeCancelledByStaff()`, nút hủy biến mất và POST trực tiếp cũng bị từ chối sau `Accepted`.

**Step 6: Commit**

```text
fix: block admin cancellation after acceptance
```

# Payment Method Database Script Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Đồng bộ `QuickBite-Data/script.sql` với model hiện tại để database mới có `Orders.PaymentMethod` và dữ liệu mẫu cho cả tiền mặt lẫn chuyển khoản.

**Architecture:** `script.sql` tiếp tục là nguồn sự thật Database First và vẫn xóa/tạo/seed lại toàn bộ bảng. Một regression script kiểm tra cấu trúc văn bản trước, sau đó toàn bộ SQL được thực thi trên SQL Server local và xác minh bằng metadata cùng dữ liệu thực.

**Tech Stack:** SQL Server T-SQL, PowerShell regression test, SQL Server Object Explorer/`sqlcmd` cho xác minh tự động.

## Global Constraints

- Giữ nguyên hành vi xóa bảng cũ và seed lại.
- Không bảo toàn dữ liệu database hiện có.
- Không tạo hoặc chạy EF Core migration.
- Không sửa model, DbContext hoặc mã ứng dụng.
- `0 = Cash`, `1 = BankTransfer`.

---

### Task 1: Đồng bộ schema và seed

**Files:**
- Create: `tests/PaymentMethodScriptRegression.ps1`
- Modify: `../QuickBite-Data/script.sql`

**Interfaces:**
- Consumes: `Order.PaymentMethod` với enum `Cash = 0`, `BankTransfer = 1`.
- Produces: cột SQL `Orders.PaymentMethod INT NOT NULL DEFAULT 0` và seed chỉ chứa `0/1`.

- [ ] **Step 1: Viết regression test đang thất bại**

```powershell
$ErrorActionPreference = "Stop"
$scriptPath = Join-Path $PSScriptRoot "../../QuickBite-Data/script.sql"
$sql = Get-Content -Raw $scriptPath

function Assert-Match([string]$Pattern, [string]$Message) {
    if ($sql -notmatch $Pattern) { throw $Message }
}

Assert-Match 'PaymentMethod\s+INT\s+NOT\s+NULL\s+DEFAULT\s+0' "Thiếu cột PaymentMethod."
Assert-Match 'INSERT\s+INTO\s+Orders\s*\([^)]*PaymentMethod[^)]*\)' "Seed Orders chưa ghi PaymentMethod."
Assert-Match 'PaymentMethod\s*=\s*0|PaymentMethod[^\r\n]*Cash' "Thiếu quy ước Cash = 0."
Assert-Match 'PaymentMethod\s*=\s*1|PaymentMethod[^\r\n]*BankTransfer' "Thiếu quy ước BankTransfer = 1."
Assert-Match "COL_LENGTH\(N?'dbo\.Orders',\s*N?'PaymentMethod'\)" "Thiếu kiểm tra metadata PaymentMethod."
Assert-Match 'PaymentMethod\s+NOT\s+IN\s*\(0,\s*1\)' "Thiếu kiểm tra giá trị ngoài enum."

Write-Output "PaymentMethod script regression checks passed."
```

- [ ] **Step 2: Chạy test để xác nhận RED**

Run: `powershell -ExecutionPolicy Bypass -File tests/PaymentMethodScriptRegression.ps1`

Expected: fail với `Thiếu cột PaymentMethod.`

- [ ] **Step 3: Sửa `CREATE TABLE Orders`**

```sql
Status        INT NOT NULL DEFAULT 0,
PaymentMethod INT NOT NULL DEFAULT 0, -- 0 Cash, 1 BankTransfer
CreatedAt     DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
```

- [ ] **Step 4: Sửa seed bảy đơn**

```sql
INSERT INTO Orders
    (Id, CustomerName, Phone, Address, Note, Status, PaymentMethod, CreatedAt, Total)
VALUES
(1, N'Nguyễn Văn An',  N'0901234567', N'123 Nguyễn Trãi, Q.1, TP.HCM',            N'Ít cay, giao trước 12h',     0, 0, DATEADD(MINUTE, -5,  SYSDATETIME()), 140000),
(2, N'Trần Thị Bình',  N'0912345678', N'45 Lê Lợi, Q.Hải Châu, Đà Nẵng',          NULL,                          1, 1, DATEADD(MINUTE, -20, SYSDATETIME()),  70000),
(3, N'Lê Hoàng Cường', N'0987654321', N'78 Cầu Giấy, Q.Cầu Giấy, Hà Nội',         N'Không hành',                 2, 0, DATEADD(MINUTE, -35, SYSDATETIME()), 150000),
(4, N'Phạm Minh Đức',  N'0938111222', N'12 Võ Văn Ngân, TP. Thủ Đức, TP.HCM',     NULL,                          3, 1, DATEADD(MINUTE, -50, SYSDATETIME()),  75000),
(5, N'Võ Thu Hà',      N'0909888777', N'56 Trần Hưng Đạo, Q.5, TP.HCM',           N'Lấy tại quầy',               4, 0, DATEADD(HOUR,   -2,  SYSDATETIME()), 255000),
(6, N'Đỗ Quang Huy',   N'0977666555', N'89 Hoàng Hoa Thám, Q.Bình Thạnh, TP.HCM', NULL,                          4, 1, DATEADD(DAY,    -1,  SYSDATETIME()), 120000),
(7, N'Bùi Thanh Lam',  N'0966555444', N'34 Phan Chu Trinh, TP. Huế',              N'Khách gọi hủy vì đặt nhầm',  5, 0, DATEADD(HOUR,   -1,  SYSDATETIME()),  35000);
```

Giữ nguyên toàn bộ chuỗi khách hàng, địa chỉ, ghi chú và biểu thức thời gian hiện có; chỉ thêm cột và giá trị `PaymentMethod` ở vị trí đã chỉ định.

- [ ] **Step 5: Thêm xác minh cuối script**

```sql
SELECT COL_LENGTH(N'dbo.Orders', N'PaymentMethod') AS PaymentMethodColumnLength;

IF EXISTS (SELECT 1 FROM Orders WHERE PaymentMethod NOT IN (0, 1))
    THROW 51000, 'Orders.PaymentMethod contains an invalid value.', 1;

SELECT PaymentMethod, COUNT(*) AS Orders
FROM Orders
GROUP BY PaymentMethod
ORDER BY PaymentMethod;
```

- [ ] **Step 6: Chạy regression test để xác nhận GREEN**

Run: `powershell -ExecutionPolicy Bypass -File tests/PaymentMethodScriptRegression.ps1`

Expected: `PaymentMethod script regression checks passed.`

---

### Task 2: Thực thi và xác minh SQL Server

**Files:** Verify only.

**Interfaces:**
- Consumes: `../QuickBite-Data/script.sql` đã sửa.
- Produces: database local `QuickBite` có schema và seed khớp model.

- [ ] **Step 1: Xác nhận đúng target**

Target bắt buộc: SQL Server instance `.`; database duy nhất bị script tạo lại là `QuickBite`.

- [ ] **Step 2: Chạy toàn bộ script**

Trong Visual Studio 2026 Enterprise: mở **View → SQL Server Object Explorer**, kết nối instance `.`, mở `QuickBite-Data/script.sql`, chọn **Execute**.

Xác minh tự động tương đương trong phiên agent: `sqlcmd -S . -E -i ../QuickBite-Data/script.sql`.

Expected: script kết thúc không lỗi; bảng đếm trả `6 | 30 | 7 | 14 | 1`.

- [ ] **Step 3: Truy vấn metadata và seed**

```sql
SELECT COL_LENGTH(N'dbo.Orders', N'PaymentMethod') AS ColumnLength;
SELECT PaymentMethod, COUNT(*) AS Orders
FROM dbo.Orders
GROUP BY PaymentMethod
ORDER BY PaymentMethod;
```

Expected: `ColumnLength = 4`; cả nhóm `0` và `1` đều có ít nhất một đơn.

- [ ] **Step 4: Build ứng dụng và review phạm vi**

Run: `dotnet build QuickBite/QuickBite.csproj --no-restore`

Expected: `0 Error(s)`; không có migration hoặc thay đổi model/DbContext.

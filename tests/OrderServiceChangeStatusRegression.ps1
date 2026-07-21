$ErrorActionPreference = "Stop"

function Assert-Match {
    param(
        [string]$Text,
        [string]$Pattern,
        [string]$Message
    )

    if ($Text -notmatch $Pattern) {
        throw $Message
    }
}

$service = Get-Content -Raw "QuickBite/Services/OrderService.cs"

Assert-Match $service `
    'Task<Order>\s+ChangeStatusAsync' `
    "OrderService chưa cung cấp ChangeStatusAsync."
Assert-Match $service `
    'Enum\.IsDefined\(nextStatus\)' `
    "ChangeStatusAsync chưa từ chối giá trị enum giả."
Assert-Match $service `
    'CanTransitionTo\(nextStatus\)' `
    "ChangeStatusAsync chưa tái sử dụng máy trạng thái BR-01."
Assert-Match $service `
    'CanBeCancelledByStaff\(\)' `
    "ChangeStatusAsync chưa áp dụng chính sách hủy của admin."
Assert-Match $service `
    'SaveChangesAsync\(cancellationToken\)' `
    "ChangeStatusAsync chưa lưu thay đổi qua AppDbContext."

Write-Output "OrderService change-status regression checks passed."

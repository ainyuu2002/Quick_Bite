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

$pageModel = Get-Content -Raw "QuickBite/Pages/Admin/Orders/Index.cshtml.cs"
$view = Get-Content -Raw "QuickBite/Pages/Admin/Orders/Index.cshtml"
$service = Get-Content -Raw "QuickBite/Services/OrderService.cs"

Assert-Match $pageModel `
    '\.Include\(o\s*=>\s*o\.Items\)' `
    "GET chưa tải Order.Items."
Assert-Match $pageModel `
    '\.ThenInclude\(item\s*=>\s*item\.MenuItem\)' `
    "GET chưa tải tên MenuItem."
Assert-Match $pageModel `
    'OnPostChangeStatusAsync' `
    "Thiếu POST handler đổi trạng thái."
Assert-Match $pageModel `
    '_orderService\.ChangeStatusAsync' `
    "POST handler chưa gọi OrderService."
Assert-Match $view `
    'asp-page-handler="ChangeStatus"' `
    "View thiếu form đổi trạng thái."
Assert-Match $view `
    'item\.UnitPrice' `
    "Chi tiết chưa dùng UnitPrice đã chốt."
Assert-Match $service `
    'Enum\.IsDefined\(nextStatus\)' `
    "Service chưa chặn enum giả."

Write-Output "Admin order management regression checks passed."

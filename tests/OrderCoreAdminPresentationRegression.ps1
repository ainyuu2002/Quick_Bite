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

$orders = Get-Content -Raw "QuickBite/Pages/Admin/Orders/Index.cshtml"
$kitchen = Get-Content -Raw "QuickBite/Pages/Admin/Kitchen/Index.cshtml"
$shipper = Get-Content -Raw "QuickBite/Pages/Admin/Shipper/Index.cshtml"
$css = Get-Content -Raw "QuickBite/wwwroot/css/admin.css"

Assert-Match $orders `
    'status-groups' `
    "Orders chưa nhóm trạng thái."
Assert-Match $orders `
    'empty-state--orders' `
    "Orders chưa có empty state riêng."
Assert-Match $orders `
    'exception-disclosure' `
    "Orders chưa tách thao tác ngoại lệ."
Assert-Match $orders `
    'id="liveBanner"' `
    "Orders làm mất hook realtime."
Assert-Match $orders `
    'id="staffOnline"' `
    "Orders làm mất staff-online hook."

Assert-Match $kitchen `
    'workboard-lane' `
    "Kitchen chưa có workboard lane."
Assert-Match $kitchen `
    'OrderStatus\.Confirmed' `
    "Kitchen thiếu lane Confirmed."
Assert-Match $kitchen `
    'OrderStatus\.Preparing' `
    "Kitchen thiếu lane Preparing."
Assert-Match $kitchen `
    'data-role-board="kitchen"' `
    "Kitchen làm mất SignalR hook."

Assert-Match $shipper `
    'workboard-lane' `
    "Shipper chưa có workboard lane."
Assert-Match $shipper `
    'OrderStatus\.Ready' `
    "Shipper thiếu lane Ready."
Assert-Match $shipper `
    'OrderStatus\.Delivering' `
    "Shipper thiếu lane Delivering."
Assert-Match $shipper `
    'exception-disclosure' `
    "Shipper chưa tách giao thất bại."
Assert-Match $shipper `
    'data-role-board="shipper"' `
    "Shipper làm mất SignalR hook."

Assert-Match $css `
    '\.status-groups' `
    "CSS thiếu status groups."
Assert-Match $css `
    '\.workboard-lane' `
    "CSS thiếu workboard lane."
Assert-Match $css `
    '\.exception-disclosure' `
    "CSS thiếu exception disclosure."
Assert-Match $css `
    '@media \(max-width: 540px\)' `
    "CSS thiếu mobile breakpoint."

Write-Output "OrderCore admin presentation regression checks passed."

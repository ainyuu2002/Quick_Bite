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

function Assert-NotMatch {
    param(
        [string]$Text,
        [string]$Pattern,
        [string]$Message
    )

    if ($Text -match $Pattern) {
        throw $Message
    }
}

$orderService = Get-Content -Raw "QuickBite/Services/OrderService.cs"
$loyaltyService = Get-Content -Raw "QuickBite/Services/LoyaltyService.cs"
$loginPage = Get-Content -Raw "QuickBite/Pages/Admin/Login.cshtml.cs"
$storePage = Get-Content -Raw "QuickBite/Pages/Admin/Operations/Store.cshtml.cs"
$ingredientPage = Get-Content -Raw "QuickBite/Pages/Admin/Ingredients/Index.cshtml.cs"
$trackPage = Get-Content -Raw "QuickBite/Pages/Orders/Track.cshtml"
$accountOrdersPage = Get-Content -Raw "QuickBite/Pages/Account/Orders.cshtml"
$feedbackPage = Get-Content -Raw "QuickBite/Pages/Orders/Feedback.cshtml"

Assert-Match $orderService 'IStoreAvailabilityService' `
    "OrderService must depend on IStoreAvailabilityService."
Assert-Match $orderService 'GetStatusAsync' `
    "OrderService must check store status."
Assert-Match $orderService 'IMenuAvailabilityService' `
    "OrderService must depend on IMenuAvailabilityService."
Assert-Match $orderService 'TryReserveAsync' `
    "OrderService must reserve quota."
Assert-Match $orderService 'ReleaseAsync' `
    "OrderService must release quota."
Assert-Match $orderService 'ShouldReleaseQuota' `
    "OrderService must restrict quota release states."

$transactionIndex = $orderService.IndexOf("BeginTransactionAsync")
$menuReadIndex = $orderService.IndexOf("_db.MenuItems")
if ($transactionIndex -lt 0 -or $menuReadIndex -lt 0 -or $transactionIndex -gt $menuReadIndex) {
    throw "The order transaction must begin before the authoritative menu read."
}

Assert-Match $loyaltyService 'PointsFor\(order\.Subtotal\)' `
    "Loyalty points must use subtotal."
Assert-Match $loyaltyService 'order\.OrderCode' `
    "Loyalty note must use OrderCode."

Assert-NotMatch $loginPage '_\s*=>\s*throw\s+new\s+InvalidOperationException' `
    "Login must not crash for an invalid role."

Assert-Match $storePage 'OnPostPauseAsync\(\s*string\s+pauseReason' `
    "Pause form must bind an independent input."
Assert-NotMatch $storePage '\[BindProperty\][\s\r\n]+public\s+BusinessHoursInput' `
    "BusinessHours must not validate during pause."
Assert-Match $ingredientPage 'OnPostCreateAsync\(\s*string\s+ingredientName' `
    "Ingredient create form must bind an independent input."
Assert-NotMatch $ingredientPage '\[BindProperty\][\s\r\n]+public\s+LinkInput' `
    "Links must not validate during ingredient creation."

Assert-NotMatch $accountOrdersPage '#@order\.Id' `
    "Account order history must not expose the primary key."
Assert-NotMatch $feedbackPage '#@Model\.Order\.Id' `
    "Feedback page must not expose the primary key."
Assert-Match $trackPage 'CanReceiveComplaint' `
    "Track page must use the complaint status policy."

if (-not (Test-Path "QuickBite/Pages/Admin/_ViewStart.cshtml")) {
    throw "Admin area must have a shared _ViewStart."
}
if (-not (Test-Path "QuickBite/Data/Patches/2026-07-29-normalize-manager-role.sql")) {
    throw "The manager role patch must ship in the repository."
}
if (-not (Test-Path "QuickBite/Data/Patches/2026-07-29-backfill-open-order-quotas.sql")) {
    throw "The open-order quota backfill must ship in the repository."
}

Write-Output "Post-merge business regression checks passed."

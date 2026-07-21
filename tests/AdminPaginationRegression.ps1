param(
    [string]$BaseUrl = "http://127.0.0.1:5215"
)

$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$loginPage = Invoke-WebRequest -Uri "$BaseUrl/Admin/Login" -SessionVariable adminSession -UseBasicParsing

$tokenMatch = [regex]::Match(
    $loginPage.Content,
    'name="__RequestVerificationToken"[^>]*value="([^"]+)"'
)

Assert-True $tokenMatch.Success "Login page did not contain an antiforgery token."

$loginBody = @{
    Username = "admin"
    Password = "Admin@123"
    __RequestVerificationToken = $tokenMatch.Groups[1].Value
}

Invoke-WebRequest -Uri "$BaseUrl/Admin/Login" -Method Post -WebSession $adminSession -Body $loginBody -UseBasicParsing | Out-Null

$menuPage = Invoke-WebRequest -Uri "$BaseUrl/Admin/MenuItems" -WebSession $adminSession -UseBasicParsing

$nextLinkMatch = [regex]::Match(
    $menuPage.Content,
    '<a[^>]*href="([^"]*)"[^>]*>\s*Sau\s*</a>',
    [System.Text.RegularExpressions.RegexOptions]::IgnoreCase
)

Assert-True $nextLinkMatch.Success "MenuItems did not render the next-page link."
Assert-True ($nextLinkMatch.Groups[1].Value -match 'pageNumber=2') "MenuItems next-page link did not contain pageNumber=2. Actual href: '$($nextLinkMatch.Groups[1].Value)'"

$ordersSecondPage = Invoke-WebRequest -Uri "$BaseUrl/Admin/Orders?status=Completed&pageNumber=2" -WebSession $adminSession -UseBasicParsing

Assert-True ($ordersSecondPage.Content -notmatch '<span class="order-id">#5</span>') "Orders ignored pageNumber=2 and returned first-page data."

Write-Output "Admin pagination regression test passed."

$ErrorActionPreference = "Stop"

$scriptPath = Join-Path $PSScriptRoot "../../QuickBite-Data/script.sql"
$sql = Get-Content -Raw $scriptPath

function Assert-Match {
    param(
        [string]$Pattern,
        [string]$Message
    )

    if ($sql -notmatch $Pattern) {
        throw $Message
    }
}

Assert-Match `
    'PaymentMethod\s+INT\s+NOT\s+NULL\s+DEFAULT\s+0' `
    "Thiếu cột PaymentMethod."
Assert-Match `
    'INSERT\s+INTO\s+Orders\s*\([^)]*PaymentMethod[^)]*\)' `
    "Seed Orders chưa ghi PaymentMethod."
Assert-Match `
    'PaymentMethod\s*=\s*0|PaymentMethod[^\r\n]*Cash' `
    "Thiếu quy ước Cash = 0."
Assert-Match `
    'PaymentMethod\s*=\s*1|PaymentMethod[^\r\n]*BankTransfer' `
    "Thiếu quy ước BankTransfer = 1."
Assert-Match `
    "COL_LENGTH\(N?'dbo\.Orders',\s*N?'PaymentMethod'\)" `
    "Thiếu kiểm tra metadata PaymentMethod."
Assert-Match `
    'PaymentMethod\s+NOT\s+IN\s*\(0,\s*1\)' `
    "Thiếu kiểm tra giá trị ngoài enum."
Assert-Match `
    'Kỳ vọng:\s*6\s*\|\s*30\s*\|\s*7\s*\|\s*15\s*\|\s*1' `
    "Số OrderItems kỳ vọng phải khớp 15 dòng seed thực tế."

Write-Output "PaymentMethod script regression checks passed."

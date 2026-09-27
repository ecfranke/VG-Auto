# Generates backend/src/Carmasters.Http.Api/appsettings.Secrets.json and frontend/.env
# with cryptographically random secrets. Existing files are kept unless -Force is given.
param([switch]$Force)
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")

$AppSettingsExample = "backend/src/Carmasters.Http.Api/appsettings.Secrets.json.example"
$AppSettingsTarget  = "backend/src/Carmasters.Http.Api/appsettings.Secrets.json"
$EnvExample         = "frontend/.env.example"
$EnvTarget          = "frontend/.env"

if (((Test-Path $AppSettingsTarget) -or (Test-Path $EnvTarget)) -and -not $Force) {
    Write-Error "Secrets already exist. Use -Force to regenerate."
}

function New-Secret([int]$bytes) {
    $buffer = New-Object byte[] $bytes
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($buffer)
    return -join ($buffer | ForEach-Object { $_.ToString("x2") })
}

$JwtSecret      = New-Secret 64
$ConsumerSecret = New-Secret 32
$SessionSecret  = New-Secret 32
$DbPassword     = if ($env:DB_PASSWORD) { $env:DB_PASSWORD } else { New-Secret 16 }

$settings = Get-Content $AppSettingsExample -Raw
$settings = $settings.Replace("[your-jwt-secret]", $JwtSecret).Replace("[your-server-secret]", $ConsumerSecret).Replace("[your-db-password]", $DbPassword)
Set-Content -Path $AppSettingsTarget -Value $settings -NoNewline

$envText = Get-Content $EnvExample -Raw
$envText = $envText.Replace("[your-server-secret]", $ConsumerSecret).Replace("[random-32-byte-base64]", $SessionSecret)
Set-Content -Path $EnvTarget -Value $envText -NoNewline

Write-Host "Secrets initialized:"
Write-Host "  - $AppSettingsTarget"
Write-Host "  - $EnvTarget"
Write-Host "Database password: $DbPassword  (create the database user with this password, or edit $AppSettingsTarget)"

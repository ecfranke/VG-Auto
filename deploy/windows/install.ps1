<#
.SYNOPSIS
  Installs or upgrades VG Auto on Windows without Docker.

.DESCRIPTION
  - API: published to <InstallDir>\api and registered as the Windows service "VGAutoApi"
         (or run under pm2 with -ApiUnderPm2).
  - Web: built in <InstallDir>\web and run under pm2; a scheduled task restores pm2 at logon.
  - Database migrations are applied on every run. Configuration in <InstallDir>\config is kept.

  Requirements: .NET 9 SDK, Node.js 20+, PostgreSQL or MySQL 8 reachable from this machine.
  Run from an elevated PowerShell in the repository folder:

    powershell -ExecutionPolicy Bypass -File deploy\windows\install.ps1 -AdminEmail you@example.com

.EXAMPLE
  .\deploy\windows\install.ps1 -DbProvider MySql -DbPassword "..." -AppUrl http://workshop-pc:3000 -AdminEmail boss@example.com
#>
param(
  [string]$InstallDir = "C:\VGAuto",
  [string]$AppUrl = "http://localhost:3000",
  [string]$ApiUrl = "",
  [ValidateSet("PostgreSql", "MySql")][string]$DbProvider = "PostgreSql",
  [string]$DbHost = "localhost",
  [int]$DbPort = 0,
  [string]$DbName = "vgauto",
  [string]$DbUser = "vgauto",
  [string]$DbPassword = "",
  [string]$AdminEmail = "",
  [switch]$ApiUnderPm2,
  [switch]$SkipWeb,
  [switch]$SkipApi,
  [switch]$SkipMigrations
)
$ErrorActionPreference = "Stop"
$RepoDir = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$ConfigDir = Join-Path $InstallDir "config"
$DataDir = Join-Path $InstallDir "data"
$Secrets = Join-Path $ConfigDir "appsettings.Secrets.json"
$WebEnv = Join-Path $ConfigDir "web.env"
$AdminPasswordFile = Join-Path $ConfigDir "initial-admin-password.txt"
$ServiceName = "VGAutoApi"

function Log($message) { Write-Host "`n==> $message" -ForegroundColor Cyan }
function Require($command, $hint) { if (-not (Get-Command $command -ErrorAction SilentlyContinue)) { throw "$command is required. $hint" } }
function New-Secret([int]$bytes) {
  $buffer = New-Object byte[] $bytes
  [System.Security.Cryptography.RandomNumberGenerator]::Fill($buffer)
  return -join ($buffer | ForEach-Object { $_.ToString("x2") })
}
function Protect-File($path) {
  # only Administrators, SYSTEM and the installing user may read configuration with secrets
  icacls $path /inheritance:r /grant:r "Administrators:F" "SYSTEM:F" "$($env:USERNAME):F" | Out-Null
}
function Invoke-Checked([scriptblock]$block, $what) {
  & $block
  if ($LASTEXITCODE -ne 0) { throw "$what failed (exit code $LASTEXITCODE)" }
}

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin -and -not $ApiUnderPm2) { throw "Run PowerShell as Administrator (needed to register the Windows service), or use -ApiUnderPm2." }

Require dotnet "Install the .NET 9 SDK from https://dot.net"
if (-not ((dotnet --list-sdks) -match '^9\.')) { throw ".NET 9 SDK not found (dotnet --list-sdks)." }
if (-not $SkipWeb -or $ApiUnderPm2) {
  Require node "Install Node.js 20+ from https://nodejs.org"
  Require npm ""
  if (-not (Get-Command pm2 -ErrorAction SilentlyContinue)) { Log "Installing pm2"; Invoke-Checked { npm install -g pm2 } "npm install pm2" }
}

New-Item -ItemType Directory -Force -Path $InstallDir, $ConfigDir, (Join-Path $DataDir "pdf"), (Join-Path $DataDir "puppeteer") | Out-Null

if ($DbPort -eq 0) { $DbPort = if ($DbProvider -eq "MySql") { 3306 } else { 5432 } }
if (-not $ApiUrl) { $ApiUrl = "http://$(([Uri]$AppUrl).Host):15567" }

# ---- configuration (first run only) ---------------------------------------------
if (-not (Test-Path $Secrets)) {
  Log "Writing $Secrets"
  if (-not $DbPassword) { $DbPassword = New-Secret 16 }
  $consumer = New-Secret 32
  $settings = [ordered]@{
    JwtOptions = [ordered]@{ Secret = (New-Secret 64); ConsumerSecret = $consumer }
    DbOptions = [ordered]@{ Provider = $DbProvider; Host = $DbHost; Port = $DbPort; UserId = $DbUser; Password = $DbPassword; Name = $DbName; MultiTenancy = @{ Enabled = $false } }
    DefaultAdmin = [ordered]@{ UserName = "admin"; Email = $AdminEmail; Password = "" }
    Email = [ordered]@{
      Provider = "Smtp"; FromAddress = ""; FromName = ""
      Smtp = [ordered]@{ Host = ""; Port = 587; User = ""; Password = ""; Security = "Auto" }
      Graph = [ordered]@{ TenantId = ""; ClientId = ""; ClientSecret = ""; Sender = ""; SaveToSentItems = $true }
    }
    Authentication = [ordered]@{
      EmailCode = @{ RequireForPasswordLogin = $true }
      Microsoft = [ordered]@{ Enabled = $false; ClientId = ""; ClientSecret = ""; TenantId = "common" }
    }
    Cors = [ordered]@{ Mode = "restricted"; AllowedOrigins = @($AppUrl) }
    PdfDirectory = (Join-Path $DataDir "pdf")
    PuppeteerPath = (Join-Path $DataDir "puppeteer")
    Kestrel = @{ Endpoints = @{ Http = @{ Url = "http://0.0.0.0:15567" } } }
  }
  $settings | ConvertTo-Json -Depth 6 | Set-Content -Path $Secrets -Encoding UTF8
  $cookieSecure = if ($AppUrl.StartsWith("https://")) { "true" } else { "false" }
  @(
    "SERVER_SECRET=$consumer"
    "SESSION_SECRET=$(New-Secret 32)"
    "API_URL=http://127.0.0.1:15567"
    "NEXT_PUBLIC_API_URL=$ApiUrl"
    "APP_URL=$AppUrl"
    "COOKIE_SECURE=$cookieSecure"
    "NEXT_PUBLIC_SESSION_TIMEOUT=1500"
    "NEXT_PUBLIC_SESSION_DIALOG_TIMEOUT=120"
  ) | Set-Content -Path $WebEnv -Encoding UTF8
  Protect-File $Secrets
  Protect-File $WebEnv
  Write-Host @"

Configuration created in $ConfigDir.
 - Create the database user '$DbUser' with the password from $Secrets (DbOptions:Password)
   and allow it to create the database '$DbName' (or create it yourself), for example:
     PostgreSQL:  CREATE ROLE "$DbUser" LOGIN PASSWORD '...'; CREATE DATABASE "$DbName" OWNER "$DbUser";
     MySQL:       CREATE DATABASE $DbName CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
                  CREATE USER '$DbUser'@'localhost' IDENTIFIED BY '...'; GRANT ALL ON $DbName.* TO '$DbUser'@'localhost';
 - Configure Email (SMTP or Microsoft Graph) in $Secrets, otherwise nobody receives login codes.
Then run this script again.
"@
  exit 0
}
Log "Using configuration in $ConfigDir"

# ---- API + migrations ---------------------------------------------------------------
$buildDir = Join-Path ([IO.Path]::GetTempPath()) ("vg-auto-build-" + [Guid]::NewGuid().ToString("N"))
try {
  if (-not $SkipApi) {
    Log "Building the API and the migration tool"
    $devSecrets = Join-Path $RepoDir "backend\src\VgAuto.Http.Api\appsettings.Secrets.json"
    if (-not (Test-Path $devSecrets)) { "{}" | Set-Content $devSecrets }   # needed at build time only
    Invoke-Checked { dotnet publish (Join-Path $RepoDir "backend\src\VgAuto.Http.Api\VgAuto.Http.Api.csproj") -c Release -o "$buildDir\api" --nologo -v quiet } "API build"
    Invoke-Checked { dotnet publish (Join-Path $RepoDir "backend\src\DbUp\DbUp.csproj") -c Release -o "$buildDir\dbup" --nologo -v quiet } "DbUp build"

    $service = Get-Service $ServiceName -ErrorAction SilentlyContinue
    if ($service -and $service.Status -eq "Running") { Stop-Service $ServiceName }
    if ($ApiUnderPm2) { pm2 stop vg-auto-api 2>$null | Out-Null }

    foreach ($part in "api", "dbup") {
      $target = Join-Path $InstallDir $part
      New-Item -ItemType Directory -Force -Path $target | Out-Null
      robocopy "$buildDir\$part" $target /MIR /XF appsettings.Secrets.json /XD pdf puppeteer /NFL /NDL /NJH /NJS /NP | Out-Null
      if ($LASTEXITCODE -ge 8) { throw "copying $part failed" }
      Copy-Item $Secrets (Join-Path $target "appsettings.Secrets.json") -Force
      Protect-File (Join-Path $target "appsettings.Secrets.json")
    }
  }

  if (-not $SkipMigrations) {
    Log "Applying database migrations"
    if (-not (Test-Path $AdminPasswordFile)) {
      $script:AdminPasswordCreated = $true
      (New-Secret 8) | Set-Content $AdminPasswordFile -NoNewline
      Protect-File $AdminPasswordFile
    }
    $env:DefaultAdmin__Password = (Get-Content $AdminPasswordFile -Raw).Trim()
    Push-Location (Join-Path $InstallDir "dbup")
    try { Invoke-Checked { dotnet DbUp.dll } "Database migration" } finally { Pop-Location; Remove-Item Env:\DefaultAdmin__Password }
  }

  if (-not $SkipApi -and -not $ApiUnderPm2) {
    Log "Registering Windows service $ServiceName"
    $exe = Join-Path $InstallDir "api\VgAuto.Http.Api.exe"
    if (-not (Get-Service $ServiceName -ErrorAction SilentlyContinue)) {
      New-Service -Name $ServiceName -BinaryPathName "`"$exe`"" -DisplayName "VG Auto API" -StartupType Automatic | Out-Null
      sc.exe failure $ServiceName reset= 86400 actions= restart/10000/restart/10000/restart/60000 | Out-Null
    }
    Start-Service $ServiceName
  }
}
finally {
  if (Test-Path $buildDir) { Remove-Item $buildDir -Recurse -Force }
}

# ---- web app ---------------------------------------------------------------------------
$webDir = Join-Path $InstallDir "web"
if (-not $SkipWeb) {
  Log "Building the web app"
  New-Item -ItemType Directory -Force -Path $webDir | Out-Null
  robocopy (Join-Path $RepoDir "frontend") $webDir /MIR /XD node_modules .next /XF .env /NFL /NDL /NJH /NJS /NP | Out-Null
  if ($LASTEXITCODE -ge 8) { throw "copying the web app failed" }
  Copy-Item $WebEnv (Join-Path $webDir ".env") -Force
  Protect-File (Join-Path $webDir ".env")
  Push-Location $webDir
  try {
    Invoke-Checked { npm ci --no-audit --no-fund } "npm ci"
    Invoke-Checked { npm run build } "web build"   # NEXT_PUBLIC_* values are compiled in
  } finally { Pop-Location }
}

# ---- pm2 ----------------------------------------------------------------------------------
$apps = @()
if (Test-Path (Join-Path $webDir ".next")) {
  $apps += @"
  {
    name: 'vg-auto-web',
    cwd: '$($webDir -replace '\\','/')',
    script: 'node_modules/next/dist/bin/next',
    args: 'start -p 3000',
    env: { NODE_ENV: 'production' },
  }
"@
}
if ($ApiUnderPm2) {
  $apps += @"
  {
    name: 'vg-auto-api',
    cwd: '$((Join-Path $InstallDir 'api') -replace '\\','/')',
    script: '$((Join-Path $InstallDir 'api\VgAuto.Http.Api.exe') -replace '\\','/')',
    interpreter: 'none',
    env: { ASPNETCORE_ENVIRONMENT: 'Production' },
  }
"@
}
if ($apps.Count -gt 0) {
  $ecosystem = Join-Path $InstallDir "ecosystem.config.cjs"
  "module.exports = { apps: [`n$($apps -join ",`n")`n] };" | Set-Content $ecosystem -Encoding UTF8
  Log "Starting with pm2"
  Invoke-Checked { pm2 startOrReload $ecosystem --update-env } "pm2 start"
  Invoke-Checked { pm2 save } "pm2 save"

  # pm2 has no Windows service of its own: restore the saved processes at logon
  $pm2 = (Get-Command pm2).Source
  $action = New-ScheduledTaskAction -Execute "cmd.exe" -Argument "/c `"$pm2`" resurrect"
  $trigger = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME
  Register-ScheduledTask -TaskName "VG Auto pm2" -Action $action -Trigger $trigger -Description "Starts the VG Auto web app" -Force | Out-Null
}

Log "Done"
$webEnvContent = Get-Content $WebEnv
$appUrlNow = ($webEnvContent | Where-Object { $_ -like "APP_URL=*" }) -replace "^APP_URL=", ""
$apiUrlNow = ($webEnvContent | Where-Object { $_ -like "NEXT_PUBLIC_API_URL=*" }) -replace "^NEXT_PUBLIC_API_URL=", ""
Write-Host "  App:            $appUrlNow"
Write-Host "  API:            $apiUrlNow   (health: /health)"
Write-Host "  Configuration:  $ConfigDir  (after editing it, copy appsettings.Secrets.json by running this script again)"
if ($script:AdminPasswordCreated) {
  Write-Host "  First login:    user 'admin', password in $AdminPasswordFile (you must change it; delete the file afterwards)"
}
Write-Host "  Open the ports 3000 and 15567 in the Windows firewall if other computers should reach the app."

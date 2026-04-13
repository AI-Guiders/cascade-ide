# One-off: OmniSharp folder + Marksman on User PATH (run: powershell -File ...)
$ErrorActionPreference = "Stop"
$omni = "C:\Users\dkara\Tools\omnisharp-win-x64-net6.0"
$marksDir = "C:\Users\dkara\Tools\marksman"
New-Item -ItemType Directory -Force -Path $marksDir | Out-Null

$marksExe = Join-Path $marksDir "marksman.exe"
if (-not (Test-Path $marksExe)) {
    Write-Host "Downloading Marksman 2026-02-08..."
    Invoke-WebRequest -Uri "https://github.com/artempyanykh/marksman/releases/download/2026-02-08/marksman.exe" -OutFile $marksExe -UseBasicParsing
}

function Add-UserPath {
    param([string]$dir)
    $userPath = [Environment]::GetEnvironmentVariable("Path", "User")
    if ([string]::IsNullOrEmpty($userPath)) { $userPath = "" }
    $parts = $userPath -split ";" | ForEach-Object { $_.Trim() } | Where-Object { $_ }
    foreach ($p in $parts) {
        if ($p -ieq $dir) {
            Write-Host "PATH already contains: $dir"
            return
        }
    }
    $newPath = if ([string]::IsNullOrWhiteSpace($userPath)) { $dir } else { $userPath.TrimEnd(";") + ";" + $dir }
    [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
    Write-Host "Added to User PATH: $dir"
}

$omniExe = Join-Path $omni "OmniSharp.exe"
if (-not (Test-Path $omniExe)) {
    Write-Error "OmniSharp.exe not found under $omni - unpack omnisharp-win-x64-net6.0.zip there first."
}
Add-UserPath $omni
Add-UserPath $marksDir

$machinePath = [Environment]::GetEnvironmentVariable("Path", "Machine")
$userPathOnly = [Environment]::GetEnvironmentVariable("Path", "User")
$env:Path = $machinePath + ";" + $userPathOnly

Write-Host ""
Write-Host "Smoke:"
& $omniExe -v 2>&1 | Select-Object -First 3
& $marksExe --version 2>&1

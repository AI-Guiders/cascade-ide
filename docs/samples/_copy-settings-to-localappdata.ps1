$ErrorActionPreference = "Stop"
$src = Join-Path $PSScriptRoot "settings.localappdata.example.toml"
$destDir = Join-Path $env:LOCALAPPDATA "CascadeIDE"
$dest = Join-Path $destDir "settings.toml"
New-Item -ItemType Directory -Force -Path $destDir | Out-Null
Copy-Item -LiteralPath $src -Destination $dest -Force
Write-Host "Copied to $dest"
Get-Item $dest | Format-List FullName, LastWriteTime, Length
Get-Content $dest -TotalCount 6

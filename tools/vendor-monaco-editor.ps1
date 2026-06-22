# Vendors monaco-editor min/vs into Assets/cide-editor/monaco/min/vs (offline host, ADR 0162).
param(
    [string]$Version = "0.52.2"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$TargetRoot = Join-Path $repoRoot "Assets\cide-editor\monaco\min\vs"
$work = Join-Path $env:TEMP "cide-monaco-vendor-$Version"
if (Test-Path $work) { Remove-Item $work -Recurse -Force }
New-Item -ItemType Directory -Path $work | Out-Null

Push-Location $work
try {
    npm pack "monaco-editor@$Version" --silent 2>$null | Out-Null
    $tgz = Get-ChildItem -Filter "monaco-editor-*.tgz" | Select-Object -First 1
    if (-not $tgz) { throw "npm pack did not produce monaco-editor tarball" }
    tar -xf $tgz.FullName
    $src = Join-Path (Join-Path $work "package") "min\vs"
    if (-not (Test-Path $src)) { throw "package/min/vs missing in $Version" }

    if (Test-Path $TargetRoot) { Remove-Item $TargetRoot -Recurse -Force }
    New-Item -ItemType Directory -Path $TargetRoot -Force | Out-Null
    Copy-Item -Path (Join-Path $src "*") -Destination $TargetRoot -Recurse -Force
    Write-Host "Monaco $Version vendored to $TargetRoot"
}
finally {
    Pop-Location
    if (Test-Path $work) { Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue }
}

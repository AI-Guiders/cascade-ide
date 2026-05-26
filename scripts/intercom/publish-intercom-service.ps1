# Publish reference intercom-service (self-contained win-x64 by default).
# Run from repo root:  .\scripts\intercom\publish-intercom-service.ps1
# Optional: -Configuration Debug  -OutDir "D:\cascade-ide\tools\intercom-service"
[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",

    [string] $Runtime = "win-x64",

    [switch] $SelfContained,

    [string] $OutDir = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$csproj = Join-Path $repoRoot "host\intercom-service\src\IntercomService\IntercomService.csproj"
if (-not (Test-Path -LiteralPath $csproj)) {
    Write-Error "IntercomService.csproj not found at $csproj"
    exit 1
}

if ([string]::IsNullOrWhiteSpace($OutDir)) {
    $OutDir = Join-Path $repoRoot "artifacts\intercom-service"
}

$publishArgs = @(
    "publish", $csproj,
    "-c", $Configuration,
    "-r", $Runtime,
    "-o", $OutDir,
    "-v", "minimal"
)
if ($SelfContained) {
    $publishArgs += "--self-contained", "true"
} else {
    $publishArgs += "--self-contained", "false"
}

Push-Location $repoRoot
try {
    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    $exe = Join-Path $OutDir "IntercomService.exe"
    if (-not (Test-Path -LiteralPath $exe)) {
        Write-Error "Expected exe not found: $exe"
        exit 1
    }

    $ts = (Get-Item -LiteralPath $exe).LastWriteTimeUtc.ToString("o")
    Write-Host "OK: $exe  (UTC $ts)"
    Write-Host "Deploy: publish-release.ps1 copies to <Target>/tools/intercom-service/ (default local_server_path in settings.toml)."
} finally {
    Pop-Location
}

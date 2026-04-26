# Publish Release (self-contained win-x64) and mirror to a fixed path without spaces (for Cursor MCP).
# Run from repo root:  cd ...\cascade-ide  ;  .\scripts\deploy\publish-release.ps1
# Optional: -SkipDocGen  (faster when IdeCommands XML-doc / MCP markdown codegen not changed)
# Optional: -Target "D:\cascade-ide"
[CmdletBinding()]
param(
    [string] $Target = "D:\cascade-ide",
    [switch] $SkipDocGen
)

$ErrorActionPreference = "Stop"
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$csproj = Join-Path $repoRoot "CascadeIDE.csproj"
if (-not (Test-Path -LiteralPath $csproj)) {
    Write-Error "CascadeIDE.csproj not found. Run from cascade-ide repo (resolved root=$repoRoot)."
    exit 1
}

$outDir = Join-Path $repoRoot "publish"

Push-Location $repoRoot
try {
    $publishArgs = @(
        "publish", $csproj,
        "-c", "Release",
        "-r", "win-x64",
        "--self-contained", "true",
        "-o", $outDir,
        "-v", "minimal"
    )
    if ($SkipDocGen) {
        $publishArgs += "/p:GenerateIdeProtocolDocs=false"
    }

    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if (-not (Test-Path -LiteralPath $Target)) {
        New-Item -ItemType Directory -Path $Target -Force | Out-Null
    }

    robocopy $outDir $Target /E /MIR /NFL /NDL /NJH /NJS | Out-Null
    $robocode = $LASTEXITCODE
    if ($robocode -ge 8) {
        Write-Error "robocopy failed with exit code $robocode"
        exit $robocode
    }

    $exe = Join-Path $Target "CascadeIDE.exe"
    if (-not (Test-Path -LiteralPath $exe)) {
        Write-Error "Expected exe not found: $exe"
        exit 1
    }

    $ts = (Get-Item -LiteralPath $exe).LastWriteTimeUtc.ToString("o")
    $exeJson = $exe.Replace('\', '\\')
    Write-Host ""
    Write-Host "OK: $exe  (UTC $ts)"
    Write-Host ""
    Write-Host "Cursor MCP (Release): paste into mcp.json ->"
    Write-Host @"
  "cascade-ide": {
    "command": "$exeJson",
    "args": ["--mcp-stdio"]
  }
"@
    Write-Host ""
} finally {
    Pop-Location
}

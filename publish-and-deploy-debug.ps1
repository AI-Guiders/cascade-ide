# Publish Debug and copy to a fixed path without spaces (for Cursor MCP).
# Run from repo:  cd ...\cascade-ide  ;  .\publish-and-deploy-debug.ps1
# Optional: -SkipDocGen  (faster when IdeCommands XML-doc / MCP markdown codegen not changed)
# Optional: -Target "D:\cascade-ide-debug"
[CmdletBinding()]
param(
    [string] $Target = "D:\cascade-ide-debug",
    [switch] $SkipDocGen
)

$ErrorActionPreference = "Stop"
$here = $PSScriptRoot
$outDir = Join-Path $here "publish-debug"

Push-Location $here
try {
    $publishArgs = @(
        "publish", (Join-Path $here "CascadeIDE.csproj"),
        "-c", "Debug",
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

    if (-not (Test-Path $Target)) {
        New-Item -ItemType Directory -Path $Target -Force | Out-Null
    }

    # Copy so Cursor runs the build we just published (avoids junction/space issues)
    robocopy $outDir $Target /E /MIR /NFL /NDL /NJH /NJS
    if ($LASTEXITCODE -ge 8) { exit $LASTEXITCODE }

    $exe = Join-Path $Target "CascadeIDE.exe"
    $ts = (Get-Item $exe).LastWriteTimeUtc.ToString("o")
    $exeJson = $exe.Replace('\', '\\')
    Write-Host ""
    Write-Host "OK: $exe  (UTC $ts)"
    Write-Host ""
    Write-Host "Cursor MCP (debug): paste into mcp.json ->"
    Write-Host @"
  "cascade-ide-debug": {
    "command": "$exeJson",
    "args": ["--mcp-stdio"]
  }
"@
    Write-Host ""
} finally {
    Pop-Location
}

# Publish Debug and copy to D:\cascade-ide-debug (path without spaces for Cursor MCP).
# Run from cascade-ide folder. If D:\cascade-ide-debug was a junction, remove it first and create a normal folder.
$ErrorActionPreference = "Stop"
$here = $PSScriptRoot
$target = "D:\cascade-ide-debug"

Push-Location $here
try {
    dotnet publish -c Debug -o publish-debug
    if (-not (Test-Path $target)) {
        New-Item -ItemType Directory -Path $target -Force
    }
    # Copy so Cursor runs the build we just published (avoids junction/space issues)
    robocopy (Join-Path $here "publish-debug") $target /E /MIR /NFL /NDL /NJH /NJS
    if ($LASTEXITCODE -ge 8) { exit $LASTEXITCODE }
    Write-Host "Done. Check: (Get-Item $target\CascadeIDE.exe).LastWriteTime"
} finally {
    Pop-Location
}

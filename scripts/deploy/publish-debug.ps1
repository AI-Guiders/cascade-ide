# Publish Debug and copy to a fixed path without spaces (for Cursor MCP).
# Also builds samples/DebugTarget first (test target for DAP).
# Run from repo root:  cd ...\cascade-ide  ;  .\scripts\deploy\publish-debug.ps1
# Optional: -SkipDocGen  (faster when IdeCommands XML-doc / MCP markdown codegen not changed)
# Optional: -Target "D:\cascade-ide-debug"
# Optional: -KillRunning  (stop CascadeIDE from Target path if it locks publish output)
[CmdletBinding()]
param(
    [string] $Target = "D:\cascade-ide-debug",
    [switch] $SkipDocGen,
    [switch] $KillRunning
)

$ErrorActionPreference = "Stop"
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$csproj = Join-Path $repoRoot "CascadeIDE.csproj"
if (-not (Test-Path -LiteralPath $csproj)) {
    Write-Error "CascadeIDE.csproj not found. Run from cascade-ide repo (resolved root=$repoRoot)."
    exit 1
}

$debugTargetProj = Join-Path $repoRoot "samples\DebugTarget\DebugTarget.csproj"
$generic = Join-Path $repoRoot "scripts\deploy\publish-to-fixed-target.ps1"

$intercomPublish = Join-Path $repoRoot "scripts\intercom\publish-intercom-service.ps1"
$intercomOut = Join-Path $repoRoot "artifacts\intercom-service"

Push-Location $repoRoot
try {
    if (Test-Path -LiteralPath $intercomPublish) {
        & pwsh -NoProfile -ExecutionPolicy Bypass -File $intercomPublish -Configuration Debug -SelfContained -OutDir $intercomOut
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    if (-not (Test-Path -LiteralPath $debugTargetProj)) {
        Write-Error "DebugTarget project not found: $debugTargetProj"
        exit 1
    }
    & dotnet build $debugTargetProj -c Debug -v minimal
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if (-not (Test-Path -LiteralPath $generic)) {
        Write-Error "Generic publish script not found: $generic"
        exit 1
    }

    if ($SkipDocGen) {
        & pwsh -NoProfile -ExecutionPolicy Bypass -File $generic `
            -Project $csproj `
            -Runtime "win-x64" `
            -Configuration "Debug" `
            -Target $Target `
            -SelfContained `
            -KillRunning:$KillRunning `
            -MsbuildProps "/p:GenerateIdeProtocolDocs=false"
    } else {
        & pwsh -NoProfile -ExecutionPolicy Bypass -File $generic `
            -Project $csproj `
            -Runtime "win-x64" `
            -Configuration "Debug" `
            -Target $Target `
            -SelfContained `
            -KillRunning:$KillRunning
    }
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if (Test-Path -LiteralPath $intercomOut) {
        $intercomTarget = Join-Path $Target "tools\intercom-service"
        New-Item -ItemType Directory -Path $intercomTarget -Force | Out-Null
        robocopy $intercomOut $intercomTarget /E /MIR /NFL /NDL /NJH /NJS | Out-Null
        if ($LASTEXITCODE -ge 8) {
            Write-Error "robocopy intercom-service failed with exit code $LASTEXITCODE"
            exit $LASTEXITCODE
        }
    }

    $exe = Join-Path $Target "CascadeIDE.exe"
    $exeJson = $exe.Replace('\', '\\')
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

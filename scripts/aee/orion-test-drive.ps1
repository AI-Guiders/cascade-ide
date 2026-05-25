# Orion AEE local test-drive (ADR 0148)
# Usage: pwsh -File scripts/aee/orion-test-drive.ps1 [-Ui] [-Filter name]

param(
    [switch]$Ui,
    [string]$Filter = "Category=AgentEnvironment"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

Write-Host "=== Orion AEE test-drive ===" -ForegroundColor Cyan
Write-Host "Repo: $repoRoot"
Write-Host ""

Write-Host "[1/3] Automated stress (xUnit)..." -ForegroundColor Yellow
Push-Location $repoRoot
try {
    dotnet test "CascadeIDE.Tests\CascadeIDE.Tests.csproj" --filter $Filter -v minimal
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "[2/3] MLP notes (Orion vs current build)" -ForegroundColor Yellow
Write-Host @"
- Coalesce 1.5s applies to L2 build dedup inside one verify, not to stacking /agent verify.
  Rapid verify SUPERCEDES predecessor (implicit cancel), not a queue of CancellationTokens.
- Substrate: %LocalAppData%/CascadeIDE/agent-runs/{run_id}/substrate (wit.db + port.txt).
  Parallel runs must get distinct DevPort values (automated test above).
- Supervised host is in-proc (supervised-inproc) until separate MSBuild worker ships.
  Host death => AgentEnvironmentTaskDied on DataBus; PFD strip listens via RefreshPfdBackgroundStatusBar.
"@

if ($Ui) {
    Write-Host ""
    Write-Host "[3/3] Manual UI checklist" -ForegroundColor Yellow
    Write-Host @"
1. Open CascadeIDE.sln, open any .cs tab.
2. Run /agent verify standard — watch PFD strip (AEE verify …).
3. While running, edit .cs every ~500ms — expect stale epoch (DataBus); /agent status still works.
4. Spam /agent verify minimal — only one active; prior runs superseded.
5. /agent sandbox agent_ephemeral — check agent-runs folder under LocalAppData.
6. Cancel: /agent cancel — strip clears; chat trace on completion if show_in_chat=true.
"@
    Write-Host "Launch IDE: dotnet run --project CascadeIDE.csproj" -ForegroundColor DarkGray
}
else {
    Write-Host ""
    Write-Host "[3/3] Skipped UI (-Ui to print manual checklist)" -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "Done." -ForegroundColor Green

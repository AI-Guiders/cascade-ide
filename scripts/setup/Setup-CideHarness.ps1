#Requires -Version 7.0
<#
.SYNOPSIS
  Idempotent harness setup for CIDE «CASA / Neumann T1» (interim, ADR 0166 §5.1).

.DESCRIPTION
  - Ensures %LocalAppData%\CascadeIDE exists
  - Merges harness-neumann-t1.overlay.toml keys into settings.toml
  - Deploys harness-external-mcp.json (optional roslyn + python stdio)
  - Does NOT install Cloud.ru keys (ai-keys.toml) — operator only

.PARAMETER Apply
  Write files. Without -Apply, dry-run only.

.PARAMETER AgentNotesConfigPath
  Path to agent-notes-mcp.toml (same as Cursor --config).

.PARAMETER SkipExternalMcp
  Skip copying optional external MCP JSON (roslyn/python only).

.PARAMETER RoslynMcpExe
  Override roslyn MCP executable path in external JSON.

.PARAMETER PythonMcpExe
  Override python MCP executable path in external JSON.
#>
[CmdletBinding()]
param(
    [switch]$Apply,
    [string]$AgentNotesConfigPath = $env:CIDE_AGENT_NOTES_CONFIG,
    [switch]$SkipExternalMcp,
    [string]$RoslynMcpExe = $env:CIDE_ROSLYN_MCP_EXE,
    [string]$PythonMcpExe = $env:CIDE_PYTHON_MCP_EXE
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$samples = Join-Path $repoRoot 'docs\samples'
$cideDir = Join-Path $env:LOCALAPPDATA 'CascadeIDE'
$settingsPath = Join-Path $cideDir 'settings.toml'
$overlayPath = Join-Path $samples 'harness-neumann-t1.overlay.toml'
$externalTemplate = Join-Path $samples 'harness-external-mcp.optional.json'
$externalDest = Join-Path $cideDir 'harness-external-mcp.json'

function Write-Step([string]$Message) { Write-Host "→ $Message" }

function Merge-TomlBlock {
    param(
        [string]$Existing,
        [string]$Overlay
    )
    # Append overlay if settings missing; if exists, append overlay as commented block + key overrides for known sections.
    if (-not $Existing) {
        return $Overlay
    }
    $marker = "# --- harness-neumann-t1 overlay (Setup-CideHarness.ps1) ---"
    if ($Existing -match [regex]::Escape($marker)) {
        $before = ($Existing -split [regex]::Escape($marker))[0].TrimEnd()
        return "$before`n`n$marker`n$Overlay"
    }
    return "$($Existing.TrimEnd())`n`n$marker`n$Overlay"
}

Write-Step "CIDE harness setup (Apply=$Apply)"
Write-Step "Target: $cideDir"

if (-not (Test-Path $overlayPath)) {
    throw "Missing overlay: $overlayPath"
}

$overlay = Get-Content -Raw -Path $overlayPath
if ($AgentNotesConfigPath) {
    $overlay = $overlay -replace 'config_path = "D:/agent-notes-mcp/agent-notes-mcp.toml"',
        "config_path = `"$($AgentNotesConfigPath.Replace('\', '/'))`""
}

if (-not $Apply) {
    Write-Host "`n[DRY-RUN] Would ensure directory: $cideDir"
    Write-Host "[DRY-RUN] Would merge overlay into: $settingsPath"
    if (-not $SkipExternalMcp) {
        Write-Host "[DRY-RUN] Would write: $externalDest"
    }
    Write-Host "`nRe-run with -Apply. Set -AgentNotesConfigPath or env CIDE_AGENT_NOTES_CONFIG."
    exit 0
}

New-Item -ItemType Directory -Force -Path $cideDir | Out-Null

$existingSettings = ''
if (Test-Path $settingsPath) {
    $existingSettings = Get-Content -Raw -Path $settingsPath
}
$merged = Merge-TomlBlock -Existing $existingSettings -Overlay $overlay
Set-Content -Path $settingsPath -Value $merged -Encoding utf8NoBOM
Write-Step "Updated $settingsPath"

if (-not $SkipExternalMcp) {
    if (-not (Test-Path $externalTemplate)) {
        throw "Missing template: $externalTemplate"
    }
    $json = Get-Content -Raw -Path $externalTemplate | ConvertFrom-Json
    foreach ($entry in $json) {
        if ($entry.name -eq 'roslyn' -and $RoslynMcpExe) { $entry.command = $RoslynMcpExe }
        if ($entry.name -eq 'python' -and $PythonMcpExe) { $entry.command = $PythonMcpExe }
    }
    $json | ConvertTo-Json -Depth 5 | Set-Content -Path $externalDest -Encoding utf8
    Write-Step "Wrote $externalDest (edit paths if exe not built yet)"
}

Write-Host @"

Done. Manual steps:
  1. ai.mode — НЕ меняется overlay (local | mcp_only | cloud | acp). Cloud FM: harness-cloud-fm.overlay.toml + ai-keys.toml
  2. Smoke harness: read_hot_context / ide_agent_status (in-proc; mcp_only = внешний MCP-клиент)
  3. Cursor until ~Aug 2026: hooks.json for checkpoint
  4. ACP: suppress_acp_ide_stdio_inject in [agent.harness] (0082 loopback backlog)

"@

$root = "$env:LOCALAPPDATA\cdp-mcp"
$now = [DateTimeOffset]::UtcNow.ToString("o")
$path = "D:\Experiments\Personal Cursor Folder\Financial\software\open\cascade-ide\CascadeIDE.GlassCore\SoftInstrument\GlassEditorSituRibbon.cs"

@{
  schema = "cide_presentation_latch/v1"
  topology = "(F)(P/M)"
  tier = "cockpit"
  mfd_page = "Editor"
  origin = "agent"
  stamped_utc = $now
} | ConvertTo-Json | Set-Content "$root\presentation-LATEST.json" -Encoding utf8

@{
  schema = "navigation_land_latch/v1"
  command = "open"
  path = $path
  wire = "[Family:navigation;Command:open;Anchor:[File:$path]]"
  stamped_utc = $now
} | ConvertTo-Json | Set-Content "$root\land-LATEST.json" -Encoding utf8

# Touch plan so WHY cache refreshes (no Prefer OneOf anymore)
$planPath = "$root\plan-LATEST.json"
if (Test-Path $planPath) {
  $plan = Get-Content $planPath -Raw | ConvertFrom-Json
  $plan.stamped_utc = [DateTimeOffset]::UtcNow.ToString("o")
  $plan | ConvertTo-Json -Depth 8 | Set-Content $planPath -Encoding utf8
}

Start-Sleep -Milliseconds 800
Write-Output "latches stamped $now"
Get-Process CDP.GlassCockpit.Windows -ErrorAction SilentlyContinue | Select-Object Id,StartTime

# Live dogfood: Glass CIT lane → citizen dialog latch → bridge → Intercom human surface
param(
    [string]$Message = 'full-ready-e2e-1105 ping',
    [int]$WaitSeconds = 120,
    [string]$OutPath = ''
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

$p = Get-Process CDP.GlassCockpit.Windows -ErrorAction Stop | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)

function Click-Name([string]$Name) {
    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $Name)
    $el = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
    if (-not $el) { throw "no button: $Name" }
    try {
        $inv = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $inv.Invoke()
    }
    catch {
        $sel = $el.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
        $sel.Select()
    }
    Start-Sleep -Milliseconds 300
}

Click-Name 'CIT'
Start-Sleep -Milliseconds 800

$editCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Edit)
$edits = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)
if ($edits.Count -lt 1) { throw 'no edit controls' }

$composer = $null
foreach ($e in $edits) {
    if ($e.Current.IsEnabled) { $composer = $e; break }
}
if (-not $composer) { $composer = $edits[$edits.Count - 1] }

$val = $composer.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
$val.SetValue($Message)
Start-Sleep -Milliseconds 400
Click-Name 'Send'
Write-Output "SENT msg=$Message pid=$($p.Id)"

$latch = Join-Path $env:LOCALAPPDATA 'cdp-mcp/citizen-dialog-request-LATEST.json'
$deadline = (Get-Date).AddSeconds($WaitSeconds)
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 2
    if (Test-Path $latch) {
        $j = Get-Content $latch -Raw | ConvertFrom-Json
        Write-Output "status=$($j.status) id=$($j.id)"
        if ($j.status -in @('done', 'error')) { break }
    }
}

if (Test-Path $latch) {
    Write-Output '---LATCH---'
    Get-Content $latch -Raw
}

$journal = Join-Path $env:LOCALAPPDATA 'cdp-mcp/intercom-journal.jsonl'
if (Test-Path $journal) {
    Write-Output '---JOURNAL_TAIL---'
    Get-Content $journal -Tail 5
}

if ($OutPath) {
    $cap = Join-Path $PSScriptRoot 'Capture-Window.ps1'
    & $cap -Process CDP.GlassCockpit.Windows -Title 'CDP GlassCockpit' -OutPath $OutPath
    Write-Output "PNG=$OutPath exists=$(Test-Path $OutPath)"
}

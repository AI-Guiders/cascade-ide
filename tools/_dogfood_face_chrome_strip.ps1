# Live dogfood: Radio feed → flat chrome (no topic strip / no bubble) → PrintWindow
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

$p = Get-Process CDP.GlassCockpit.Windows -ErrorAction Stop | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)

function Click-Name([string]$Name) {
    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $Name)
    $el = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
    if (-not $el) { throw "no button: $Name" }
    $inv = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $inv.Invoke()
    Start-Sleep -Milliseconds 400
}

Click-Name 'Radio'
Start-Sleep -Seconds 1

$out = Join-Path $PSScriptRoot '..\tmp-glass-shots\face-chrome-strip-20260806.png'
$out = [IO.Path]::GetFullPath($out)
New-Item -ItemType Directory -Force -Path (Split-Path $out) | Out-Null
& (Join-Path $PSScriptRoot 'Capture-Window.ps1') -Process CDP.GlassCockpit.Windows -Title 'CDP GlassCockpit' -OutPath $out

$names = @()
$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Button)
foreach ($b in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)) {
    $n = $b.Current.Name
    if ($n -match 'Topics|#crew|Radio|DM|Overview|Back') { $names += $n }
}

Write-Output "PNG=$out exists=$(Test-Path $out)"
Write-Output "UIA=$($names -join ' | ')"

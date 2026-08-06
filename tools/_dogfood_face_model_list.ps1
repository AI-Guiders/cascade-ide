# Live dogfood: CIT → Models directory visible → PrintWindow
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

Click-Name 'CIT'
Start-Sleep -Seconds 1

$out = Join-Path $PSScriptRoot '..\tmp-glass-shots\face-model-list-cit-20260806.png'
$out = [IO.Path]::GetFullPath($out)
New-Item -ItemType Directory -Force -Path (Split-Path $out) | Out-Null
& (Join-Path $PSScriptRoot 'Capture-Window.ps1') -Process CDP.GlassCockpit.Windows -Title 'CDP GlassCockpit' -OutPath $out

$names = @()
$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Text)
foreach ($t in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)) {
    $n = $t.Current.Name
    if ($n -match 'Models|GLM|Qwen|default') { $names += $n }
}

Write-Output "PNG=$out exists=$(Test-Path $out)"
Write-Output "UIA=$($names -join ' | ')"

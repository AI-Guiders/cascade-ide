$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class SoftFgSe {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool AllowSetForegroundWindow(int pid);
}
'@

$p = Get-Process CDP.GlassCockpit.Windows -ErrorAction Stop | Select-Object -First 1
[void][SoftFgSe]::AllowSetForegroundWindow(-1)
[void][SoftFgSe]::ShowWindow($p.MainWindowHandle, 9)
[void][SoftFgSe]::SetForegroundWindow($p.MainWindowHandle)
Start-Sleep -Milliseconds 400

$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)

function Invoke-Named($name) {
  $el = $root.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
      [System.Windows.Automation.AutomationElement]::NameProperty, $name)))
  if (-not $el) { return $false }
  try {
    $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    return $true
  } catch { return $false }
}

# Soft chord path: type "solution" or open SE via slash /solution explorer show
$edit = $root.FindFirst(
  [System.Windows.Automation.TreeScope]::Descendants,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'ComposerBox')))
if (-not $edit) { throw 'ComposerBox missing' }

$radio = $root.FindFirst(
  [System.Windows.Automation.TreeScope]::Descendants,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, 'Radio')))
if ($radio) {
  try { $radio.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() } catch {}
  Start-Sleep -Milliseconds 200
}

$edit.SetFocus()
Start-Sleep -Milliseconds 150
$vp = $edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
$vp.SetValue('/solution explorer show')
Start-Sleep -Milliseconds 200
Write-Output "COMPOSER=$($vp.Current.Value)"
$send = $root.FindFirst(
  [System.Windows.Automation.TreeScope]::Descendants,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, 'Send')))
$send.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Start-Sleep -Milliseconds 1500

$filter = $root.FindFirst(
  [System.Windows.Automation.TreeScope]::Descendants,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'MfdSolutionExplorerFilter')))
if (-not $filter) {
  # Probe names for SE page
  $probe = @()
  foreach ($el in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    $n = $el.Current.Name
    $aid = $el.Current.AutomationId
    if (($n -and ($n -match 'Solution|SE |Filter')) -or ($aid -match 'Solution|Filter')) {
      $probe += "aid=$aid name=$n"
    }
  }
  Write-Output "FILTER=missing PROBE=$($probe.Count)"
  $probe | Select-Object -First 30 | ForEach-Object { Write-Output "P=$_" }
  throw 'MfdSolutionExplorerFilter not found — SE Face not visible'
}

$filter.SetFocus()
Start-Sleep -Milliseconds 150
$fvp = $filter.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
$needle = 'MainWindow'
$fvp.SetValue($needle)
# WPF TextChanged may need keyboard nudge after ValuePattern
[System.Windows.Forms.SendKeys]::SendWait('{END} ')
Start-Sleep -Milliseconds 100
[System.Windows.Forms.SendKeys]::SendWait('{BACKSPACE}')
Start-Sleep -Milliseconds 800
Write-Output "FILTER=$($fvp.Current.Value)"

$names = @()
foreach ($el in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
  $n = $el.Current.Name
  if ($n -and ($n -match 'SolutionExplorer|MainWindow|Filter|M ·')) { $names += $n }
}
Write-Output "PROBE=$($names.Count)"
$names | Select-Object -First 25 | ForEach-Object { Write-Output "N=$_" }

$out = 'D:\Experiments\Personal Cursor Folder\Financial\software\open\cascade-ide\tmp-glass-shots\se-filter-textbox-20260808.png'
& 'D:\Experiments\Personal Cursor Folder\Financial\software\open\cascade-ide\tools\_cap_copyfromscreen.ps1' `
  -Process CDP.GlassCockpit.Windows -Title 'GlassCockpit' -OutPath $out
Write-Output "SHOT=$out SIZE=$((Get-Item -LiteralPath $out).Length)"

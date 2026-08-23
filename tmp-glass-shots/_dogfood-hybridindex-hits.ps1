$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class SoftFgHi {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool AllowSetForegroundWindow(int pid);
}
'@

$sln = 'D:\Experiments\Personal Cursor Folder\Financial\software\open\cascade-ide\CascadeIDE.sln'
$p = Get-Process CDP.GlassCockpit.Windows -ErrorAction Stop | Select-Object -First 1
[void][SoftFgHi]::AllowSetForegroundWindow(-1)
[void][SoftFgHi]::ShowWindow($p.MainWindowHandle, 9)
[void][SoftFgHi]::SetForegroundWindow($p.MainWindowHandle)
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

function Set-Composer($text) {
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
    Start-Sleep -Milliseconds 150
  }
  $edit.SetFocus()
  Start-Sleep -Milliseconds 100
  $vp = $edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
  $vp.SetValue($text)
  Start-Sleep -Milliseconds 150
  $send = $root.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
      [System.Windows.Automation.AutomationElement]::NameProperty, 'Send')))
  if (-not $send) { throw 'Send missing' }
  $send.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
}

# Load full cascade-ide workspace (not tmp fixture)
Set-Composer("/solution load `"$sln`"")
Start-Sleep -Milliseconds 2500
Write-Output 'SOLUTION_LOAD_SENT'

# Prefer M then HybridIndex via palette
[System.Windows.Forms.SendKeys]::SendWait('^q')
Start-Sleep -Milliseconds 500
[System.Windows.Forms.SendKeys]::SendWait('MFD: HybridIndex')
Start-Sleep -Milliseconds 400
[System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
Start-Sleep -Milliseconds 900

# Prefer M seat (PrintWindow Face)
Invoke-Named 'Prefer M' | Out-Null
Start-Sleep -Milliseconds 400

$box = $root.FindFirst(
  [System.Windows.Automation.TreeScope]::Descendants,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'HybridIndexSearchBox')))
if (-not $box) {
  # fallback: any Edit under HybridIndex host
  $probe = @()
  foreach ($el in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    $aid = $el.Current.AutomationId
    $n = $el.Current.Name
    if ($aid -match 'Hybrid' -or $n -match 'Hybrid|HCI|search') {
      $probe += "aid=$aid name=$n"
    }
  }
  Write-Output "SEARCHBOX=missing PROBE=$($probe.Count)"
  $probe | Select-Object -First 40 | ForEach-Object { Write-Output "P=$_" }
  throw 'HybridIndexSearchBox missing'
}

$box.SetFocus()
Start-Sleep -Milliseconds 100
$bvp = $box.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
$bvp.SetValue('PlanBoardLeaf')
Start-Sleep -Milliseconds 200

if (-not (Invoke-Named 'Search')) {
  # SoftKey may be labeled differently
  if (-not (Invoke-Named 'search')) { throw 'Search SoftKey missing' }
}
Start-Sleep -Milliseconds 1200
Write-Output 'SEARCH_DONE PlanBoardLeaf'
Write-Output "SEARCH_VALUE=$($bvp.Current.Value)"

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms, System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class SoftFgCamel {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool AllowSetForegroundWindow(int pid);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, int f);
  public struct RECT { public int Left, Top, Right, Bottom; }
}
'@

$sln = 'D:\Experiments\Personal Cursor Folder\Financial\software\open\cascade-ide\CascadeIDE.sln'
$out = 'D:\Experiments\Personal Cursor Folder\Financial\software\open\cascade-ide\tmp-glass-shots\hybridindex-camelcase-boardleaf-20260808.png'

$deadline = (Get-Date).AddSeconds(40)
$p = $null
while ((Get-Date) -lt $deadline) {
  $p = Get-Process CDP.GlassCockpit.Windows -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
  if ($p) { break }
  Start-Sleep -Milliseconds 400
}
if (-not $p) { throw 'Glass window not ready' }

[void][SoftFgCamel]::AllowSetForegroundWindow(-1)
[void][SoftFgCamel]::ShowWindow($p.MainWindowHandle, 9)
[void][SoftFgCamel]::SetForegroundWindow($p.MainWindowHandle)
Start-Sleep -Milliseconds 500

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

Set-Composer("/solution load `"$sln`"")
Start-Sleep -Milliseconds 2800
Write-Output 'SOLUTION_LOAD_SENT'

[System.Windows.Forms.SendKeys]::SendWait('^q')
Start-Sleep -Milliseconds 500
[System.Windows.Forms.SendKeys]::SendWait('MFD: HybridIndex')
Start-Sleep -Milliseconds 400
[System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
Start-Sleep -Milliseconds 1000

Invoke-Named 'Prefer M' | Out-Null
Start-Sleep -Milliseconds 500

# Touch force + SoftKey reindex so ExpandBody lands for PlanBoardLeaf
if (-not (Invoke-Named 'reindex')) {
  if (-not (Invoke-Named 'Reindex')) { throw 'Reindex SoftKey missing' }
}
Write-Output 'REINDEX_INVOKED'
Start-Sleep -Seconds 8

$box = $root.FindFirst(
  [System.Windows.Automation.TreeScope]::Descendants,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'HybridIndexSearchBox')))
if (-not $box) { throw 'HybridIndexSearchBox missing' }

$box.SetFocus()
Start-Sleep -Milliseconds 100
$bvp = $box.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
$bvp.SetValue('BoardLeaf')
Start-Sleep -Milliseconds 200

if (-not (Invoke-Named 'Search')) {
  if (-not (Invoke-Named 'search')) { throw 'Search SoftKey missing' }
}
Start-Sleep -Milliseconds 1500
Write-Output 'SEARCH_DONE BoardLeaf'
Write-Output "SEARCH_VALUE=$($bvp.Current.Value)"
Write-Output "TITLE=$($p.MainWindowTitle)"

# status label probe
$status = $root.FindFirst(
  [System.Windows.Automation.TreeScope]::Descendants,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'HybridIndexStatusLabel')))
if ($status) { Write-Output "STATUS=$($status.Current.Name)" }

$hwnd = $p.MainWindowHandle
[void][SoftFgCamel]::ShowWindow($hwnd, 9)
[void][SoftFgCamel]::SetForegroundWindow($hwnd)
Start-Sleep -Milliseconds 400
$r = New-Object SoftFgCamel+RECT
[void][SoftFgCamel]::GetWindowRect($hwnd, [ref]$r)
$w = [Math]::Max(1, $r.Right - $r.Left)
$h = [Math]::Max(1, $r.Bottom - $r.Top)
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[void][SoftFgCamel]::PrintWindow($hwnd, $hdc, 2)
$g.ReleaseHdc($hdc)
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output "SAVED $out title=$($p.MainWindowTitle) ${w}x${h}"

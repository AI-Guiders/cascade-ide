$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class SoftFg8 {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool AllowSetForegroundWindow(int pid);
  [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
  public const byte VK_CONTROL = 0x11;
  public const byte VK_RETURN = 0x0D;
  public const uint KEYEVENTF_KEYUP = 0x0002;
}
'@

$p = Get-Process CDP.GlassCockpit.Windows -ErrorAction Stop | Select-Object -First 1
[void][SoftFg8]::AllowSetForegroundWindow(-1)
[void][SoftFg8]::ShowWindow($p.MainWindowHandle, 9)
[void][SoftFg8]::SetForegroundWindow($p.MainWindowHandle)
Start-Sleep -Milliseconds 500

$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
$edit = $root.FindFirst(
  [System.Windows.Automation.TreeScope]::Descendants,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'ComposerBox')))
if (-not $edit) { throw 'ComposerBox missing' }

$edit.SetFocus()
Start-Sleep -Milliseconds 200

$cmd = '/search FindDeskSurface'
$vp = $edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
$vp.SetValue($cmd)
Start-Sleep -Milliseconds 300
Write-Output "COMPOSER_AFTER_SET=$($vp.Current.Value)"

# If ValuePattern didn't stick (WPF quirk), fall back to InputSimulator-style unicode paste via clipboard + Ctrl+V with keybd_event
if ($vp.Current.Value -ne $cmd) {
  Write-Output 'FALLBACK=clipboard+keybd'
  Set-Clipboard -Value $cmd
  Start-Sleep -Milliseconds 100
  [SoftFg8]::keybd_event([SoftFg8]::VK_CONTROL, 0, 0, [UIntPtr]::Zero)
  [System.Windows.Forms.SendKeys]::SendWait('v')
  [SoftFg8]::keybd_event([SoftFg8]::VK_CONTROL, 0, [SoftFg8]::KEYEVENTF_KEYUP, [UIntPtr]::Zero)
  Start-Sleep -Milliseconds 300
  Write-Output "COMPOSER_AFTER_PASTE=$($vp.Current.Value)"
}

$send = $root.FindFirst(
  [System.Windows.Automation.TreeScope]::Descendants,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, 'Send')))
if (-not $send) { throw 'Send missing' }
$send.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Start-Sleep -Milliseconds 3200

$names = @()
foreach ($el in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
  $n = $el.Current.Name
  if ($n -and ($n -match 'FindDesk|FindDeskSurface|/search|find ·|workspace')) { $names += $n }
}
Write-Output "PROBE=$($names.Count)"
$names | Select-Object -First 25 | ForEach-Object { Write-Output "N=$_" }

$out = 'D:\Experiments\Personal Cursor Folder\Financial\software\open\cascade-ide\tmp-glass-shots\search-slash-finddesk-20260808.png'
& 'D:\Experiments\Personal Cursor Folder\Financial\software\open\cascade-ide\tools\_cap_copyfromscreen.ps1' `
  -Process CDP.GlassCockpit.Windows -Title 'GlassCockpit' -OutPath $out
Write-Output "SHOT=$out SIZE=$((Get-Item -LiteralPath $out).Length)"

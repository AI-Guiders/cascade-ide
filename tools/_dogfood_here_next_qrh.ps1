# SoftFL verify: Soft:QRH situations + steps + HERE/NEXT (CopyFromScreen).
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class SoftFg {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool AllowSetForegroundWindow(int pid);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
  public struct RECT { public int Left, Top, Right, Bottom; }
  public const uint LEFTDOWN = 0x0002;
  public const uint LEFTUP = 0x0004;
}
'@

$root = Split-Path $PSScriptRoot -Parent
$cap = Join-Path $PSScriptRoot '_cap_copyfromscreen.ps1'
$shotDir = Join-Path $root 'tmp-glass-shots'
New-Item -ItemType Directory -Force -Path $shotDir | Out-Null

function Focus-Glass {
  $p = Get-Process CDP.GlassCockpit.Windows -ErrorAction Stop | Select-Object -First 1
  [void][SoftFg]::AllowSetForegroundWindow(-1)
  [void][SoftFg]::ShowWindow($p.MainWindowHandle, 9)
  [void][SoftFg]::SetForegroundWindow($p.MainWindowHandle)
  Start-Sleep -Milliseconds 400
  return $p
}

function Cap($name) {
  $out = Join-Path $shotDir $name
  & $cap -Process CDP.GlassCockpit.Windows -Title '' -OutPath $out
  if (-not (Test-Path -LiteralPath $out)) { throw "missing $out" }
  $out
}

function Click-Relative($fx, $fy) {
  $p = Focus-Glass
  $r = New-Object SoftFg+RECT
  [void][SoftFg]::GetWindowRect($p.MainWindowHandle, [ref]$r)
  $x = [int]($r.Left + ($r.Right - $r.Left) * $fx)
  $y = [int]($r.Top + ($r.Bottom - $r.Top) * $fy)
  [void][SoftFg]::SetCursorPos($x, $y)
  Start-Sleep -Milliseconds 80
  [SoftFg]::mouse_event([SoftFg]::LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
  [SoftFg]::mouse_event([SoftFg]::LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
  Start-Sleep -Milliseconds 600
}

Focus-Glass | Out-Null
[System.Windows.Forms.SendKeys]::SendWait('^q')
Start-Sleep -Milliseconds 450
[System.Windows.Forms.SendKeys]::SendWait('qrh')
Start-Sleep -Milliseconds 200
[System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
Start-Sleep -Milliseconds 1000
$qrhList = Cap 'softorgan-qrh-situations-20260808.png'

Click-Relative 0.22 0.42
$qrhSteps = Cap 'softorgan-qrh-steps-20260808.png'

Focus-Glass | Out-Null
[System.Windows.Forms.SendKeys]::SendWait('^q')
Start-Sleep -Milliseconds 450
[System.Windows.Forms.SendKeys]::SendWait('here')
Start-Sleep -Milliseconds 200
[System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
Start-Sleep -Milliseconds 1100
$here = Cap 'softorgan-here-next-20260808.png'

Write-Output "QRH_LIST=$qrhList"
Write-Output "QRH_STEPS=$qrhSteps"
Write-Output "HERE=$here"

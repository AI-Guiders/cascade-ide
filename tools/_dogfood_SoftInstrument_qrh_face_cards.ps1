# SoftFL dogfood: Soft: QRH → glance cards Face (not markdown wall).
$ErrorActionPreference = 'Stop'
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class SoftInstrumentWin {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
}
'@
Add-Type -AssemblyName System.Windows.Forms

$p = Get-Process CDP.GlassCockpit.Windows -ErrorAction Stop | Select-Object -First 1
$h = $p.MainWindowHandle
if ($h -eq [IntPtr]::Zero) {
  Start-Sleep -Seconds 2
  $p.Refresh()
  $h = $p.MainWindowHandle
}
if ($h -eq [IntPtr]::Zero) { throw 'no MainWindowHandle' }

[void][SoftInstrumentWin]::ShowWindow($h, 9)
[void][SoftInstrumentWin]::SetForegroundWindow($h)
Start-Sleep -Milliseconds 500
[System.Windows.Forms.SendKeys]::SendWait('^q')
Start-Sleep -Milliseconds 500
[System.Windows.Forms.SendKeys]::SendWait('qrh{ENTER}')
Start-Sleep -Seconds 1

$out = Join-Path $PSScriptRoot '..\tmp-glass-shots\softinstrument-qrh-face-cards-20260808.png'
New-Item -ItemType Directory -Force -Path (Split-Path $out) | Out-Null
& (Join-Path $PSScriptRoot 'Capture-Window.ps1') -Process CDP.GlassCockpit.Windows -Title 'CDP GlassCockpit' -OutPath $out
Write-Output $out
if (-not (Test-Path -LiteralPath $out)) { throw "missing $out" }

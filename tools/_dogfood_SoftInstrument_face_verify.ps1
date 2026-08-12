# SoftFL verify: Soft:QRH → Soft:ECL → CopyFromScreen PNGs (no PrintWindow hang).
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class SoftFg {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool AllowSetForegroundWindow(int pid);
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
  Start-Sleep -Milliseconds 350
  return $p
}

function Run-Soft($query, $outName) {
  Focus-Glass | Out-Null
  [System.Windows.Forms.SendKeys]::SendWait('^q')
  Start-Sleep -Milliseconds 450
  [System.Windows.Forms.SendKeys]::SendWait($query)
  Start-Sleep -Milliseconds 200
  [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
  Start-Sleep -Milliseconds 900
  $out = Join-Path $shotDir $outName
  & $cap -Process CDP.GlassCockpit.Windows -Title 'GlassCockpit' -OutPath $out
  if (-not (Test-Path -LiteralPath $out)) { throw "missing $out" }
  $out
}

$qrh = Run-Soft 'qrh' 'softinstrument-qrh-face-cards-20260808.png'
$ecl = Run-Soft 'ecl' 'softinstrument-ecl-face-cards-20260808.png'
Write-Output "QRH=$qrh"
Write-Output "ECL=$ecl"

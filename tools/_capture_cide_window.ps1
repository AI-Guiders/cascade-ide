param([string]$OutName = 'shot.png')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
if (-not ('W32' -as [type])) {
  Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class W32 {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
  public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
}
'@
}
$p = Get-Process CascadeIDE -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) { Write-Output 'NO_PROCESS'; exit 1 }
Write-Output "pid=$($p.Id) title=$($p.MainWindowTitle) responding=$($p.Responding)"
$hwnd = $p.MainWindowHandle
if ($hwnd -eq [IntPtr]::Zero) { Write-Output 'NO_HWND'; exit 2 }
[void][W32]::ShowWindow($hwnd, 9)
[void][W32]::SetForegroundWindow($hwnd)
Start-Sleep -Milliseconds 600
$r = New-Object W32+RECT
[void][W32]::GetWindowRect($hwnd, [ref]$r)
$w = [Math]::Max(1, $r.Right - $r.Left)
$h = [Math]::Max(1, $r.Bottom - $r.Top)
$shots = Join-Path $env:TEMP 'cide-glass-shots'
New-Item -ItemType Directory -Force -Path $shots | Out-Null
$out = Join-Path $shots $OutName
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output "SAVED $out ${w}x${h}"

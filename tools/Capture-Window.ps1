<#
.SYNOPSIS
  Capture a specific top-level HWND to PNG. Prefer CDP: cdp_webcam op=window (PrintWindow).
  This script is the escape hatch (CopyFromScreen of window rect).
.EXAMPLE
  .\Capture-Window.ps1 -List -Process CDP.GlassCockpit.Windows
  .\Capture-Window.ps1 -Process CDP.GlassCockpit.Windows -Title 'M · MFD' -OutPath $env:TEMP\m.png
#>
param(
  [string]$Process,
  [string]$Title,
  [string]$Hwnd,
  [string]$OutPath,
  [switch]$List
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
if (-not ('WinEnum' -as [type])) {
  Add-Type @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class WinEnum {
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint cmd);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowTextLength(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int nCmdShow);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  public struct RECT { public int L,T,R,B; }
  public const uint GW_OWNER = 4;
  public const uint PW_RENDERFULLCONTENT = 2;
}
'@
}

$script:hits = New-Object System.Collections.Generic.List[object]
[WinEnum]::EnumWindows({
  param($h, $lp)
  if (-not [WinEnum]::IsWindowVisible($h)) { return $true }
  if ([WinEnum]::GetWindow($h, [WinEnum]::GW_OWNER) -ne [IntPtr]::Zero) { return $true }
  $len = [WinEnum]::GetWindowTextLength($h)
  if ($len -le 0) { return $true }
  $sb = New-Object System.Text.StringBuilder ($len + 1)
  [void][WinEnum]::GetWindowText($h, $sb, $sb.Capacity)
  $r = New-Object WinEnum+RECT
  if (-not [WinEnum]::GetWindowRect($h, [ref]$r)) { return $true }
  $w = $r.R - $r.L; $hh = $r.B - $r.T
  if ($w -lt 80 -or $hh -lt 40) { return $true }
  [uint32]$pid = 0
  [void][WinEnum]::GetWindowThreadProcessId($h, [ref]$pid)
  $pname = '?'
  try { $pname = (Get-Process -Id $pid -EA Stop).ProcessName } catch {}
  $script:hits.Add([pscustomobject]@{
    Hwnd = [int64]$h; Pid = $pid; Process = $pname; Width = $w; Height = $hh
    X = $r.L; Y = $r.T; Title = $sb.ToString()
  })
  return $true
}, [IntPtr]::Zero) | Out-Null

$all = @($script:hits)
if ($Process) { $all = $all | Where-Object { $_.Process -like "*$Process*" } }
if ($Title) { $all = $all | Where-Object { $_.Title -like "*$Title*" } }
if ($Hwnd) {
  $n = if ($Hwnd -like '0x*') { [Convert]::ToInt64($Hwnd, 16) } else { [int64]$Hwnd }
  $all = $all | Where-Object { $_.Hwnd -eq $n }
}

if ($List) {
  $all | ForEach-Object { "hwnd=$($_.Hwnd) pid=$($_.Pid) process=$($_.Process) $($_.Width)x$($_.Height) title=$($_.Title)" }
  return
}

if (@($all).Count -eq 0) { throw 'No matching window' }
$best = $all | Sort-Object { $_.Width * $_.Height } -Descending | Select-Object -First 1
if (-not $OutPath) {
  $dir = Join-Path $env:TEMP 'cide-glass-shots'
  New-Item -ItemType Directory -Force -Path $dir | Out-Null
  $OutPath = Join-Path $dir ("window-{0:yyyyMMdd-HHmmss}.png" -f (Get-Date))
}

$hwndPtr = [IntPtr]$best.Hwnd
[void][WinEnum]::ShowWindow($hwndPtr, 9)
[void][WinEnum]::SetForegroundWindow($hwndPtr)
Start-Sleep -Milliseconds 250

$bmp = New-Object System.Drawing.Bitmap ([Math]::Max(1, $best.Width)), ([Math]::Max(1, $best.Height)), ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
$ok = [WinEnum]::PrintWindow($hwndPtr, $hdc, [WinEnum]::PW_RENDERFULLCONTENT)
$g.ReleaseHdc($hdc)
if (-not $ok) {
  # Fallback: screen rect (may include overlaps)
  $g.CopyFromScreen($best.X, $best.Y, 0, 0, $bmp.Size)
}
$bmp.Save($OutPath, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
"SAVED $OutPath $($best.Width)x$($best.Height) method=$(if ($ok) { 'PrintWindow' } else { 'CopyFromScreen' })"
"MATCH hwnd=$($best.Hwnd) process=$($best.Process) title=$($best.Title)"

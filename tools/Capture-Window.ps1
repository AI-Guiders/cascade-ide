<#
.SYNOPSIS
  Capture a specific top-level HWND to PNG. Prefer CDP: cdp_webcam op=window (PrintWindow).
.EXAMPLE
  .\Capture-Window.ps1 -List -Process CDP.GlassCockpit.Windows
  .\Capture-Window.ps1 -Process CDP.GlassCockpit.Windows -Title MFD -OutPath $env:TEMP\m.png
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

if (-not ('WinCapEnum4' -as [type])) {
  Add-Type @'
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class WinCapEnum4 {
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint cmd);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowTextLength(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint lpdwProcessId);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int nCmdShow);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  public struct RECT { public int L,T,R,B; }
  public const uint GW_OWNER = 4;
  public const uint PW_RENDERFULLCONTENT = 2;
  public static List<string> ListVisible() {
    var list = new List<string>();
    EnumWindows((h, lParam) => {
      if (!IsWindowVisible(h) || GetWindow(h, GW_OWNER) != IntPtr.Zero) return true;
      int len = GetWindowTextLength(h); if (len <= 0) return true;
      var sb = new StringBuilder(len + 1); GetWindowText(h, sb, sb.Capacity);
      RECT r; if (!GetWindowRect(h, out r)) return true;
      int w = r.R - r.L, hh = r.B - r.T; if (w < 80 || hh < 40) return true;
      uint procId = 0; GetWindowThreadProcessId(h, out procId);
      string pname = "?";
      try { pname = System.Diagnostics.Process.GetProcessById((int)procId).ProcessName; } catch {}
      list.Add(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", (long)h, procId, pname, w, hh, r.L, r.T, sb));
      return true;
    }, IntPtr.Zero);
    return list;
  }
}
'@
}

$rows = @([WinCapEnum4]::ListVisible() | ForEach-Object {
  $p = $_ -split "`t", 8
  [pscustomobject]@{
    Hwnd = [int64]$p[0]; ProcessId = [uint32]$p[1]; Process = $p[2]
    Width = [int]$p[3]; Height = [int]$p[4]; X = [int]$p[5]; Y = [int]$p[6]; Title = $p[7]
  }
})
if ($Process) { $rows = @($rows | Where-Object { $_.Process -like "*$Process*" }) }
if ($Title) { $rows = @($rows | Where-Object { $_.Title -like "*$Title*" }) }
if ($Hwnd) {
  $n = if ($Hwnd -like '0x*') { [Convert]::ToInt64($Hwnd, 16) } else { [int64]$Hwnd }
  $rows = @($rows | Where-Object { $_.Hwnd -eq $n })
}

if ($List) {
  $rows | ForEach-Object { "hwnd=$($_.Hwnd) pid=$($_.ProcessId) process=$($_.Process) $($_.Width)x$($_.Height) title=$($_.Title)" }
  return
}
if ($rows.Count -eq 0) { throw 'No matching window' }

$best = $rows | Sort-Object { $_.Width * $_.Height } -Descending | Select-Object -First 1
if (-not $OutPath) {
  $dir = Join-Path $env:TEMP 'cide-glass-shots'
  New-Item -ItemType Directory -Force -Path $dir | Out-Null
  $OutPath = Join-Path $dir ("window-{0:yyyyMMdd-HHmmss}.png" -f (Get-Date))
}

$hwndPtr = [IntPtr]$best.Hwnd
[void][WinCapEnum4]::ShowWindow($hwndPtr, 9)
[void][WinCapEnum4]::SetForegroundWindow($hwndPtr)
Start-Sleep -Milliseconds 250

$r = New-Object WinCapEnum4+RECT
[void][WinCapEnum4]::GetWindowRect($hwndPtr, [ref]$r)
$w = [Math]::Max(1, $r.R - $r.L)
$h = [Math]::Max(1, $r.B - $r.T)

$bmp = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
$ok = [WinCapEnum4]::PrintWindow($hwndPtr, $hdc, [WinCapEnum4]::PW_RENDERFULLCONTENT)
$g.ReleaseHdc($hdc)
if (-not $ok) { $g.CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size) }
$bmp.Save($OutPath, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
"SAVED $OutPath ${w}x${h} method=$(if ($ok) {'PrintWindow'} else {'CopyFromScreen'})"
"MATCH hwnd=$($best.Hwnd) process=$($best.Process) title=$($best.Title)"

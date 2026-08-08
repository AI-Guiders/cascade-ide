# Escape when PrintWindow hangs on WPF Glass — CopyFromScreen only.
param(
  [Parameter(Mandatory)][string]$Process,
  [string]$Title = '',
  [Parameter(Mandatory)][string]$OutPath
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class CapCopy {
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint cmd);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowTextLength(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  public struct RECT { public int L,T,R,B; }
  public const uint GW_OWNER = 4;
  public static List<(IntPtr h, uint pid, string title, int w, int hh, int x, int y)> List() {
    var list = new List<(IntPtr,uint,string,int,int,int,int)>();
    EnumWindows((h, _) => {
      if (!IsWindowVisible(h) || GetWindow(h, GW_OWNER) != IntPtr.Zero) return true;
      int len = GetWindowTextLength(h); if (len <= 0) return true;
      var sb = new StringBuilder(len + 1); GetWindowText(h, sb, sb.Capacity);
      RECT r; if (!GetWindowRect(h, out r)) return true;
      int w = r.R - r.L, hh = r.B - r.T; if (w < 80 || hh < 40) return true;
      uint pid = 0; GetWindowThreadProcessId(h, out pid);
      list.Add((h, pid, sb.ToString(), w, hh, r.L, r.T));
      return true;
    }, IntPtr.Zero);
    return list;
  }
}
'@

$want = @(Get-Process -Name ($Process -replace '\.exe$','') -ErrorAction Stop | ForEach-Object { [uint32]$_.Id })
$hit = [CapCopy]::List() | Where-Object {
  $want -contains $_.Item2 -and ($Title -eq '' -or $_.Item3 -like "*$Title*")
} | Sort-Object { $_.Item4 * $_.Item5 } -Descending | Select-Object -First 1
if (-not $hit) { throw "no window for $Process title=$Title" }

[void][CapCopy]::ShowWindow($hit.Item1, 9)
[void][CapCopy]::SetForegroundWindow($hit.Item1)
Start-Sleep -Milliseconds 200

$bmp = New-Object System.Drawing.Bitmap $hit.Item4, $hit.Item5, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($hit.Item6, $hit.Item7, 0, 0, $bmp.Size)
New-Item -ItemType Directory -Force -Path (Split-Path $OutPath) | Out-Null
$bmp.Save($OutPath, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
"SAVED $OutPath $($hit.Item4)x$($hit.Item5) title=$($hit.Item3)"

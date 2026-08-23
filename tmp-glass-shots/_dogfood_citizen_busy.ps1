$ErrorActionPreference = 'Stop'
$p = Join-Path $env:LOCALAPPDATA 'cdp-mcp/intercom-presence-LATEST.json'
$now = [DateTimeOffset]::UtcNow.ToString('o')
$doc = [ordered]@{
  schema = 'cide_intercom_presence_latch/v0'
  pf = [ordered]@{
    state = 'busy'
    stamped_utc = $now
    ttl_seconds = 120
    who = 'Citizen'
    kind = 'citizen'
  }
  pm = [ordered]@{
    state = 'idle'
    stamped_utc = $now
  }
}
$json = $doc | ConvertTo-Json -Depth 6
$tmp = $p + '.' + ([guid]::NewGuid().ToString('N').Substring(0, 8)) + '.tmp'
[IO.File]::WriteAllText($tmp, $json)
[IO.File]::Move($tmp, $p, $true)
Write-Output "WROTE $now"
Start-Sleep -Seconds 16

Add-Type -AssemblyName System.Drawing
if (-not ('W32g4' -as [type])) {
  Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class W32g4 {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, int f);
  public struct RECT { public int Left, Top, Right, Bottom; }
}
'@
}
$proc = Get-Process CDP.GlassCockpit.Windows | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
if (-not $proc) { throw 'NO_GLASS' }
$hwnd = $proc.MainWindowHandle
[void][W32g4]::ShowWindow($hwnd, 9)
[void][W32g4]::SetForegroundWindow($hwnd)
Start-Sleep -Milliseconds 500
$r = New-Object W32g4+RECT
[void][W32g4]::GetWindowRect($hwnd, [ref]$r)
$w = [Math]::Max(1, $r.Right - $r.Left)
$h = [Math]::Max(1, $r.Bottom - $r.Top)
$out = Join-Path $PSScriptRoot 'face-citizen-busy-cue-20260806.png'
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[void][W32g4]::PrintWindow($hwnd, $hdc, 2)
$g.ReleaseHdc($hdc)
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output "SAVED $out title=$($proc.MainWindowTitle) ${w}x${h}"
Get-Content -LiteralPath $p

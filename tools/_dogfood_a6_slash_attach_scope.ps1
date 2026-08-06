# Live dogfood: Radio → /intercom attach scope → honest DIG REJECT bubble → PrintWindow
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

if (-not ('WinFindGlass' -as [type])) {
  Add-Type @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class WinFindGlass {
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowTextLength(IntPtr h);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  public static IntPtr Find(int pid, string titleSub) {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h, l) => {
      if (!IsWindowVisible(h)) return true;
      uint p = 0; GetWindowThreadProcessId(h, out p);
      if ((int)p != pid) return true;
      int len = GetWindowTextLength(h); if (len <= 0) return true;
      var sb = new StringBuilder(len + 1); GetWindowText(h, sb, sb.Capacity);
      if (sb.ToString().IndexOf(titleSub, StringComparison.OrdinalIgnoreCase) >= 0) { found = h; return false; }
      return true;
    }, IntPtr.Zero);
    return found;
  }
}
'@
}

$p = Get-Process CDP.GlassCockpit.Windows -ErrorAction Stop | Select-Object -First 1
$hwnd = [WinFindGlass]::Find($p.Id, 'CDP GlassCockpit')
if ($hwnd -eq [IntPtr]::Zero) { throw 'no GlassCockpit window' }
$root = [System.Windows.Automation.AutomationElement]::FromHandle($hwnd)
Write-Output "hwnd=$([int64]$hwnd) title=$($root.Current.Name)"

function Click-Name([string]$Name) {
    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $Name)
    $el = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
    if (-not $el) { throw "no button: $Name" }
    try {
        $inv = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $inv.Invoke()
    }
    catch {
        $sel = $el.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
        $sel.Select()
    }
    Start-Sleep -Milliseconds 400
}

# Prefer Radio; fall back to #crew / Intercom channel buttons
$clicked = $false
foreach ($n in @('Radio', '#crew', 'Crew', 'Intercom')) {
    try { Click-Name $n; $clicked = $true; Write-Output "clicked=$n"; break } catch { }
}
if (-not $clicked) {
    $btnCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Button)
    $names = @()
    foreach ($b in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCond)) {
        $nm = $b.Current.Name
        if ($nm) { $names += $nm }
    }
    Write-Output "buttons=$($names -join ' | ')"
    throw 'no Radio/#crew'
}
Start-Sleep -Seconds 1

$editCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Edit)
$edits = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)
if ($edits.Count -lt 1) { throw 'no edit controls' }

$composer = $null
foreach ($e in $edits) {
    if ($e.Current.IsEnabled) { $composer = $e; break }
}
if (-not $composer) { $composer = $edits[$edits.Count - 1] }

$val = $composer.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
$val.SetValue('/intercom attach scope')
Start-Sleep -Milliseconds 400
Click-Name 'Send'
Start-Sleep -Seconds 1

$out = Join-Path $PSScriptRoot '..\tmp-glass-shots\a6-slash-attach-scope-refuse-20260806.png'
$out = [IO.Path]::GetFullPath($out)
New-Item -ItemType Directory -Force -Path (Split-Path $out) | Out-Null
& (Join-Path $PSScriptRoot 'Capture-Window.ps1') -Process CDP.GlassCockpit.Windows -Title 'CDP GlassCockpit' -OutPath $out

Write-Output "PNG=$out exists=$(Test-Path $out) pid=$($p.Id) composerAid=$($composer.Current.AutomationId)"

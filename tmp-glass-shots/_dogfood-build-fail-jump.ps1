$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class SoftFgBuild2 {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool AllowSetForegroundWindow(int pid);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
  public const uint LEFTDOWN = 0x0002;
  public const uint LEFTUP = 0x0004;
}
'@

$failProj = 'D:\Experiments\Personal Cursor Folder\Financial\software\open\cascade-ide\tmp-glass-shots\_softfl-build-fail\SoftFlBuildFail.csproj'
$p = Get-Process CDP.GlassCockpit.Windows -ErrorAction Stop | Select-Object -First 1
[void][SoftFgBuild2]::AllowSetForegroundWindow(-1)
[void][SoftFgBuild2]::ShowWindow($p.MainWindowHandle, 9)
[void][SoftFgBuild2]::SetForegroundWindow($p.MainWindowHandle)
Start-Sleep -Milliseconds 500
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)

function Find-ByAid([string]$aid) {
  return $root.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
      [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $aid)))
}
function Find-ByName([string]$name) {
  return $root.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
      [System.Windows.Automation.AutomationElement]::NameProperty, $name)))
}
function Invoke-El($el) {
  if (-not $el) { return $false }
  try { $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); return $true }
  catch { return $false }
}

# 1) Load fail project
$edit = Find-ByAid 'ComposerBox'
if (-not $edit) { throw 'ComposerBox missing' }
$radio = Find-ByName 'Radio'
if ($radio) { [void](Invoke-El $radio); Start-Sleep -Milliseconds 200 }
$edit.SetFocus(); Start-Sleep -Milliseconds 100
$vp = $edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
$vp.SetValue("/solution load $failProj")
[void](Invoke-El (Find-ByName 'Send'))
Start-Sleep -Milliseconds 2500
Write-Output 'LOADED_FAIL_PROJ'

# 2) Soft chord Ctrl+K → mb → Enter (MFD Build)
[void][SoftFgBuild2]::SetForegroundWindow($p.MainWindowHandle)
Start-Sleep -Milliseconds 200
# Click main chrome so PreviewKeyDown hits MainWindow (not composer)
$title = Find-ByAid 'MfdZoneTitle'
if ($title) {
  $br = $title.Current.BoundingRectangle
  [void][SoftFgBuild2]::SetCursorPos([int]($br.X + 20), [int]($br.Y + 8))
  [SoftFgBuild2]::mouse_event([SoftFgBuild2]::LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
  [SoftFgBuild2]::mouse_event([SoftFgBuild2]::LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
  Start-Sleep -Milliseconds 150
}
# Layout-safe: Ctrl+K then VK_M VK_B (Soft chord alias mb → mfd_build; exact auto-commits)
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class SoftKeysBuild {
  [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
  public const byte VK_CONTROL = 0x11;
  public const byte VK_K = 0x4B;
  public const byte VK_M = 0x4D;
  public const byte VK_B = 0x42;
  public const uint KEYUP = 0x0002;
  public static void Chord(byte a, byte b) {
    keybd_event(a, 0, 0, UIntPtr.Zero);
    keybd_event(b, 0, 0, UIntPtr.Zero);
    keybd_event(b, 0, KEYUP, UIntPtr.Zero);
    keybd_event(a, 0, KEYUP, UIntPtr.Zero);
  }
  public static void Tap(byte vk) {
    keybd_event(vk, 0, 0, UIntPtr.Zero);
    keybd_event(vk, 0, KEYUP, UIntPtr.Zero);
  }
}
'@
[SoftKeysBuild]::Chord([SoftKeysBuild]::VK_CONTROL, [SoftKeysBuild]::VK_K)
Start-Sleep -Milliseconds 450
[SoftKeysBuild]::Tap([SoftKeysBuild]::VK_M)
Start-Sleep -Milliseconds 120
[SoftKeysBuild]::Tap([SoftKeysBuild]::VK_B)
Start-Sleep -Milliseconds 900
Write-Output "MFD_TITLE=$((Find-ByAid 'MfdZoneTitle').Current.Name)"

# 3) Run build
$run = Find-ByAid 'BuildRunBtn'
if (-not $run) {
  # probe
  foreach ($el in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    $aid = $el.Current.AutomationId
    $n = $el.Current.Name
    if ($aid -match 'Build' -or $n -eq 'build') { Write-Output "HIT aid=$aid name=$n" }
  }
  throw 'BuildRunBtn missing'
}
Write-Output 'BUILD_RUN=invoke'
[void](Invoke-El $run)

# 4) Wait problems
$list = $null
$deadline = (Get-Date).AddSeconds(120)
$itemCount = 0
while ((Get-Date) -lt $deadline) {
  Start-Sleep -Milliseconds 2000
  $list = Find-ByAid 'BuildProblemsList'
  if ($list) {
    $items = $list.FindAll(
      [System.Windows.Automation.TreeScope]::Children,
      (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ListItem)))
    $itemCount = $items.Count
  }
  $st = Find-ByAid 'BuildStatusLabel'
  $stName = if ($st) { $st.Current.Name } else { '?' }
  Write-Output "PROBLEMS=$itemCount STATUS=$stName"
  if ($itemCount -gt 0) { break }
}
if ($itemCount -lt 1) { throw 'No BuildProblemsList rows after build' }

# 5) DoubleClick first problem
$first = $list.FindFirst(
  [System.Windows.Automation.TreeScope]::Children,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::ListItem)))
Write-Output "FIRST=$($first.Current.Name)"
try { $first.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select() } catch {}
Start-Sleep -Milliseconds 150
$r = $first.Current.BoundingRectangle
$cx = [int]($r.X + $r.Width / 2)
$cy = [int]($r.Y + $r.Height / 2)
[void][SoftFgBuild2]::SetCursorPos($cx, $cy)
[SoftFgBuild2]::mouse_event([SoftFgBuild2]::LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
[SoftFgBuild2]::mouse_event([SoftFgBuild2]::LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 50
[SoftFgBuild2]::mouse_event([SoftFgBuild2]::LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
[SoftFgBuild2]::mouse_event([SoftFgBuild2]::LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 1800

Write-Output "AFTER_TITLE=$((Find-ByAid 'MfdZoneTitle').Current.Name)"
$names = @()
foreach ($el in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
  $n = $el.Current.Name
  if ($n -and ($n -match 'M ·|Editor|Broken|build ·|CS1002|glass · build')) { $names += $n }
}
Write-Output "PROBE=$($names.Count)"
$names | Select-Object -First 25 | ForEach-Object { Write-Output "N=$_" }

$out = 'D:\Experiments\Personal Cursor Folder\Financial\software\open\cascade-ide\tmp-glass-shots\build-fail-dblclick-jump-20260808.png'
& 'D:\Experiments\Personal Cursor Folder\Financial\software\open\cascade-ide\tools\_cap_copyfromscreen.ps1' `
  -Process CDP.GlassCockpit.Windows -Title 'GlassCockpit' -OutPath $out
Write-Output "SHOT=$out SIZE=$((Get-Item -LiteralPath $out).Length)"

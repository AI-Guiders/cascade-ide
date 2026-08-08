$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class SoftFgTests {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool AllowSetForegroundWindow(int pid);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
  public const uint LEFTDOWN = 0x0002;
  public const uint LEFTUP = 0x0004;
}
"@

$failProj = 'D:\Experiments\Personal Cursor Folder\Financial\software\open\cascade-ide\tmp-glass-shots\_softfl-test-fail\SoftFlTestFail.csproj'
$p = Get-Process CDP.GlassCockpit.Windows -ErrorAction Stop | Select-Object -First 1
[void][SoftFgTests]::AllowSetForegroundWindow(-1)
[void][SoftFgTests]::ShowWindow($p.MainWindowHandle, 9)
[void][SoftFgTests]::SetForegroundWindow($p.MainWindowHandle)
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

# 1) Load fail test project
$edit = Find-ByAid 'ComposerBox'
if (-not $edit) { throw 'ComposerBox missing' }
$radio = Find-ByName 'Radio'
if ($radio) { [void](Invoke-El $radio); Start-Sleep -Milliseconds 200 }
$edit.SetFocus(); Start-Sleep -Milliseconds 100
$vp = $edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
$vp.SetValue("/solution load $failProj")
[void](Invoke-El (Find-ByName 'Send'))
Start-Sleep -Milliseconds 2500
Write-Output 'LOADED_FAIL_TEST_PROJ'

# 2) Soft chord Ctrl+K → ms → MFD Tests
[void][SoftFgTests]::SetForegroundWindow($p.MainWindowHandle)
Start-Sleep -Milliseconds 200
$title = Find-ByAid 'MfdZoneTitle'
if ($title) {
  $br = $title.Current.BoundingRectangle
  [void][SoftFgTests]::SetCursorPos([int]($br.X + 20), [int]($br.Y + 8))
  [SoftFgTests]::mouse_event([SoftFgTests]::LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
  [SoftFgTests]::mouse_event([SoftFgTests]::LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
  Start-Sleep -Milliseconds 150
}
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class SoftKeysTests {
  [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
  public const byte VK_CONTROL = 0x11;
  public const byte VK_K = 0x4B;
  public const byte VK_M = 0x4D;
  public const byte VK_S = 0x53;
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
"@
[SoftKeysTests]::Chord([SoftKeysTests]::VK_CONTROL, [SoftKeysTests]::VK_K)
Start-Sleep -Milliseconds 450
[SoftKeysTests]::Tap([SoftKeysTests]::VK_M)
Start-Sleep -Milliseconds 120
[SoftKeysTests]::Tap([SoftKeysTests]::VK_S)
Start-Sleep -Milliseconds 900
Write-Output "MFD_TITLE=$((Find-ByAid 'MfdZoneTitle').Current.Name)"

# 3) Run tests
$run = Find-ByAid 'TestsRunBtn'
if (-not $run) {
  foreach ($el in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    $aid = $el.Current.AutomationId
    $n = $el.Current.Name
    if ($aid -match 'Test' -or $n -eq 'test') { Write-Output "HIT aid=$aid name=$n" }
  }
  throw 'TestsRunBtn missing'
}
Write-Output 'TESTS_RUN=invoke'
[void](Invoke-El $run)

# 4) Wait fail rows
$list = $null
$deadline = (Get-Date).AddSeconds(180)
$itemCount = 0
while ((Get-Date) -lt $deadline) {
  Start-Sleep -Milliseconds 2000
  $list = Find-ByAid 'TestsFailList'
  if ($list) {
    $items = $list.FindAll(
      [System.Windows.Automation.TreeScope]::Children,
      (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ListItem)))
    $itemCount = $items.Count
  }
  $st = Find-ByAid 'TestsStatusLabel'
  $stName = if ($st) { $st.Current.Name } else { '?' }
  Write-Output "FAILS=$itemCount STATUS=$stName"
  if ($itemCount -gt 0 -and $stName -match 'done|failed') { break }
  if ($itemCount -gt 0 -and $stName -notmatch 'testing') { break }
}
if ($itemCount -lt 1) { throw 'No TestsFailList rows after test' }

# 5) DoubleClick first fail
$first = $list.FindFirst(
  [System.Windows.Automation.TreeScope]::Children,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::ListItem)))
Write-Output "FIRST=$($first.Current.Name)"
try { $first.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select() } catch {}
Start-Sleep -Milliseconds 150
$r = $first.Current.BoundingRectangle
$cx = [int]($r.X + [Math]::Min(80, [Math]::Max(12, $r.Width / 8)))
$cy = [int]($r.Y + $r.Height / 2)
[void][SoftFgTests]::SetCursorPos($cx, $cy)
[SoftFgTests]::mouse_event([SoftFgTests]::LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
[SoftFgTests]::mouse_event([SoftFgTests]::LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 50
[SoftFgTests]::mouse_event([SoftFgTests]::LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
[SoftFgTests]::mouse_event([SoftFgTests]::LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 1800

Write-Output "AFTER_TITLE=$((Find-ByAid 'MfdZoneTitle').Current.Name)"
$names = @()
foreach ($el in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
  $n = $el.Current.Name
  if ($n -and ($n -match 'M ·|Editor|Broken|tests ·|FailsOnPurpose|glass · tests')) { $names += $n }
}
Write-Output "PROBE=$($names.Count)"
$names | Select-Object -First 25 | ForEach-Object { Write-Output "N=$_" }

$out = 'D:\Experiments\Personal Cursor Folder\Financial\software\open\cascade-ide\tmp-glass-shots\tests-fail-dblclick-jump-20260808.png'
& 'D:\Experiments\Personal Cursor Folder\Financial\software\open\cascade-ide\tools\_cap_copyfromscreen.ps1' `
  -Process CDP.GlassCockpit.Windows -Title 'GlassCockpit' -OutPath $out
Write-Output "SHOT=$out SIZE=$((Get-Item -LiteralPath $out).Length)"

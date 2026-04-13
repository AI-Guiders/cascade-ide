#requires -Version 7
<#
.SYNOPSIS
  Пересчитывает строки в partial-файлах MainWindowViewModel / IdeMcpCommandExecutor и обновляет таблицы в docs/architecture-migration.md.

.DESCRIPTION
  Колонка «Содержание»: по умолчанию берётся из XML-док-комментария `<summary>` непосредственно над
  `partial class MainWindowViewModel` / `partial class IdeMcpCommandExecutor` в соответствующем .cs.
  Если там нет summary — подставляется строка из
  tools/architecture-migration-slice/main-window-slice-descriptions.json (необязательный fallback; может быть ``{}``).
  Шаблон абзаца сводки — tools/architecture-migration-slice/main-window-slice-summary.template.md
  (плейсхолдеры {0}…{3}). Каталог не tools/data: там срабатывает игнор Data/ в .gitignore.

.PARAMETER RepoRoot
  Корень репозитория cascade-ide (родитель каталога tools).

.PARAMETER DryRun
  Печатает фрагменты в консоль, файл не перезаписывает.

.EXAMPLE
  pwsh ./tools/Update-ArchitectureMigrationMainWindowSlice.ps1
#>
[CmdletBinding()]
param(
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [switch] $DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-LineCount([string] $path) {
    if (-not (Test-Path -LiteralPath $path)) { return 0 }
    return (Get-Content -LiteralPath $path | Measure-Object -Line).Lines
}

function Read-OptionalDescriptionsJson([string] $jsonPath) {
    if (-not (Test-Path -LiteralPath $jsonPath)) {
        return @{}
    }
    $raw = Get-Content -LiteralPath $jsonPath -Raw -Encoding UTF8
    if ([string]::IsNullOrWhiteSpace($raw)) {
        return @{}
    }
    $o = $raw | ConvertFrom-Json
    $ht = @{}
    foreach ($p in $o.PSObject.Properties) {
        $ht[$p.Name] = [string]$p.Value
    }
    return $ht
}

function Get-CrefDisplayFragment([string] $cref) {
    $c = $cref.Trim()
    if ($c.Length -ge 2 -and [char]::IsLetter($c[0]) -and $c[1] -eq ':') {
        $c = $c.Substring(2)
    }
    $paren = $c.IndexOf('(')
    if ($paren -ge 0) {
        $c = $c.Substring(0, $paren)
    }
    $i = $c.LastIndexOf('.')
    if ($i -ge 0) {
        return $c.Substring($i + 1)
    }
    return $c
}

function Normalize-XmlDocSummaryForMarkdown([string] $text) {
    if ([string]::IsNullOrWhiteSpace($text)) {
        return ''
    }
    $t = $text
    $t = [regex]::Replace($t, '<c>([^<]*)</c>', { param($m) [char]0x0060 + $m.Groups[1].Value + [char]0x0060 })
    $t = [regex]::Replace($t, '<see\s+cref="([^"]+)"\s*/>', { param($m) [char]0x0060 + (Get-CrefDisplayFragment $m.Groups[1].Value) + [char]0x0060 })
    $t = [regex]::Replace($t, '<see\s+langword="([^"]+)"\s*/>', { param($m) [char]0x0060 + $m.Groups[1].Value + [char]0x0060 })
    $t = [regex]::Replace($t, '<[^>]+>', '')
    $t = $t -replace '&lt;', '<' -replace '&gt;', '>' -replace '&amp;', '&' -replace '&quot;', '"'
    $t = [regex]::Replace($t, '\s+', ' ').Trim()
    $t = $t -replace '\|', '¦'
    return $t
}

function Get-FileLevelXmlSummary([string] $path, [string] $classBaseName) {
    $lines = @(Get-Content -LiteralPath $path -Encoding UTF8)
    $escaped = [regex]::Escape($classBaseName)
    $pattern = "^\s*(?:public|internal)\s+(?:sealed\s+)?partial\s+class\s+$escaped\b"
    $classLineIndex = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match $pattern) {
            $classLineIndex = $i
            break
        }
    }
    if ($classLineIndex -lt 0) {
        return ''
    }
    $i = $classLineIndex - 1
    while ($i -ge 0 -and [string]::IsNullOrWhiteSpace($lines[$i])) {
        $i--
    }
    $docLines = [System.Collections.Generic.List[string]]::new()
    while ($i -ge 0 -and $lines[$i] -match '^\s*///') {
        $docLines.Insert(0, $lines[$i])
        $i--
    }
    if ($docLines.Count -eq 0) {
        return ''
    }
    $stripped = foreach ($dl in $docLines) {
        if ($dl -match '^\s*///\s?(.*)$') {
            $Matches[1]
        }
        else {
            ''
        }
    }
    $block = $stripped -join "`n"
    if ($block -notmatch '<summary>([\s\S]*?)</summary>') {
        return ''
    }
    $inner = $Matches[1].Trim()
    return (Normalize-XmlDocSummaryForMarkdown $inner)
}

function Resolve-Description([string] $path, [string] $tableKey, [string] $classBaseName, [hashtable] $fallback) {
    $fromXml = Get-FileLevelXmlSummary $path $classBaseName
    if (-not [string]::IsNullOrWhiteSpace($fromXml)) {
        return $fromXml
    }
    if ($fallback.ContainsKey($tableKey) -and -not [string]::IsNullOrWhiteSpace($fallback[$tableKey])) {
        return [string]$fallback[$tableKey]
    }
    throw "Нет XML <summary> над partial class в ``$tableKey`` и нет строки в main-window-slice-descriptions.json для этого ключа."
}

$contentDir = Join-Path $PSScriptRoot 'architecture-migration-slice'
$descriptionsPath = Join-Path $contentDir 'main-window-slice-descriptions.json'
$summaryTemplatePath = Join-Path $contentDir 'main-window-slice-summary.template.md'

$Fallback = Read-OptionalDescriptionsJson $descriptionsPath

if (-not (Test-Path -LiteralPath $summaryTemplatePath)) {
    throw "Нет шаблона сводки: $summaryTemplatePath"
}
$summaryTemplate = (Get-Content -LiteralPath $summaryTemplatePath -Raw -Encoding UTF8).Trim()

$viewModels = Join-Path $RepoRoot 'ViewModels'
$mwFiles = Get-ChildItem -LiteralPath $viewModels -Filter 'MainWindowViewModel*.cs' -File | Sort-Object Name
$execFiles = [System.Collections.Generic.List[object]]::new()
foreach ($x in Get-ChildItem -LiteralPath $viewModels -Filter 'IdeMcpCommandExecutor*.cs' -File | Sort-Object Name) {
    $execFiles.Add($x)
}
$genPath = Join-Path $viewModels 'Generated/IdeMcpCommandExecutor.Generated.g.cs'
if (Test-Path -LiteralPath $genPath) {
    $execFiles.Add((Get-Item -LiteralPath $genPath))
}

function Build-Table([System.Collections.Generic.List[object]] $rows) {
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine('| Файл | Строк (≈) | Содержание |')
    [void]$sb.AppendLine('|------|------------|------------|')
    foreach ($r in $rows) {
        [void]$sb.AppendLine("| ``$($r.Name)`` | $($r.Lines) | $($r.Desc) |")
    }
    return $sb.ToString().TrimEnd()
}

$mwRows = [System.Collections.Generic.List[object]]::new()
$sumMw = 0
foreach ($f in $mwFiles) {
    $name = $f.Name
    $lines = Get-LineCount $f.FullName
    $sumMw += $lines
    $desc = Resolve-Description $f.FullName $name 'MainWindowViewModel' $Fallback
    $mwRows.Add([pscustomobject]@{ Name = $name; Lines = $lines; Desc = $desc })
}

$execRows = [System.Collections.Generic.List[object]]::new()
$sumExec = 0
foreach ($f in $execFiles) {
    if ($null -eq $f) { continue }
    $rel = if ($f.DirectoryName -match 'Generated') { 'Generated/' + $f.Name } else { $f.Name }
    $lines = Get-LineCount $f.FullName
    $sumExec += $lines
    $desc = Resolve-Description $f.FullName $rel 'IdeMcpCommandExecutor' $Fallback
    $execRows.Add([pscustomobject]@{ Name = $rel; Lines = $lines; Desc = $desc })
}

$total = $sumMw + $sumExec
$roundTotal = [math]::Round($total / 1000, 1)
$roundMw = [math]::Round($sumMw / 1000, 1)
$roundExec = [math]::Round($sumExec / 1000, 1)
$ym = Get-Date -Format 'yyyy-MM'

$summaryParagraph = [string]::Format(
    [System.Globalization.CultureInfo]::InvariantCulture,
    $summaryTemplate,
    $roundTotal,
    $roundMw,
    $roundExec,
    $ym)

$mwTable = Build-Table $mwRows
$execTable = Build-Table $execRows

if ($DryRun) {
    Write-Host '--- SUMMARY ---'
    Write-Host $summaryParagraph
    Write-Host '--- MW TABLE ---'
    Write-Host $mwTable
    Write-Host '--- EXEC TABLE ---'
    Write-Host $execTable
    Write-Host "Totals: MW=$sumMw Exec=$sumExec Total=$total"
    return
}

$docPath = Join-Path $RepoRoot 'docs/architecture-migration.md'
$text = Get-Content -LiteralPath $docPath -Raw -Encoding UTF8

$beginSum = '<!-- AUTO:MAIN-WINDOW-SLICE:SUMMARY:BEGIN -->'
$endSum = '<!-- AUTO:MAIN-WINDOW-SLICE:SUMMARY:END -->'
$beginMw = '<!-- AUTO:MAIN-WINDOW-SLICE:MWVM-TABLE:BEGIN -->'
$endMw = '<!-- AUTO:MAIN-WINDOW-SLICE:MWVM-TABLE:END -->'
$beginEx = '<!-- AUTO:MAIN-WINDOW-SLICE:EXEC-TABLE:BEGIN -->'
$endEx = '<!-- AUTO:MAIN-WINDOW-SLICE:EXEC-TABLE:END -->'

function Replace-Between([string] $src, [string] $a, [string] $b, [string] $inner) {
    $i1 = $src.IndexOf($a, [StringComparison]::Ordinal)
    $i2 = $src.IndexOf($b, [StringComparison]::Ordinal)
    if ($i1 -lt 0) { throw "Маркер начала не найден: $a" }
    if ($i2 -lt 0) { throw "Маркер конца не найден: $b" }
    if ($i2 -le $i1) { throw "Маркер конца раньше начала: $a ... $b" }
    $endA = $i1 + $a.Length
    return $src.Substring(0, $endA) + "`n`n" + $inner.TrimEnd() + "`n`n" + $src.Substring($i2)
}

$text = Replace-Between $text $beginSum $endSum $summaryParagraph
$text = Replace-Between $text $beginMw $endMw $mwTable
$text = Replace-Between $text $beginEx $endEx $execTable

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($docPath, $text.TrimEnd() + "`n", $utf8NoBom)
Write-Host "OK: $docPath (MW lines=$sumMw, Exec lines=$sumExec, total=$total)"

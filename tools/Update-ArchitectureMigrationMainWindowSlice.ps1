#requires -Version 7
<#
.SYNOPSIS
  Пересчитывает строки в partial-файлах MainWindowViewModel / IdeMcpCommandExecutor и обновляет таблицы в docs/architecture-migration.md.

.DESCRIPTION
  Описания — tools/architecture-migration-slice/main-window-slice-descriptions.json;
  шаблон абзаца сводки — tools/architecture-migration-slice/main-window-slice-summary.template.md
  (плейсхолдеры {0}…{3}). Каталог не tools/data: там срабатывает игнор Data/ в .gitignore.
  При появлении нового .cs без ключа в JSON скрипт завершается с ошибкой.

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

function Read-DescriptionsHashtable([string] $jsonPath) {
    if (-not (Test-Path -LiteralPath $jsonPath)) {
        throw "Нет файла описаний: $jsonPath"
    }
    $raw = Get-Content -LiteralPath $jsonPath -Raw -Encoding UTF8
    $o = $raw | ConvertFrom-Json
    $ht = @{}
    foreach ($p in $o.PSObject.Properties) {
        $ht[$p.Name] = [string]$p.Value
    }
    return $ht
}

$contentDir = Join-Path $PSScriptRoot 'architecture-migration-slice'
$descriptionsPath = Join-Path $contentDir 'main-window-slice-descriptions.json'
$summaryTemplatePath = Join-Path $contentDir 'main-window-slice-summary.template.md'

$Descriptions = Read-DescriptionsHashtable $descriptionsPath

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
    if (-not $Descriptions.ContainsKey($name)) {
        throw "Нет описания для ``$name`` — добавь ключ в $descriptionsPath"
    }
    $mwRows.Add([pscustomobject]@{ Name = $name; Lines = $lines; Desc = $Descriptions[$name] })
}

$execRows = [System.Collections.Generic.List[object]]::new()
$sumExec = 0
foreach ($f in $execFiles) {
    if ($null -eq $f) { continue }
    $rel = if ($f.DirectoryName -match 'Generated') { 'Generated/' + $f.Name } else { $f.Name }
    $lines = Get-LineCount $f.FullName
    $sumExec += $lines
    if (-not $Descriptions.ContainsKey($rel)) {
        throw "Нет описания для ``$rel`` — добавь ключ в $descriptionsPath"
    }
    $execRows.Add([pscustomobject]@{ Name = $rel; Lines = $lines; Desc = $Descriptions[$rel] })
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

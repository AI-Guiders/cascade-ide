#requires -Version 7
<#
.SYNOPSIS
  Пересчитывает строки в partial-файлах MainWindowViewModel / IdeMcpCommandExecutor и обновляет таблицы в docs/architecture-migration.md.

.DESCRIPTION
  Описания колонки «Содержание» хранятся в хеш-таблице $Descriptions в этом скрипте.
  При появлении нового .cs файла без ключа скрипт завершается с ошибкой (добавь запись в $Descriptions).

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
    # Как в docs/architecture-migration.md: Measure-Object -Line (не сырой .Count строк)
    return (Get-Content -LiteralPath $path | Measure-Object -Line).Lines
}

# Описания для колонки «Содержание»; ключ — имя файла как в таблице (только имя, путь ViewModels/ не включаем для MW/Executor, для Generated — путь как в таблице).
$Descriptions = [ordered]@{
    'MainWindowViewModel.AutonomousAgent.cs'           = 'Автономный агент (Power)'
    'MainWindowViewModel.Breakpoints.cs'               = 'Брейкпоинты, файловый watcher'
    'MainWindowViewModel.Capabilities.cs'              = 'Реестр capabilities'
    'MainWindowViewModel.CommandPalette.cs'            = 'Палитра команд'
    'MainWindowViewModel.cs'                           = 'Конструктор, дочерние VM, `WorkspaceDiagnostics`, `ExecuteCommandAsync`, навигация к проблемам, `ResolveProvider`'
    'MainWindowViewModel.CSharpLsp.cs'                 = 'Запуск/перезапуск C# LSP'
    'MainWindowViewModel.CursorAcp.cs'                 = 'Путь Cursor ACP'
    'MainWindowViewModel.DocumentsDock.cs'             = 'Документы / dock'
    'MainWindowViewModel.EditorHud.cs'                 = 'HUD редактора'
    'MainWindowViewModel.EditorOllama.cs'              = 'Редактор + Ollama'
    'MainWindowViewModel.Eicas.cs'                     = 'Лента EICAS'
    'MainWindowViewModel.EnvironmentReadiness.cs'      = 'Страница «готовность окружения»; снимок через `EnvironmentReadinessSnapshotBuilder.BuildAllRowsAsync`'
    'MainWindowViewModel.IdeMcpActions.AgentNotes.cs'  = 'Реализация `IIdeMcpActions`: agent-notes'
    'MainWindowViewModel.IdeMcpActions.BuildTest.cs'   = 'MCP: сборка, тесты'
    'MainWindowViewModel.IdeMcpActions.DebuggerPanel.cs' = 'MCP: панель отладки'
    'MainWindowViewModel.IdeMcpActions.Editor.cs'      = 'MCP: редактор'
    'MainWindowViewModel.IdeMcpActions.Git.cs'         = 'MCP: git'
    'MainWindowViewModel.IdeMcpActions.UiAutomation.cs' = 'MCP: UI automation'
    'MainWindowViewModel.IdeMcpActions.Workspace.cs'   = 'MCP: workspace'
    'MainWindowViewModel.LayoutNotifications.cs'       = 'Уведомления раскладки'
    'MainWindowViewModel.MarkdownExport.cs'            = 'Экспорт Markdown'
    'MainWindowViewModel.MarkdownLsp.cs'               = 'Запуск/перезапуск Markdown LSP'
    'MainWindowViewModel.Presentation.cs'              = 'Заголовок, режимы, подписи'
    'MainWindowViewModel.PresentationLayout.cs'          = 'Раскладка / presentation'
    'MainWindowViewModel.RelayCommands.cs'             = 'Relay-команды'
    'MainWindowViewModel.RelayCommands.Debug.cs'       = 'Relay: отладка'
    'MainWindowViewModel.SecondaryShell.cs'            = 'Вторичный контур shell'
    'MainWindowViewModel.SettingsReactive.cs'          = 'Реакции на настройки, сохранение'
    'MainWindowViewModel.ShellState.cs'                = 'Панели, UI-режим, AI, телеметрия'
    'MainWindowViewModel.SolutionBuild.cs'             = 'Сборка, `BuildOutputPanel`'
    'MainWindowViewModel.StartupProject.cs'            = 'Стартовый проект'
    'MainWindowViewModel.UiGitWorkspace.cs'            = 'Git + workspace UI'
    'MainWindowViewModel.ViewBridge.cs'                = 'Мост к view (диалоги, снимки UI)'
    'MainWindowViewModel.WorkspaceHealth.cs'           = 'Связка с Workspace Health'
    'IdeMcpCommandExecutor.cs'                         = '`BuildHandlers`, `ExecuteAsync`'
    'IdeMcpCommandExecutor.Handlers.AgentNotes.cs'    = 'Хендлеры agent-notes'
    'IdeMcpCommandExecutor.Handlers.Chrome.cs'         = 'Хендлеры хрома / видимости'
    'IdeMcpCommandExecutor.Handlers.DapDebug.cs'       = 'DAP / отладка'
    'IdeMcpCommandExecutor.Handlers.DebuggerUi.cs'     = 'Поверхность отладки'
    'IdeMcpCommandExecutor.Handlers.Editor.cs'         = 'Редактор'
    'IdeMcpCommandExecutor.Handlers.PowerDocuments.cs' = 'Power / документы'
    'Generated/IdeMcpCommandExecutor.Generated.g.cs'   = 'Сгенерированные хендлеры (ProtocolDocGen / генератор)'
}

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
    if (-not $Descriptions.Contains($name)) {
        throw "Нет описания для ``$name`` — добавь ключ в `$Descriptions в tools/Update-ArchitectureMigrationMainWindowSlice.ps1"
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
    if (-not $Descriptions.Contains($rel)) {
        throw "Нет описания для ``$rel`` — добавь ключ в `$Descriptions в tools/Update-ArchitectureMigrationMainWindowSlice.ps1"
    }
    $execRows.Add([pscustomobject]@{ Name = $rel; Lines = $lines; Desc = $Descriptions[$rel] })
}

$total = $sumMw + $sumExec
$roundTotal = [math]::Round($total / 1000, 1)
$roundMw = [math]::Round($sumMw / 1000, 1)
$roundExec = [math]::Round($sumExec / 1000, 1)
$ym = Get-Date -Format 'yyyy-MM'

# Шаблон в single-quoted here-string — без съедания backtick/* PowerShell.
$summaryTemplate = @'
`MainWindowViewModel` — **композитор окна**: конструктор, подписки, мост `IIdeMcpActions` → `IdeMcpCommandExecutor`, оркестрация решения/сборки/LSP/MCP. Объём **~{0}k строк** суммарно по partial-классу `MainWindowViewModel*.cs` (**~{1}k**) плюс диспетчер `IdeMcpCommandExecutor*.cs` и `Generated/IdeMcpCommandExecutor.Generated.g.cs` (**~{2}k**); счётчики — ориентир по состоянию репозитория (авто: {3}). Чат, Git, терминал, сборка, инструментирование и т.д. — в **`Features/*`** как дочерние VM; цель дальше — **сужать** главный VM по мере доработок (вынос в сервисы, план B).
'@

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

# План: языковые сервисы C# (гибридный подход)

Зафиксировано решение использовать **вариант C — гибрид**: in-process Roslyn для быстрых сценариев, LSP (OmniSharp) для полного решения.

## Цель

- **Отзывчивость:** ввод и простые действия не ждут поднятия внешнего процесса.
- **Полнота по решению:** диагностика, Go to Definition, Find References, рефакторинги по всему решению — без собственного MSBuildWorkspace, за счёт готового language server.

## Текущее состояние (база)

- **In-process Roslyn** (один файл, кэш):
  - `CSharpLanguageService`: completion (члены после `.`, ключевые слова, LookupSymbols), signature help, подсветка вхождений в текущем файле.
  - Кэш по (path, textHash, line, column); сброс при смене файла/решения.
  - `EditorIntelligence`: триггеры (`.` / Ctrl+Space), debounce, Popup для completion и signature, IBackgroundRenderer для highlight вхождений в текущем файле (без дублирования диагностик в intelligence).
- **Диагностики по всем открытым `.cs`:** `WorkspaceDiagnosticsCoordinator` — debounce ~400 ms, кэш по пути, событие `DiagnosticsChanged`, список для панели Problems (`ProblemsPanelViewModel`). В `CSharpLanguageService.GetDiagnosticsForFile` — **только парсер** (синтаксис/лексика), без семантики однофайловой «скретч»-компиляции, чтобы не было ложных CS0246 при успешном `dotnet build`.
- **Редактор:** на каждой вкладке `DockDocumentView` ставит `EditorDiagnosticBackgroundRenderer` + подписка на координатор (не только активная вкладка); tooltip по наведению на полосу (`HitTest`).
- **LSP (выбор провайдера):** настройки `CSharpLspProvider` (ParseOnly / OmniSharp / CSharpLs / Custom), stdio JSON-RPC, `CSharpLspDiagnosticsHost` + `WorkspaceDiagnosticsCoordinator.SetLspDiagnosticsHost`. Перезапуск при смене решения или настроек.
- **Контракт:** `Services/Lsp/ILspDiagnosticSource.cs`.
- Редактор: AvaloniaEdit, TextMate для подсветки синтаксиса.
- Загрузка решения и файлов — в фоне (LoadSolutionAsync, LoadFileContentAsync).

## Этапы плана

### Этап 1 — Укрепление текущего in-process (при необходимости)

- [ ] При желании: расширить completion (например, больше контекстов, фильтрация по вводу).
- [ ] Сохранить текущее поведение: без открытого решения или до старта LSP всё работает как сейчас.

### Этап 2 — LSP-клиент в CascadeIDE

- [ ] Подключить **OmniSharp.Extensions.LanguageClient** (NuGet) для роли LSP-клиента.
- [ ] Запуск **OmniSharp-Roslyn** (или другого C# LSP-сервера) как дочернего процесса при открытии решения (.sln / .csproj); обмен по stdio (JSON-RPC).
- [ ] Инициализация сессии: `initialize` + `initialized`, привязка к корню решения (workspace folder).
- [ ] Отправка документов: при открытии/изменении файла — `textDocument/didOpen`, `textDocument/didChange` и т.д.

### Этап 3 — Использование LSP по решению

- [ ] **Диагностика:** подписка на `textDocument/publishDiagnostics`, отображение подчёркиваний/списка ошибок в редакторе (или панель проблем).
- [ ] **Completion:** при открытом решении и готовом LSP — запрос `textDocument/completion` (можно дублировать или постепенно переключать с in-process на LSP для файлов из решения).
- [ ] **Go to Definition / Find References:** запросы к LSP, навигация по результатам (открытие файла, позиция в редакторе).
- [ ] По желанию: Hover, Signature Help от LSP для файлов решения.

### Этап 4 — Правило «когда что»

- [ ] Чёткое правило: **если открыто решение и LSP запущен и готов — используем LSP** для completion/diagnostics/definition/references по этому решению.
- [ ] **Иначе** (файл без решения, LSP ещё не поднят, ошибка подключения) — только in-process Roslyn (текущая логика).
- [ ] При смене решения — перезапуск LSP (новый процесс на новое решение), сброс кэша in-process.

### Этап 5 (опционально) — Отладка (DAP)

- [x] Встроенная отладка через **netcoredbg** (протокол DAP по stdio): общий клиент в `DotnetDebug.Core` (`DapClient`), сессия в IDE — `Services/IdeDapDebugSession` (паритет с `dotnet-debug-mcp`).
- [x] Команды MCP/IDE: `debug_launch`, `debug_attach`, `debug_continue`, шаги, стек/переменные — см. `IdeCommands` и `IdeMcpCommandExecutor.Handlers.DapDebug.cs`.
- [x] UI: меню **Отладка**, горячие клавиши F5 / Shift+F5 / F10 / F11 / Shift+F11; диалоги выбора цели (.dll/.exe) и PID для attach (`MainWindow.Dialogs.axaml.cs`).
- Примечание: отдельный NuGet **OmniSharp.Extensions.DebugAdapter.Client** не используется — достаточно общего DAP-клиента и netcoredbg, как в MCP.

## Зависимости и ссылки

- **OmniSharp-Roslyn:** https://github.com/OmniSharp/omnisharp-roslyn (C# language server, LSP по stdio).
- **OmniSharp csharp-language-server-protocol:** https://github.com/OmniSharp/csharp-language-server-protocol (LSP на C#; для этапов 2–4).
- **netcoredbg** (внешний процесс): путь через `NETCOREDBG_PATH` или `PATH`; см. также `Financial/software/open/dotnet-debug-mcp`.

## Документ

- Создан: 2026-02.
- Обновлять по мере выполнения этапов и смены приоритетов.


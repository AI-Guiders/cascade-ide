# План: языковые сервисы C# (гибридный подход)

Зафиксировано решение использовать **вариант C — гибрид**: in-process Roslyn для быстрых сценариев, LSP (OmniSharp) для полного решения.

## Цель

- **Отзывчивость:** ввод и простые действия не ждут поднятия внешнего процесса.
- **Полнота по решению:** диагностика, Go to Definition, Find References, рефакторинги по всему решению — без собственного MSBuildWorkspace, за счёт готового language server.

## Текущее состояние (база)

- **In-process Roslyn** (один файл, кэш):
  - `CSharpLanguageService`: completion (члены после `.`, ключевые слова, LookupSymbols), signature help, подсветка вхождений в текущем файле.
  - Кэш по (path, textHash, line, column); сброс при смене файла/решения.
  - `EditorIntelligence`: триггеры (`.` / Ctrl+Space), debounce, Popup для completion и signature, IBackgroundRenderer для highlight.
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

- [ ] Использовать **OmniSharp.Extensions.DebugAdapter.Client** и запуск отладчика (например, vsdbg/OmniSharp) для F5/присоединения.
- [ ] Интеграция с существующим dotnet-debug-mcp по желанию (общий сценарий с Cursor).

## Зависимости и ссылки

- **OmniSharp-Roslyn:** https://github.com/OmniSharp/omnisharp-roslyn (C# language server, LSP по stdio).
- **OmniSharp csharp-language-server-protocol:** https://github.com/OmniSharp/csharp-language-server-protocol (LSP/DAP на C#, NuGet: `OmniSharp.Extensions.LanguageClient`, `OmniSharp.Extensions.LanguageServer` и др.).
- C# Dev Kit для VS Code опирается на ту же LSP-архитектуру; мы используем те же открытые компоненты, без самого VS Code.

## Документ

- Создан: 2026-02.
- Обновлять по мере выполнения этапов и смены приоритетов.

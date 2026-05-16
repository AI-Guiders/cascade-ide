# ADR 0019: Общий Git Core для Cascade IDE и git-mcp

**Статус:** Accepted · Implemented  
**Дата:** 2026-04-06  
## Связанные ADR

| ADR | Роль |
|-----|------|
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | Стабильные контракты MCP и тестируемая инфраструктура |
| [0002](0002-debug-human-agent-parity.md) | Единый слой состояния отладки для человека и агента |

### Вне ADR

| Документ | Роль |
|----------|------|
| [git-and-submodules-v1.md](../git-and-submodules-v1.md) | Политика git и субмодулей |
| [MCP-PROTOCOL.md](../MCP-PROTOCOL.md) | Протокол IDE MCP |

Внешний репозиторий: `git-mcp` (субмодуль `financial-open`).

### Снимок реализации

| Элемент | Значение |
|---------|----------|
| — | `GitMcp.Core` — общий слой для `ide_git_*` в IDE и git-mcp; паритет argv |

---
## Контекст

1. **Два потребителя одной предметной области:** операции Git для агента реализованы и как **встроенные команды IDE** (`ide_git_*`, `IIdeMcpActions` в `MainWindowViewModel.IdeMcpActions.Git.cs`), и как отдельный процесс **git-mcp** (stdio MCP, каталог тулов, сборка аргументов и JSON-ответов в `Program.cs`).

2. **git-mcp** доведён до стабильного набора инструментов (в т.ч. `fetch`, `pull`, `branch`, `show`, `submodule` рядом с `status`/`diff`/`commit`/`push`), с явными правилами экранирования аргументов и единым контрактом ответа.

3. **Cascade IDE** сейчас покрывает **подмножество** команд и держит логику **локально** (раннер + сбор argv + усечение вывода). При целевом **паритете по смыслу** с git-mcp дублирование станет заметным и будет расходиться при любых правках в одном месте.

4. **Прецедент:** общий слой **AgentNotes.Core** на **IDE** и **agent-notes-mcp** — пакет **[AIGuiders.AgentNotes.Core](https://www.nuget.org/packages/AIGuiders.AgentNotes.Core)** ([исходники](https://github.com/KarataevDmitry/AIGuiders.AgentNotes.Core)).

## Решение (направление)

1. **Выделить библиотеку Git Core** (рабочее имя, например `GitMcp.Core` / `Cascade.Git.Core` — зафиксировать при внедрении) в том же монорепо/субмодульном контуре, что и git-mcp, по аналогии с отдельным репо **AgentNotes.Core** (NuGet-пакет для двух потребителей):
   - построение списка аргументов `git` для каждой поддерживаемой операции;
   - единые правила **кавычек/экранирования** и ограничений (например несовместимость `remote` и `all` для `fetch`);
   - при необходимости — **нормализованное представление результата** (успех, код выхода, текст stdout/stderr, политика усечения), без привязки к MCP SDK или Avalonia.

2. **git-mcp** переводится на Core как **тонкий адаптер**: разбор JSON аргументов MCP → вызов Core → возврат текста/JSON клиенту.

3. **Cascade IDE** переводится на Core в слое **после** абстракции процесса ([0008](0008-mcp-contracts-and-testable-infrastructure.md)): `IGitCommandRunner` (или аналог) остаётся точкой выполнения; VM/сервис собирает команду через Core и передаёт в раннер. UI (панель Git, телеметрия) по-прежнему подписан на события IDE, не на Core напрямую.

4. **Паритет:** перечень операций и семантика (`status`/`diff`/`commit`/`push` и расширения до уровня git-mcp) считаются **источником правды** в Core; `IdeCommands` и MCP-PROTOCOL обновляются осознанно при добавлении тула (см. [0013](0013-command-surface-and-discoverability.md), [0002](0002-debug-human-agent-parity.md)).

5. **Тесты:** юнит-тесты на Core (аргументы, граничные случаи, сообщения об ошибках конфигурации) общие для обоих потребителей; интеграционные тесты остаются у каждого (MCP manifest / IDE dispatch).

## Последствия

- Один PR по изменению поведения git для агента затрагивает **Core**; адаптеры IDE и git-mcp тоньше.
- Появляется **зависимость** Cascade → Core: нужна согласованная политика версий (субмодуль, пакет или относительный `ProjectReference` — выбрать при внедрении).
- Документация: обновить [git-and-submodules-v1.md](../git-and-submodules-v1.md) и [MCP-PROTOCOL.md](../MCP-PROTOCOL.md) по мере расширения `ide_git_*`.

## Отклонённые альтернативы

- **Только документировать контракт** без общей библиотеки — дешевле краткосрочно, не снимает дублирование и риск расхождений при паритете.
- **Считать git-mcp единственным источником и убрать git из IDE** — противоречит встроенному сценарию (панель, телеметрия, офлайн-агент без внешнего exe).

## Реализация (зафиксировано)

- **Расположение исходников:** субмодуль [`git-mcp-core/`](../../../git-mcp-core/) в корне meta-repo `open` (рядом с `git-mcp`). **Канонический remote:** GitLab `Krawler/git-mcp-core`; публичное зеркало и репо для **Trusted Publishing** NuGet — **[KarataevDmitry/git-mcp-core](https://github.com/KarataevDmitry/git-mcp-core)**. **Потребители** подключают пакет **`AIGuiders.GitMcp.Core`** с nuget.org (`PackageReference`).
- **Сборка библиотеки:** `GitMcp.Core.csproj`, `net10.0`, namespace `GitMcp.Core`, NuGet id **`AIGuiders.GitMcp.Core`**. Зависимостей от `System.Text.Json` в Core нет — только примитивы и списки аргументов; `GitArgsResult` для ошибок валидации argv.
- **git-mcp:** `PackageReference` на **`AIGuiders.GitMcp.Core`**; вызов `git` через `ProcessStartInfo.ArgumentList`. Версия MCP-сервера **0.3.0**. `git_status` в MCP — последовательность из Core (`StatusMcpSequence`: `rev-parse` + `status`); в IDE панель по-прежнему `status --short --branch` (`StatusShortBranch`).
- **Cascade IDE:** `PackageReference` на тот же пакет; расширены `ide_git_*` (log, fetch, pull, branch, show, submodule); для `ide_git_push` — `Push(..., defaultOriginWhenRemoteEmpty: false)` (без подстановки `origin` при пустом remote), в отличие от MCP `git_push`.
- **Тесты:** `GitMcp.Tests` через ссылку на `GitMcp.csproj` (транзитивно Core); юнит-тесты `GitCommandBuilder`.
## Открытые вопросы (закрыты при принятии)

- ~~Размещение~~ — каталог в `open`, не внутри субмодуля `git-mcp`.
- ~~Имя~~ — `GitMcp.Core` / `GitMcp.Core`.
- ~~STJ в Core~~ — не используется.
- ~~Порядок миграции~~ — Core + оба адаптера в одном изменении.

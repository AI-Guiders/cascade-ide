# Миграция к архитектурной политике CascadeIDE

**Связь:** [architecture-policy.md](architecture-policy.md).  
**Подход:** strangler — по фазам, без остановки разработки.

## Текущее состояние

`MainWindowViewModel` (~3.4k строк) — **композитор окна** и одновременно носитель логики нижней панели, git, сборки, чата, телеметрии, документов, MCP. Это допустимо как исторический компромисс; цель — **сужать ответственность** по мере доработок.

## Целевая карта срезов

| Срез | Что сейчас (где живёт) | Целевое размещение | Примечание |
|------|------------------------|-------------------|------------|
| **Git** | Свойства/команды/парсинг в `MainWindowViewModel` | `Features/Git/GitPanelViewModel` + при необходимости `GitStatusRow` остаётся в `Models/` | Панель нижней вкладки Git; `MainWindow` передаёт workspace, `LoadSolution`, MCP commit/push |
| **Инфраструктура git** | `Process` + `git` внутри VM | `Services/IGitCommandRunner` / `GitCommandRunner` | Общий для панели, полосы телеметрии, MCP `GitCommit`/`GitStatus` |
| **Сборка / вывод** | `BuildOutput`, `IsBuilding`, команды build | `Features/Build/BuildOutputPanelViewModel` (или `BuildPanelViewModel`) | Вкладка Build output + связанные команды |
| **Терминал** | `TerminalOutput`, `TerminalInput`, команды | `Features/Terminal/TerminalPanelViewModel` | Вкладка Terminal |
| **Чат** | Сообщения, провайдеры, стриминг в `MainWindowViewModel` | `Features/Chat/ChatPanelViewModel`; сообщения уже как `ChatMessageViewModel` | Правая панель |
| **Инструментирование** | События, таймлайн, агент | `Features/Instrumentation/…` или отдельные VM по подпанелям | По мере роста |
| **Решение и документы** | `SolutionRoots`, `OpenDocuments`, dock | Остаётся в `MainWindowViewModel` или выносится **SolutionWorkspaceViewModel** позже | Ядро сессии IDE |
| **MCP-мост** | `IIdeMcpActions` на `MainWindowViewModel` | Остаётся на главном VM; делегирование в сервисы/дочерние VM | Контракт стабилен |

## Фазы

### Фаза 0 — инфраструктура (**сделано**)

- **`IGitCommandRunner` + `GitCommandRunner`:** единая точка запуска `git` по рабочему каталогу. `MainWindowViewModel` использует runner для телеметрии и MCP (`RunGitCommandAsync`).

### Фаза 1 — Git-панель (**сделано**)

- Состояние и команды вкладки **Git** — в **`Features/Git/GitPanelViewModel`**. Зависимости: `IGitCommandRunner`, `GetWorkspacePath`, `IIdeMcpActions`, `LoadSolution`, `RefreshGitSummaryAsync`.
- `MainWindowViewModel`: свойство **`GitPanel`**, видимость вкладки (`IsGitPanelVisible`) и настройки остаются на главном VM; при смене `SolutionPath` вызываются `GitPanel.RefreshRepositoryFlagAsync()` и при открытой вкладке — `RefreshGitPanelAsync()`.
- **`BottomPanelView`:** вкладка Git — внутренний `Grid` с `DataContext="{Binding GitPanel}"`.

### Фаза 2 — Build output и Terminal

- Отдельные VM для вкладок **Build output** и **Terminal** по тому же шаблону, что Git.

### Фаза 3 — Chat

- Вынести состояние чата из главного VM в `ChatPanelViewModel`, оставив в Main только привязку и глобальные настройки (если нужны).

### Фаза 4 — по необходимости

- Instrumentation, облегчённый `MainWindowViewModel` до композиции + мост MCP.

## Правила на время миграции

1. Новый код фич — **в срезе** `Features/<Имя>/`, не в конец `MainWindowViewModel`.
2. Повторное использование **git** — только через `IGitCommandRunner` (или обёртки), не копировать `Process`.
3. Изменения в `MainWindowViewModel` по старым фичам — по возможности сопровождать **микро-выносом** в сервис/панельный VM.

## Версионирование

- **v1** — карта срезов и фазы 0–4.  
- **v1.1** — фаза 1 (Git-панель) отмечена как выполненная.

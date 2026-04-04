# Миграция к архитектурной политике CascadeIDE

**Связь:** [architecture-policy.md](architecture-policy.md).  
**Подход:** strangler — по фазам, без остановки разработки.

## Текущее состояние

`MainWindowViewModel` — **композитор окна**: конструктор, подписки, мост `IIdeMcpActions` → `IdeMcpCommandExecutor`, оркестрация решения/сборки/LSP/MCP. Объём **~3.3k строк** суммарно по **partial-классу** в `ViewModels/MainWindowViewModel*.cs` (не один файл). Чат, Git, терминал, сборка, инструментирование и т.д. — в **`Features/*`** как дочерние VM; цель дальше — **сужать** главный VM по мере доработок (вынос в сервисы, план B).

### Срез `MainWindowViewModel` (карта partial-файлов)

Имена и порядок ниже — для навигации; строки — ориентир по состоянию репозитория.

| Файл | Строк (≈) | Содержание |
|------|------------|------------|
| `MainWindowViewModel.cs` | 270 | Конструктор, дочерние VM, `WorkspaceDiagnostics`, `ExecuteCommandAsync`, навигация к проблемам, `ResolveProvider` |
| `MainWindowViewModel.RelayCommands.cs` | 260 | Команды (Relay) |
| `MainWindowViewModel.IdeMcpActions.BuildTest.cs` | 325 | MCP: сборка, тесты |
| `MainWindowViewModel.IdeMcpActions.AgentNotes.cs` | 302 | MCP: agent-notes |
| `MainWindowViewModel.IdeMcpActions.Workspace.cs` | 287 | MCP: workspace |
| `MainWindowViewModel.UiGitWorkspace.cs` | 187 | Git + workspace UI (телеметрия полосы, refresh) |
| `MainWindowViewModel.ShellState.cs` | 204 | Видимость панелей, режимы UI, ключи AI, телеметрия |
| `MainWindowViewModel.SolutionBuild.cs` | 195 | Сборка решения, вывод в `BuildOutputPanel` |
| `MainWindowViewModel.IdeMcpActions.Editor.cs` | 211 | MCP: редактор |
| `MainWindowViewModel.IdeMcpActions.UiAutomation.cs` | 173 | MCP: UI automation |
| `MainWindowViewModel.Presentation.cs` | 139 | Вычисляемые свойства заголовка, режимов, подписей |
| `MainWindowViewModel.Breakpoints.cs` | 148 | Брейкпоинты и файловый watcher |
| `MainWindowViewModel.CSharpLsp.cs` | 104 | Запуск/перезапуск C# LSP |
| `MainWindowViewModel.AutonomousAgent.cs` | 128 | Автономный агент (Power) |
| `MainWindowViewModel.SettingsReactive.cs` | 119 | Реакции на настройки, сохранение |
| `MainWindowViewModel.IdeMcpActions.Git.cs` | 84 | MCP: git |
| `MainWindowViewModel.IdeMcpActions.DebuggerPanel.cs` | 53 | MCP: панель отладки |
| `MainWindowViewModel.EditorOllama.cs` | 58 | Редактор + Ollama |
| `MainWindowViewModel.DocumentsDock.cs` | 45 | Документы / dock |
| `MainWindowViewModel.ViewBridge.cs` | 46 | Мост к view (запросы к окну) |

**Техдолг по главному VM (без обязательного срока):** тяжёлые куски MCP по-прежнему рядом с VM (`IdeMcpActions.*`); при росте — переносить разбор аргументов и доменную логику в `Services/`, оставляя VM оркестратором (план B внизу документа).

## Целевая карта срезов

| Срез | Что сейчас (где живёт) | Целевое размещение | Примечание |
|------|------------------------|-------------------|------------|
| **Git** | Оркестрация и MCP на `MainWindowViewModel`; вкладка — `GitPanelViewModel` | `Features/Git/GitPanelViewModel` + `GitStatusRow` в `Models/` | `MainWindow` передаёт workspace, `LoadSolution`, MCP commit/push; телеметрия полосы частично в `UiGitWorkspace` / `ShellState` |
| **Инфраструктура git** | `Process` + `git` внутри VM | `Services/IGitCommandRunner` / `GitCommandRunner` | Общий для панели, полосы телеметрии, MCP `GitCommit`/`GitStatus` |
| **Сборка / вывод** | Текст вывода в `BuildOutputPanelViewModel`; оркестрация сборки в `MainWindowViewModel` | `Features/Build/BuildOutputPanelViewModel` | Вкладка Build output |
| **Терминал** | `TerminalPanelViewModel` | `Features/Terminal/TerminalPanelViewModel` | Вкладка Terminal (телеметрия Power на той же вкладке — пока на главном VM) |
| **Чат** | История, ввод, `SendChat`, стриминг в `ChatPanelViewModel` | `Features/Chat/ChatPanelViewModel` + `ChatMessageViewModel` | Правая колонка; провайдер/модель и контекст редактора — замыкания на `MainWindowViewModel` |
| **Инструментирование** | События, таймлайн, агент, тесты, MCP-отладка | `Features/Instrumentation/InstrumentationPanelViewModel` | В разметке — `DataContext="{Binding InstrumentationPanel}"` + `x:DataType`; главный VM не дублирует поля |
| **Решение и документы** | `SolutionRoots`, `OpenDocuments`, dock | Остаётся в `MainWindowViewModel` или выносится **SolutionWorkspaceViewModel** позже | Ядро сессии IDE |
| **MCP-мост** | `IIdeMcpActions` на `MainWindowViewModel` | Остаётся на главном VM; делегирование в сервисы/дочерние VM | Контракт стабилен |

## Фазы

### Фаза 0 — инфраструктура (**сделано**)

- **`IGitCommandRunner` + `GitCommandRunner`:** единая точка запуска `git` по рабочему каталогу. `MainWindowViewModel` использует runner для телеметрии и MCP (`RunGitCommandAsync`).

### Фаза 1 — Git-панель (**сделано**)

- Состояние и команды вкладки **Git** — в **`Features/Git/GitPanelViewModel`**. Зависимости: `IGitCommandRunner`, `GetWorkspacePath`, `IIdeMcpActions`, `LoadSolution`, `RefreshGitSummaryAsync`.
- `MainWindowViewModel`: свойство **`GitPanel`**, видимость вкладки (`IsGitPanelVisible`) и настройки остаются на главном VM; при смене `SolutionPath` вызываются `GitPanel.RefreshRepositoryFlagAsync()` и при открытой вкладке — `RefreshGitPanelAsync()`.
- **`BottomPanelView`:** вкладка Git — внутренний `Grid` с `DataContext="{Binding GitPanel}"`.

### Фаза 2 — Build output и Terminal (**сделано**)

- **`Features/Build/BuildOutputPanelViewModel`** — текст вывода сборки и связанных операций (`BuildOutput`). Видимость вкладки и команды сборки остаются в `MainWindowViewModel`; он по-прежнему заполняет `BuildOutputPanel.BuildOutput` (как для Git — оркестрация снаружи).
- **`Features/Terminal/TerminalPanelViewModel`** — `TerminalOutput`, `TerminalInput`, `RunTerminalCommandCommand` (рабочий каталог — из пути решения через замыкание).
- **`BottomPanelView`:** вкладки Terminal и Build output с `DataContext="{Binding TerminalPanel}"` и `DataContext="{Binding BuildOutputPanel}"`; телеметрия Power на вкладке Terminal остаётся на `MainWindowViewModel`.

### Фаза 3 — Chat (**сделано**)

- **`Features/Chat/ChatPanelViewModel`** — `ChatMessages`, `ChatInput`, `IsChatLoading`, `SendChatCommand`, стриминг через `AiProviderManager`. В конструктор передаются замыкания: активный провайдер, модель Ollama, минимальный контекст, `CurrentFilePath`, `EditorText`.
- **`MainWindowViewModel`:** свойство **`ChatPanel`**, `IsChatPanelExpanded`, выбор провайдера/модели и быстрые команды, подставляющие текст в `ChatPanel.ChatInput`.
- **`ChatPanelView`:** блоки сообщений и ввода с `DataContext="{Binding ChatPanel}"` (свёртка панели и пр. — по-прежнему на главном VM).

### Фаза 4 — Instrumentation (**сделано**)

- **`Features/Instrumentation/InstrumentationPanelViewModel`** — `AgentToolCalls`, `AgentTraceSteps`, `EventTimeline`, `PowerTaskQueueItems`, `TestResultsOutput`, стек/переменные MCP-отладки; `AppendAgentTraceStep` (потокобезопасно).
- **`ViewModels/`** — `DebugStackFrameViewModel`, `DebugVariableViewModel`, `AgentTraceStepViewModel` вынесены в отдельные файлы (типы для `x:DataType` без изменений).
- **`MainWindowViewModel`:** свойство **`InstrumentationPanel`**, подписка на `IsDebugPanelVisible` для строк телеметрии полосы; `IIdeMcpActions.ShowDebugState` и `RunTests` пишут в `InstrumentationPanel`. Дочерние виды биндят блоки к `InstrumentationPanel`, без прокси на главном VM.

### Фаза 5 — события, UI-поток, нагрузка (**основное сделано**)

Цель и порядок шагов — в [architecture-policy.md](architecture-policy.md) (разделы «События, UI-поток и нагрузка» и «Отложенные идеи расширяемости»). Кратко:

1. Явные границы между источниками сигналов и подписчиками (без лишней связности между подсистемами).
2. Единая политика маршалинга на UI-поток для обновлений после фона.
3. Точечно — очереди/батчинг там, где поток данных давит на UI.

**Сделано по п. 2:** контракт **`IUiScheduler`**, реализация **`AvaloniaUiScheduler`** (единственное место с прямым `Dispatcher.UIThread`), доступ из кода через **`UiScheduler.Default`**. Вызовы переведены с размазанного `Dispatcher.UIThread` на `UiScheduler.Default`; для удобства — **`GlobalUsings.cs`** с `global using CascadeIDE.Services`.

**Сделано по п. 3 (диагностики):** **`WorkspaceDiagnosticsCoordinator`** — debounce для Roslyn без LSP; **`CSharpLspDiagnosticsHost`** — коалесcing **`DiagnosticsChanged`** (один `Post` на серию `textDocument/publishDiagnostics`, по тому же принципу, что **`BuildOutputPanelViewModel.Append`**).

**Опционально позже по фазе 5:** при профилировании — ещё батчинг на стороне подписчиков Problems; шина событий / слабее связность (п. 1) — только если вырастет число кросс-подсистемных подписок.

**Сделано по MCP и UI-потоку (дыра закрыта):** единый вход `IIdeMcpActions.ExecuteCommandAsync` в `MainWindowViewModel` маршалит выполнение на UI через `IUiScheduler.InvokeAsync(Func<Task<string>>)`; добавлен перегруз `InvokeAsync<T>(Func<Task<T>>)` в `IUiScheduler` / `AvaloniaUiScheduler`. Так MCP stdio и автономный агент не вызывают хендлеры `IdeMcpCommandExecutor` с фонового потока. Долгие операции по-прежнему не блокируют UI: внутри `IIdeMcpActions` используются `ConfigureAwait(false)`, `Task.Run`, `Post` на панели вывода и т.д.

### План: связность `MainWindowViewModel` / MCP (не фаза 5, отдельная дорожка)

| Шаг | Смысл |
|-----|--------|
| **A — сделано** | Один канал маршалинга MCP → UI (`ExecuteCommandAsync` + `InvokeAsync<Task<string>>`), документация на входе executor. |
| **B — по мере роста** | Новую логику по возможности класть в сервисы с узким интерфейсом; VM оставить оркестратором и точкой `IIdeMcpActions`, без раздувания «командными» методами. |

**Сделано:** коалесcing обновлений **`BuildOutput`** в **`BuildOutputPanelViewModel.Append`** (один `Post` на серию вызовов; `Set`/`Clear`/`FlushPending` инвалидируют отложенный flush); после **`BuildSolutionAsync`** вызывается **`FlushPending`**, чтобы текст и флаг сборки были согласованы до чтения MCP.

**Не в этой фазе:** MEF и загрузка плагинов из каталога — зафиксировано как отложенная идея в политике.

## Правила на время миграции

1. Новый код фич — **в срезе** `Features/<Имя>/`, не в конец `MainWindowViewModel`. Панельный VM вешаем на UI через **`DataContext="{Binding ИмяПанели}"`** (и при compiled bindings — **`x:DataType`** на том же элементе), а не через десятки прокси-свойств на главном VM.
2. Повторное использование **git** — только через `IGitCommandRunner` (или обёртки), не копировать `Process`.
3. Изменения в `MainWindowViewModel` по старым фичам — по возможности сопровождать **микро-выносом** в сервис/панельный VM.

## Версионирование

- **v1** — карта срезов и фазы 0–4.  
- **v1.1** — фаза 1 (Git-панель) отмечена как выполненная.
- **v1.2** — фаза 2 (Build output / Terminal как отдельные VM + привязки в `BottomPanelView`).
- **v1.3** — фаза 3 (`ChatPanelViewModel`, привязки в `ChatPanelView` + `MainWindow.axaml.cs`).
- **v1.4** — фаза 4 (`InstrumentationPanelViewModel`, модели трассы/отладки в отдельных файлах, прокси на главном VM).
- **v1.5** — инструментирование без прокси: разметка с `DataContext` на `InstrumentationPanel`, правила миграции уточнены.
- **v1.6** — фаза 5 (события, UI-поток, батчинг); MEF/плагины вынесены в отложенные идеи политики.
- **v1.7** — фаза 5: введены `IUiScheduler` / `UiScheduler.Default`, маршалинг UI сосредоточен в `AvaloniaUiScheduler`.
- **v1.8** — фаза 5: `BuildOutputPanelViewModel.Append` коалесит обновления привязки; `FlushPending` после сборки решения.
- **v1.9** — фаза 5: `IUiScheduler.InvokeAsync<T>(Func<Task<T>>)`; MCP `ExecuteCommandAsync` всегда на UI; план шага B по выносу логики из VM.
- **v1.10** — фаза 5: коалесcing `DiagnosticsChanged` в `CSharpLspDiagnosticsHost`; фаза 5 отмечена как «основное сделано», опциональные пункты вынесены отдельно.
- **v1.11** — раздел «Срез MainWindowViewModel»: карта partial-файлов и уточнение текущего состояния (не один `.cs` на ~3k строк).
- **v1.12** — обозреватель решения: вложенность файлов как в VS — `<DependentUpon>` из `.csproj` + эвристика для SDK-glob (`Stem.*.cs` → родитель `Stem.cs` в той же папке). Формат `.sln` вложенность не задаёт.
- **v1.13** — рефакторинг: дерево файлов проекта вынесено в `Services/ProjectFileTreeBuilder.cs`, `SolutionParser` — только загрузка решения и сортировка узлов.

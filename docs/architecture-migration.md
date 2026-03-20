# Миграция к архитектурной политике CascadeIDE

**Связь:** [architecture-policy.md](architecture-policy.md).  
**Подход:** strangler — по фазам, без остановки разработки.

## Текущее состояние

`MainWindowViewModel` (~3.2k строк) — **композитор окна** и носитель логики нижней панели, телеметрии, документов, MCP; чат, инструментирование и отдельные вкладки нижней панели вынесены в `Features/*`. Это допустимо как исторический компромисс; цель — **сужать ответственность** по мере доработок.

## Целевая карта срезов

| Срез | Что сейчас (где живёт) | Целевое размещение | Примечание |
|------|------------------------|-------------------|------------|
| **Git** | Свойства/команды/парсинг в `MainWindowViewModel` | `Features/Git/GitPanelViewModel` + при необходимости `GitStatusRow` остаётся в `Models/` | Панель нижней вкладки Git; `MainWindow` передаёт workspace, `LoadSolution`, MCP commit/push |
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

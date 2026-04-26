# Миграция к архитектурной политике CascadeIDE

**Связь:** [architecture-policy.md](architecture-policy.md).  
**Подход:** strangler — по фазам, без остановки разработки.

## Текущее состояние

<!-- AUTO:MAIN-WINDOW-SLICE:SUMMARY:BEGIN -->

`MainWindowViewModel` — **композитор окна**: конструктор, подписки, мост `IIdeMcpActions` → `IdeMcpCommandExecutor`, оркестрация решения/сборки/LSP/MCP. Объём **~7.1k строк** суммарно по partial-классу `MainWindowViewModel*.cs` (**~6k**) плюс диспетчер `IdeMcpCommandExecutor*.cs` и `Generated/IdeMcpCommandExecutor.Generated.g.cs` (**~1.1k**); счётчики — ориентир по состоянию репозитория (авто: 2026-04). Чат, Git, терминал, сборка, инструментирование и т.д. — в **`Features/*`** как дочерние VM; цель дальше — **сужать** главный VM по мере доработок (вынос в сервисы, план B).

<!-- AUTO:MAIN-WINDOW-SLICE:SUMMARY:END -->

### Срез `MainWindowViewModel` (карта partial-файлов)

Имена **по алфавиту** — для поиска; строки — `Measure-Object -Line` по `.cs` (пересчёт: `tools/Update-ArchitectureMigrationMainWindowSlice.ps1`).

<!-- AUTO:MAIN-WINDOW-SLICE:MWVM-TABLE:BEGIN -->

| Файл | Строк (≈) | Содержание |
|------|------------|------------|
| `MainWindowViewModel.AutonomousAgent.cs` | 113 | Автономный агент (Power). |
| `MainWindowViewModel.Breakpoints.cs` | 88 | Брейкпоинты: `BreakpointsFileService` / `BreakpointsStorage` — один источник (ADR 0002). |
| `MainWindowViewModel.Capabilities.cs` | 23 | Реестр capabilities. |
| `MainWindowViewModel.CascadeChord.cs` | 426 | Аккордный слой ADR 0060: корень `cascade_chord` из hotkeys.toml (по умолчанию Ctrl+K), затем тот же хвост мелодии, что после `c:` в палитре (см. `IntentMelodyAliases`), без префикса `c:` и без Enter — если alias однозначен (например `so`). При конфликте префиксов (например `gs` vs `gsu`) точное совпадение после полного ввода или по клавише Enter. |
| `MainWindowViewModel.CommandPalette.cs` | 550 | Палитра команд. |
| `MainWindowViewModel.cs` | 367 | Главный композитор окна (partial-класс, несколько `MainWindowViewModel*.cs`). Карта файлов и ответственности — `docs/architecture-migration.md`, раздел «Срез MainWindowViewModel». |
| `MainWindowViewModel.CSharpLsp.cs` | 120 | Запуск/перезапуск C# LSP. |
| `MainWindowViewModel.CursorAcp.cs` | 36 | Путь Cursor ACP и предпочитаемая модель. |
| `MainWindowViewModel.DebugStackUi.cs` | 35 | Выбор кадра в панели «Стек» Mfd: подгрузка Locals для выбранного кадра (DAP). |
| `MainWindowViewModel.DockInstrumentSlots.cs` | 28 | Какой инструмент показан в слотах PFD/MFD главного окна — по `InstrumentPlacementRuntime` и `DisplaySettings` (в т.ч. `[display.instrument_routing]` и merge `workspace.toml`). Логика — `MainWindowDockedGridInstrumentSlots`. |
| `MainWindowViewModel.DocumentsDock.cs` | 43 | Документы / dock. |
| `MainWindowViewModel.EditorHud.cs` | 102 | Полоса HUD над редактором (ADR 0021 §9): баннеры без отдельного якоря-колонки. Основной сценарий продукта — внешний агент (например Cursor) + Cascade; текст сюда задаётся явно (MCP, диагностика, позже — встроенная автономия), а не «по умолчанию» от автономного цикла Power. |
| `MainWindowViewModel.EditorOllama.cs` | 43 | Состояние редактора, Markdown и выбора модели Ollama. |
| `MainWindowViewModel.Eicas.cs` | 17 | Канал EICAS / CAS — отдельно от полосы телеметрии контура работы (ADR 0021, вариант A). |
| `MainWindowViewModel.EnvironmentReadiness.cs` | 61 | Снимок «готовность окружения» (ADR 0023), отдельно от Workspace Health. |
| `MainWindowViewModel.IdeHealth.cs` | 86 | Связка с Workspace Health. |
| `MainWindowViewModel.IdeMcpActions.AgentNotes.cs` | 45 | Реализация `IIdeMcpActions`: agent-notes. |
| `MainWindowViewModel.IdeMcpActions.BuildTest.cs` | 165 | MCP: сборка, тесты. |
| `MainWindowViewModel.IdeMcpActions.DebuggerPanel.cs` | 73 | Панель отладки и снимок DAP (ADR 0002): один `DebugSessionSnapshot`. |
| `MainWindowViewModel.IdeMcpActions.Editor.cs` | 138 | MCP: редактор. |
| `MainWindowViewModel.IdeMcpActions.Git.cs` | 144 | MCP: git. |
| `MainWindowViewModel.IdeMcpActions.Navigation.cs` | 68 | MCP: семантическая навигация (ADR 0039). |
| `MainWindowViewModel.IdeMcpActions.UiAutomation.cs` | 170 | MCP: UI automation. |
| `MainWindowViewModel.IdeMcpActions.Workspace.cs` | 92 | MCP: workspace. |
| `MainWindowViewModel.IdeMcpHostLifecycle.cs` | 27 | Жизненный цикл IDE MCP-хоста: `ide_ping`, перезапуск внешних MCP и stdio-сессии Cursor ACP. |
| `MainWindowViewModel.LaunchProfiles.cs` | 116 | Селектор launch profile, импорт `launchSettings.json` (ADR 0090). |
| `MainWindowViewModel.LayoutNotifications.cs` | 17 | Инвалидация производных высот `MainGrid` без длинных цепочек `NotifyPropertyChangedFor` в ShellState. |
| `MainWindowViewModel.MarkdownExport.cs` | 55 | Экспорт Markdown. |
| `MainWindowViewModel.MarkdownLsp.cs` | 103 | Запуск/перезапуск Markdown LSP. |
| `MainWindowViewModel.McpBreakpointReveal.cs` | 61 | MCP: постановка брейкпоинта с загрузкой решения и показом строки в редакторе. |
| `MainWindowViewModel.MfdShell.cs` | 86 | Оболочка Mfd: одна активная страница; навигация — команды и палитра. Якорь на экране задаётся presentation (зона Mfd в main и/или окно-хост). |
| `MainWindowViewModel.Presentation.cs` | 286 | Вычисляемые свойства разметки, Workspace Health и видимости панелей (режимы UI). |
| `MainWindowViewModel.PresentationLayout.cs` | 207 | ADR 0017: строка `presentation` и второй `TopLevel` — `MfdHostWindow` с полным вторичным контуром (п. 8). |
| `MainWindowViewModel.PresentationLayoutAuthority.cs` | 14 | Запись intent видимости панелей (семантика «хочу»); фактическая поверхность — `MainWindowShellSurfaceCompositor`. |
| `MainWindowViewModel.RelayCommands.cs` | 294 | Relay-команды. |
| `MainWindowViewModel.RelayCommands.Debug.cs` | 139 | Relay: отладка. |
| `MainWindowViewModel.SettingsReactive.cs` | 175 | Реакции на изменение полей настроек и ключей API: диск, автономный агент, панели. |
| `MainWindowViewModel.ShellState.cs` | 274 | Раскладка панелей, нижняя зона, Workspace Health / автономный агент, ключи провайдеров и чата. |
| `MainWindowViewModel.SolutionBuild.cs` | 195 | Сборка, `BuildOutputPanel`. |
| `MainWindowViewModel.StartupProject.cs` | 326 | Стартовый проект. |
| `MainWindowViewModel.UiGitWorkspace.cs` | 138 | Git + workspace UI. |
| `MainWindowViewModel.ViewBridge.cs` | 62 | Колбэки и провайдеры, которые View подставляет в главный VM (диалоги, UI automation). |
| `MainWindowViewModel.WorkspaceNavigationMap.cs` | 330 | Слот Pfd: отображение карты намерений / `CodeNavigationMapSubgraphDocument` (те же данные, что JSON MCP). Граф подграфа — не синоним `instrument_id`, см. ADR 0065. По доменам: карта намерений (в т.ч. control flow) — CodeNavigation; зависимости файлов — WorkspaceNavigation; submodules — дерево/GitMap (ADR 0062). |
| `MainWindowViewModel.WorkspaceSplitters.cs` | 23 | Сплиттеры рабочей области (MainGrid, обозреватель решения, Git и т.д.): режим «взлёт» — блокировка перетаскивания. |

<!-- AUTO:MAIN-WINDOW-SLICE:MWVM-TABLE:END -->

### `IdeMcpCommandExecutor` (диспетчер MCP → `IIdeMcpActions`)

<!-- AUTO:MAIN-WINDOW-SLICE:EXEC-TABLE:BEGIN -->

| Файл | Строк (≈) | Содержание |
|------|------------|------------|
| `IdeMcpCommandExecutor.cs` | 51 | Диспетчер MCP-команд IDE: разбор args и вызов `IIdeMcpActions` / UI-команд главного окна. |
| `IdeMcpCommandExecutor.Handlers.AgentNotes.cs` | 70 | Хендлеры agent-notes. |
| `IdeMcpCommandExecutor.Handlers.Chrome.cs` | 359 | Хендлеры хрома / видимости. |
| `IdeMcpCommandExecutor.Handlers.DapDebug.cs` | 112 | DAP / отладка. |
| `IdeMcpCommandExecutor.Handlers.DebuggerUi.cs` | 57 | Поверхность отладки. |
| `IdeMcpCommandExecutor.Handlers.Editor.cs` | 108 | Редактор. |
| `IdeMcpCommandExecutor.Handlers.PowerDocuments.cs` | 266 | Power / документы. |
| `Generated/IdeMcpCommandExecutor.Generated.g.cs` | 67 | Сгенерированные хендлеры MCP → `IIdeMcpActions` (`CascadeIDE.ProtocolDocGen`). |

<!-- AUTO:MAIN-WINDOW-SLICE:EXEC-TABLE:END -->

**Техдолг по главному VM (не блокирует развитие):** крупные куски MCP по-прежнему рядом с VM (`IdeMcpActions.*`); дальнейший вынос — по мере изменений. **План B по примитивам MCP для текущего объёма закрыт:** координаты/пути редактора (`EditorTextCoordinateUtilities`), разбор JSON панели отладки (`McpDebugPayloadParsing`), чтение полей args MCP (`McpCommandJsonArgs` в `Services/`) — вне `ViewModels/`. **Готовность окружения ([ADR 0023](adr/0023-environment-readiness-glance.md)):** полный список строк — `EnvironmentReadinessSnapshotBuilder.BuildAllRowsAsync` в `Features/EnvironmentReadiness/Application/` (DAL/канал CCU, ADR 0102); обновление страницы — `EnvironmentReadinessRefreshOrchestrator` (канал → compositor на UI), главный VM вызывает только оркестратор. **Cursor ACP (ADR 0102):** разрешение пути к `cursor-agent`, stdio-процесс и fs в пределах workspace — `Features/CursorAcp/DataAcquisition/`; сессия ACP и `IAcpClient` — `Services/CursorAcp/CursorAcpChatConnection`.

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

Цель и порядок шагов зафиксированы в ADR: [0004](adr/0004-ui-thread-marshaling.md) (маршалинг UI), [0007](adr/0007-signals-coupling-and-ui-backpressure.md) (сигналы, батчинг), [0005](adr/0005-defer-dynamic-plugins-mef.md) (отложенные идеи расширяемости). Краткий навигатор — [architecture-policy.md](architecture-policy.md). Кратко:

<a id="arch-migration-phase5-p1"></a>
1. Явные границы между источниками сигналов и подписчиками (без лишней связности между подсистемами).
<a id="arch-migration-phase5-p2"></a>
2. Единая политика маршалинга на UI-поток для обновлений после фона.
<a id="arch-migration-phase5-p3"></a>
3. Точечно — очереди/батчинг там, где поток данных давит на UI.

**Сделано по [п. 2](#arch-migration-phase5-p2):** контракт **`IUiScheduler`**, реализация **`AvaloniaUiScheduler`** (единственное место с прямым `Dispatcher.UIThread`), доступ из кода через **`UiScheduler.Default`**. Вызовы переведены с размазанного `Dispatcher.UIThread` на `UiScheduler.Default`; для удобства — **`GlobalUsings.cs`** с `global using CascadeIDE.Services`.

**Сделано по [п. 3](#arch-migration-phase5-p3) (диагностики):** **`WorkspaceDiagnosticsCoordinator`** — debounce для Roslyn без LSP; **`CSharpLspDiagnosticsHost`** — коалесcing **`DiagnosticsChanged`** (один `Post` на серию `textDocument/publishDiagnostics`, по тому же принципу, что **`BuildOutputPanelViewModel.Append`**).

**Опционально позже по фазе 5:** при профилировании — ещё батчинг на стороне подписчиков Problems; шина событий / слабее связность ([п. 1](#arch-migration-phase5-p1)) — только если вырастет число кросс-подсистемных подписок.

**Сделано по MCP и UI-потоку (дыра закрыта):** единый вход `IIdeMcpActions.ExecuteCommandAsync` в `MainWindowViewModel` маршалит выполнение на UI через `IUiScheduler.InvokeAsync(Func<Task<string>>)`; добавлен перегруз `InvokeAsync<T>(Func<Task<T>>)` в `IUiScheduler` / `AvaloniaUiScheduler`. Так MCP stdio и автономный агент не вызывают хендлеры `IdeMcpCommandExecutor` с фонового потока. Долгие операции по-прежнему не блокируют UI: внутри `IIdeMcpActions` используются `ConfigureAwait(false)`, `Task.Run`, `Post` на панели вывода и т.д.

### План: связность `MainWindowViewModel` / MCP (не фаза 5, отдельная дорожка)

| Шаг | Смысл |
|-----|--------|
| **A — сделано** | Один канал маршалинга MCP → UI (`ExecuteCommandAsync` + `InvokeAsync<Task<string>>`), документация на входе executor. |
| **B — примитивы (текущий объём закрыт)** | Разбор аргументов JSON и вспомогательная логика без UI — в `Services/` (`EditorTextCoordinateUtilities`, `McpDebugPayloadParsing`, `McpCommandJsonArgs`); `IdeMcpCommandExecutor` остаётся диспетчером к `IIdeMcpActions`. |
| **B — дальше по мере роста** | Новые фичи по возможности — в сервисы с узким интерфейсом; VM — оркестратор и `IIdeMcpActions`, без раздувания «командными» методами. |

**Не блокирует продукт:** выделение **`SolutionWorkspaceViewModel`**, MEF/плагины, опциональный дополнительный батчинг Problems — см. политику и таблицу срезов выше.

**Сделано:** коалесcing обновлений **`BuildOutput`** в **`BuildOutputPanelViewModel.Append`** (один `Post` на серию вызовов; `Set`/`Clear`/`FlushPending` инвалидируют отложенный flush); после **`BuildSolutionAsync`** вызывается **`FlushPending`**, чтобы текст и флаг сборки были согласованы до чтения MCP.

**Не в этой фазе:** MEF и загрузка плагинов из каталога — зафиксировано как отложенная идея в политике.

## Правила на время миграции

1. Новый код фич — **в срезе** `Features/<Имя>/`, не в конец `MainWindowViewModel`. Панельный VM вешаем на UI через **`DataContext="{Binding ИмяПанели}"`** (и при compiled bindings — **`x:DataType`** на том же элементе), а не через десятки прокси-свойств на главном VM.
2. Повторное использование **git** — только через `IGitCommandRunner` (или обёртки), не копировать `Process`.
3. Изменения в `MainWindowViewModel` по старым фичам — по возможности сопровождать **микро-выносом** в сервис/панельный VM.
4. Добыча внешних данных (fs/process/json/toml/wire) — в **Data Acquisition Layer** (`Features/<Feature>/DataAcquisition`), а не в `Cockpit/ComputingUnits/*` (см. ADR 0102).

## Wave 1: MCP thinning (практический срез)

Цель волны: уменьшить плотность логики в `MainWindowViewModel.IdeMcpActions.*`, сохранив `MainWindowViewModel` как orchestration-слой.

- Вынести из `IdeMcpActions.*` подготовку payload/парсинг/валидацию в feature-application сервисы с узкими интерфейсами.
- Оставить в `MainWindowViewModel` только:
  - проверку preconditions контекста окна;
  - вызов соответствующего application-сервиса;
  - публикацию DataBus/UI обновлений.
- Порядок первой волны: `IdeMcpActions.Editor` -> `IdeMcpActions.Navigation` -> `IdeMcpActions.BuildTest`.

## Wave 1: UI clusters thinning

Цель волны: сократить связность крупных partial-кластеров `MainWindowViewModel` без big-bang.

- Кластер `Presentation*`:
  - удерживать вычисления раскладки/видимости в compositor/policy-сервисах;
  - на VM оставить свойства-проекции и orchestration вызовы.
- Кластер `ShellState`:
  - состояния панелей и режимов — в отдельные state-модули по доменам, не в один monolith-файл.
- Кластер `WorkspaceNavigationMap`:
  - graph/data трансформации — в сервисы/CCU;
  - в VM оставить binding-state и команды поверхности.

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
- **v1.14** — план B (примитивы MCP): `McpCommandJsonArgs` в `Services/` вместо вложенного класса в `IdeMcpCommandExecutor`; генератор `ProtocolDocGen` и `IdeMcpCommandExecutor.Generated.g.cs` синхронизированы; тесты на контракт чтения args.
- **v1.15** — зафиксирован слой **Data Acquisition Layer** (fs/process/parse outside CCU), добавлены wave-планы `MCP thinning` и `UI clusters thinning` для следующей итерации strangler.
- **v1.16** — готовность окружения: `EnvironmentReadinessSnapshotBuilder` и `EnvironmentReadinessRefreshOrchestrator` в `Features/EnvironmentReadiness/Application/`; сценарий refresh вынесен из `MainWindowViewModel`; **CASCOPE020** / **CASCOPE021** (граница CCU) — severity **Error** при чистом baseline.
- **v1.17** — DAL-завершение среза Environment Readiness (ADR 0102): `EnvironmentReadinessExecutablePathProbe`, `EnvironmentReadinessEnvSnapshot` / `WellKnownEnv`, `EnvironmentReadinessPathAcquisition`, `EnvironmentReadinessFileFacts` в `Features/EnvironmentReadiness/DataAcquisition/`; I/O и классификация путей вне `Application/`.
- **v1.18** — Cursor ACP DAL: `CursorAcpAgentPath`, `CursorAcpWorkspaceFileAccess` в `Features/CursorAcp/DataAcquisition/`; `global using` для этого пространства имён; `CursorAcpChatConnection` без прямого I/O путей агента и workspace-файлов в обработчиках ACP.

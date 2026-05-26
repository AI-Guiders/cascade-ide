# Миграция к архитектурной политике CascadeIDE

**Связь:** [architecture-policy.md](architecture-policy.md).  
**Подход:** strangler — по фазам, без остановки разработки.

## Стратегия: опора на целевой каркас (CDS · CCU · DAL · IDS)

Геометрия **канал→композитор кабины (CDS)** ([ADR 0036](adr/0036-cds-channel-compositor-surface-pipeline.md)), **вычислительных блоков кабины (CCU)** ([ADR 0097](adr/0097-cockpit-compute-units-transport-to-channel-dto.md)), **DAL vs Application vs UI** ([ADR 0102](adr/0102-data-acquisition-layer-boundary-and-contract.md)) и **IDS** ([ADR 0079](adr/0079-ide-display-system-ids-overlay-pipeline.md)) уже **устоялась** как целевая линия: новые возможности смешиваем с нею, а не с параллельными локальными «мини-архитектурами» в главном окне.

Иначе неизбежное **наполнение `MainWindowViewModel` новыми фичами** съест запас времени на приведение к этому каркасу. Поэтому:

1. **Держим активный упрощающий буфер** вокруг MW: последовательное сужение крупных partial-кластеров (MCP, `Presentation*` / `ShellState`, карта workspace и др.) через оркестраторы и политики в `Features/<домен>/` — см. ниже Wave 1 (MCP / UI clusters).
2. **Новая логика по умолчанию** живёт в `Features/` по слоям DAL → Application/CCU-потребители → биндинг на VM; главный VM — **тонкий композитор** там, где уместно уже зафиксировано таблицей «Целевая карта срезов» и [ADR 0006](adr/0006-presentation-layers-and-feature-slices.md).
3. **[ADR 0009](adr/0009-strangler-migration-and-exceptions.md)** остаётся в силе: полное стирание исторического монолита по дедлайну не обязательно — но без пункта 1 можно соблюдать strangler формально и при этом **не приблизиться к каркасу**, если главный узел только разрастается новыми фичами без выносов.

## Текущее состояние

<!-- AUTO:MAIN-WINDOW-SLICE:SUMMARY:BEGIN -->

`MainWindowViewModel` — **композитор окна**: конструктор, подписки, мост `IIdeMcpActions` → `IdeMcpCommandExecutor`, оркестрация решения/сборки/LSP/MCP. Объём **~4.7k строк** суммарно по partial-классу `MainWindowViewModel*.cs` (**~4.7k**) плюс диспетчер `IdeMcpCommandExecutor*.cs` и `Generated/IdeMcpCommandExecutor.Generated.g.cs` (**~0k**); счётчики — ориентир по состоянию репозитория (авто: 2026-05). Чат, Git, терминал, сборка, инструментирование и т.д. — в **`Features/*`** как дочерние VM; цель дальше — **сужать** главный VM по мере доработок (вынос в сервисы, план B).

<!-- AUTO:MAIN-WINDOW-SLICE:SUMMARY:END -->

### Срез `MainWindowViewModel` (карта partial-файлов)

Имена **по алфавиту** — для поиска; строки — `Measure-Object -Line` по `.cs` (пересчёт: `tools/Update-ArchitectureMigrationMainWindowSlice.ps1`).

<!-- AUTO:MAIN-WINDOW-SLICE:MWVM-TABLE:BEGIN -->

| Файл | Строк (≈) | Содержание |
|------|------------|------------|
| `MainWindowViewModel.AgentEnvironment.cs` | 65 | AEE: PFD status strip, chat trace, epoch stale on write (ADR 0148 W3–W5). |
| `MainWindowViewModel.AutonomousAgent.cs` | 113 | Автономный агент (Power). |
| `MainWindowViewModel.Capabilities.cs` | 23 | Реестр capabilities. |
| `MainWindowViewModel.CascadeChord.cs` | 107 | Аккордный слой ADR 0060: корень `cascade_chord` из hotkeys.toml (по умолчанию Ctrl+K), затем тот же хвост мелодии, что после `c:`. Однозначный обычный alias (например `so`) исполняется без Enter при отсутствии более длинного alias-префикса; параметрические (`wai:`, `els:`:…) — только по Enter или из палитры. При конфликте префиксов (`gs` vs `gsu`) — точный хвост или Enter. |
| `MainWindowViewModel.CommandPalette.cs` | 164 | Палитра команд. |
| `MainWindowViewModel.cs` | 333 | Главный композитор окна (partial-класс, несколько `MainWindowViewModel*.cs`). Карта файлов и ответственности — `docs/architecture-migration.md`, раздел «Срез MainWindowViewModel». |
| `MainWindowViewModel.CSharpLsp.cs` | 120 | Запуск/перезапуск C# LSP. |
| `MainWindowViewModel.CursorAcp.cs` | 36 | Путь Cursor ACP и предпочитаемая модель. |
| `MainWindowViewModel.DebugStackUi.cs` | 35 | Выбор кадра в панели «Стек» Mfd: подгрузка Locals для выбранного кадра (DAP). |
| `MainWindowViewModel.DockInstrumentSlots.cs` | 34 | Какой инструмент показан в слотах PFD/MFD главного окна — по `InstrumentPlacementRuntime` и `DisplaySettings` (в т.ч. `[display.instrument_routing]` и merge `workspace.toml`). Логика — `MainWindowDockedGridInstrumentSlots`. |
| `MainWindowViewModel.DocumentsDock.cs` | 29 | Документы / dock. |
| `MainWindowViewModel.EditorOllama.cs` | 30 | Состояние редактора, Markdown и выбора модели Ollama. |
| `MainWindowViewModel.EditorSession.cs` | 126 | Wave 2: `EditorWorkspaceViewModel` + прокси на MWVM для существующих привязок и MCP. |
| `MainWindowViewModel.Eicas.cs` | 17 | Канал EICAS / CAS — отдельно от полосы телеметрии контура работы (ADR 0021, вариант A). |
| `MainWindowViewModel.EnvironmentReadiness.cs` | 48 | Снимок «готовность окружения» (ADR 0023), отдельно от Workspace Health. |
| `MainWindowViewModel.FeatureCommandProxies.cs` | 66 | Wave 2: прокси RelayCommand на feature VMs (XAML, MCP, hotkeys без смены привязок). |
| `MainWindowViewModel.FeatureSessions.cs` | 13 | Wave 2 этап 5: application shell, debug, build session VMs. |
| `MainWindowViewModel.HybridIndex.cs` | 144 | Hybrid Codebase Index (HCI): status projection and UI commands for the HIS (MFD) page. |
| `MainWindowViewModel.HybridIndexSettings.cs` | 18 | Привязки окна настроек к `HybridIndex` (ADR 0106). |
| `MainWindowViewModel.IdeHealth.cs` | 95 | Связка с Workspace Health. |
| `MainWindowViewModel.IdeMcpHostLifecycle.cs` | 20 | Жизненный цикл IDE MCP-хоста: `ide_ping`, перезапуск внешних MCP и stdio-сессии Cursor ACP. |
| `MainWindowViewModel.IntercomAttachmentReveal.cs` | 44 | Reveal вложения Intercom из чата: загрузка решения при необходимости, затем навигация в редактор (ADR 0128). |
| `MainWindowViewModel.IntercomFeedMetrics.cs` | 26 | `[intercom] feed_metrics` — плотность Skia-ленты (comfortable / compact). |
| `MainWindowViewModel.IntercomTransport.cs` | 34 | Координатор team transport Intercom: подключение SSE/HTTP ingest и делегирование в ChatPanel. |
| `MainWindowViewModel.IntercomTransportSettings.cs` | 35 | Привязки `[intercom.transport]` и Connect/Disconnect (ADR 0144). |
| `MainWindowViewModel.LaunchProfiles.cs` | 90 | Селектор launch profile, импорт `launchSettings.json` (ADR 0090). |
| `MainWindowViewModel.LayoutNotifications.cs` | 17 | Инвалидация производных высот `MainGrid` без длинных цепочек `NotifyPropertyChangedFor` в ShellState. |
| `MainWindowViewModel.MarkdownExport.cs` | 31 | Экспорт Markdown. |
| `MainWindowViewModel.MarkdownLsp.cs` | 103 | Запуск/перезапуск Markdown LSP. |
| `MainWindowViewModel.McpBreakpointReveal.cs` | 49 | MCP: постановка брейкпоинта с загрузкой решения и показом строки в редакторе. |
| `MainWindowViewModel.McpHostBridge.cs` | 52 | Внутренний мост для `MainWindowIdeMcpHost` (Wave 2 Big Bang). |
| `MainWindowViewModel.MfdShell.cs` | 58 | Оболочка Mfd: одна активная страница; навигация — команды и палитра. Якорь на экране задаётся presentation (зона Mfd в main и/или окно-хост). |
| `MainWindowViewModel.PfdBackgroundStatus.cs` | 162 | Компактная полоса статуса над PFD/Forward: индексация HCI и solution warm-up (ADR 0141). |
| `MainWindowViewModel.Presentation.cs` | 277 | Вычисляемые свойства разметки, Workspace Health и видимости панелей (режимы UI). |
| `MainWindowViewModel.PresentationLayout.CockpitSurfaceSnapshot.cs` | 8 | Сборка `CockpitSurfaceState` главного окна (`Build`). |
| `MainWindowViewModel.PresentationLayout.cs` | 91 | ADR 0017: строка `presentation` и второй `TopLevel` — `MfdHostWindow` с полным вторичным контуром (п. 8). |
| `MainWindowViewModel.PresentationLayout.HostShell.cs` | 47 | События «окно-хост открыло полный контур» — скрытие колонок в main (`PresentationLayout`). |
| `MainWindowViewModel.PresentationLayout.HostWindowBounds.cs` | 84 | Персистенция геометрии окон-хостов пресета `presentation` (ADR 0017). |
| `MainWindowViewModel.PresentationLayoutAuthority.cs` | 14 | Запись intent видимости панелей (семантика «хочу»); фактическая поверхность — `MainWindowShellSurfaceCompositor`. |
| `MainWindowViewModel.PrimaryWorkSurface.cs` | 66 | Переключатель лобового якоря Intercom / Editor (ADR 0120). |
| `MainWindowViewModel.SettingsReactive.cs` | 252 | Реакции на изменение полей настроек и ключей API: диск, автономный агент, панели. |
| `MainWindowViewModel.ShellConstruction.cs` | 237 | Конструктор и композиция shell: дочерние VM, шина, DAP/HCI, топология presentation (ADR 0017). |
| `MainWindowViewModel.ShellSession.cs` | 187 | Wave 2 этап 3: `ShellChromeViewModel` + прокси на MWVM для привязок и presentation. |
| `MainWindowViewModel.ShellState.AiProviders.cs` | 58 | Часть `ShellState`: режим ИИ и облачные ключи привязаны к нижнему приложению/чату. |
| `MainWindowViewModel.ShellState.AutonomousAgentStripe.cs` | 63 | Часть `ShellState`: полоса/карточки автономной задачи агента, безопасности, LOC и сводки тестов для IDE Health. |
| `MainWindowViewModel.ShellState.ChatAndSessionConfig.cs` | 34 | Часть `ShellState`: ввод чата и конфиг MCP/ACP для автономной сессии. |
| `MainWindowViewModel.ShellState.cs` | 12 | Состояние раскладки главного окна: три зоны внимания в `MainGrid` (PFD · Forward · MFD), см. ADR 0021 и `docs/ui-ux/cascade-ide-ui-layout-v1.md`. Терминал, сборка, Git и пр. — во вторичном контуре колонки MFD (`MfdShellView` / `MfdShellPageStack`); отдельной строки «нижней панели» на всю ширину под сеткой нет. Режим ИИ и облачные ключи — `MainWindowViewModel.ShellState.AiProviders.cs`; чат и MCP/ACP — `MainWindowViewModel.ShellState.ChatAndSessionConfig.cs`; полоса агента / тесты для IDE Health — `MainWindowViewModel.ShellState.AutonomousAgentStripe.cs`; регион MFD/PFD и страницы контура — `ShellState.RegionAndContour.cs`; режим/UI-сессия и полосы — `ShellState.UiSessionChrome.cs`; модель/Kroki — `ShellState.ModelPullMarkdown.cs`. |
| `MainWindowViewModel.ShellState.ModelPullMarkdown.cs` | 20 | Часть `MainWindowViewModel`: pull модели и превью Markdown / Kroki. |
| `MainWindowViewModel.SolutionBuild.cs` | 122 | Загрузка решения, Ollama install; сборка — `MainWindowBuildSessionViewModel`. |
| `MainWindowViewModel.SolutionWarmup.cs` | 61 | Solution warm-up: оркестратор фаз загрузки решения, лампа статуса и подписка на DataBus (ADR 0141). |
| `MainWindowViewModel.StartupProject.cs` | 140 | Стартовый проект. |
| `MainWindowViewModel.UiGitWorkspace.cs` | 126 | Git + workspace UI. |
| `MainWindowViewModel.ViewBridge.cs` | 66 | Колбэки и провайдеры, которые View подставляет в главный VM (диалоги, UI automation). |
| `MainWindowViewModel.WebAiPortal.cs` | 29 | Страница MFD «веб-портал» (ADR 0108): URL, результат последнего вызова моста, `WebAiPortalCommandBridge`. |
| `MainWindowViewModel.WorkspaceNavigationMap.cs` | 141 | Слот Pfd: отображение карты намерений / `GraphDocument` (те же данные, что JSON MCP). Граф подграфа — не синоним `instrument_id`, см. ADR 0065. По доменам: карта намерений (в т.ч. control flow) — CodeNavigation; зависимости файлов — WorkspaceNavigation; submodules — дерево/GitMap (ADR 0062). |
| `MainWindowViewModel.WorkspaceNavigationMap.Refresh.cs` | 186 | Срез карты workspace: перезапрос refresh и сборка через `WorkspaceNavigationMapRefreshComposer`. |
| `MainWindowViewModel.WorkspaceSplitters.cs` | 23 | Сплиттеры рабочей области (MainGrid, обозреватель решения, Git и т.д.): режим «взлёт» — блокировка перетаскивания. |

<!-- AUTO:MAIN-WINDOW-SLICE:MWVM-TABLE:END -->

### `IdeMcpCommandExecutor` (диспетчер MCP → `IIdeMcpActions`)

<!-- AUTO:MAIN-WINDOW-SLICE:EXEC-TABLE:BEGIN -->

| Файл | Строк (≈) | Содержание |
|------|------------|------------|

<!-- AUTO:MAIN-WINDOW-SLICE:EXEC-TABLE:END -->

**Техдолг по главному VM (не блокирует развитие):** крупные куски MCP по-прежнему рядом с VM (`IdeMcpActions.*`); дальнейший вынос — по мере изменений. **Странглер-фасад и уведомления UI:** связка списка `HybridIndexDependentPresentationNames` и `[NotifyPropertyChangedFor]` у `_hybridIndexLast` закреплена тестом `HybridIndexPresentationNotificationsConsistencyTests`; пачечные вызовы `OnPropertyChanged` для Workspace Health IDE, док-инструментов в слотах и глифов брейкпоинтов сведены к статическим массивам имён и циклу в partial-файлах главного VM. **План B по примитивам MCP для текущего объёма закрыт:** координаты/пути редактора (`EditorTextCoordinateUtilities`), разбор JSON панели отладки (`McpDebugPayloadParsing`), чтение полей args MCP (`McpCommandJsonArgs` в `Services/`) — вне `ViewModels/`. **Готовность окружения ([ADR 0023](adr/0023-environment-readiness-glance.md)):** полный список строк — `EnvironmentReadinessSnapshotBuilder.BuildAllRowsAsync` в `Features/EnvironmentReadiness/Application/` (DAL/канал CCU, ADR 0102); обновление страницы — `EnvironmentReadinessRefreshOrchestrator` (канал → compositor на UI), главный VM вызывает только оркестратор. **Cursor ACP (ADR 0102):** разрешение пути к `cursor-agent`, stdio-процесс и fs в пределах workspace — `Features/CursorAcp/DataAcquisition/`; сессия ACP и `IAcpClient` — `Services/CursorAcp/CursorAcpChatConnection`. **Настройки (ADR 0102):** пути к `%LocalAppData%\CascadeIDE\`, hotkeys и чтение внешнего JSON MCP — `Features/Settings/DataAcquisition/` (`UserSettingsPaths`, `TextFileReadWrite`, `HotkeyTomlLoader`); сериализация и merge display — `Services/SettingsService`. **Workspace (ADR 0102):** загрузка .sln/.csproj, дерево проекта, поиск .sln у файла, папка-workspace, разрешение `workspace_path` для отладки — `Features/Workspace/DataAcquisition/`; обход `SolutionItem` для MCP/JSON — `Features/Workspace/Application/McpSolutionTree`.

## Целевая карта срезов

| Срез | Что сейчас (где живёт) | Целевое размещение | Примечание |
|------|------------------------|-------------------|------------|
| **Git** | Оркестрация и MCP на `MainWindowViewModel`; вкладка — `GitPanelViewModel` | `Features/Git/GitPanelViewModel` + `GitStatusRow` в `Models/` | `MainWindow` передаёт workspace, `LoadSolution`, MCP commit/push; телеметрия полосы частично в `UiGitWorkspace` / `ShellState` |
| **Инфраструктура git** | `Process` + `git` внутри VM | `Services/IGitCommandRunner` / `GitCommandRunner` | Общий для панели, полосы телеметрии, MCP `GitCommit`/`GitStatus` |
| **Сборка / вывод** | Текст вывода в `BuildOutputPanelViewModel`; оркестрация сборки в `MainWindowViewModel` | `Features/Build/BuildOutputPanelViewModel` | Вкладка Build output |
| **Терминал** | `TerminalPanelViewModel` | `Features/Terminal/TerminalPanelViewModel` | Вкладка Terminal (телеметрия Power на той же вкладке — пока на главном VM) |
| **Чат** | История, ввод, `SendChat`, стриминг в `ChatPanelViewModel` | `Features/Chat/ChatPanelViewModel` + `ChatMessageViewModel` | Правая колонка; провайдер/модель и контекст редактора — замыкания на `MainWindowViewModel` |
| **Инструментирование** | События, таймлайн, агент, тесты, MCP-отладка | `Features/Instrumentation/InstrumentationPanelViewModel` | В разметке — `DataContext="{Binding InstrumentationPanel}"` + `x:DataType`; главный VM не дублирует поля |
| **Решение и документы** | `SolutionRoots`, `OpenDocuments`, dock | **`SolutionWorkspaceViewModel`** + **`DocumentsWorkspaceViewModel`** (`Features/Workspace`, `Features/Documents`); MWVM — композитор и `SolutionLoadSessionApplyProjection.IHost` | Ядро сессии IDE |
| **MCP-мост** | `IIdeMcpActions` на `MainWindowViewModel` | Остаётся на главном VM; делегирование в сервисы/дочерние VM | Контракт стабилен |

## Фазы

### Фаза 0 — инфраструктура (**сделано**)

- **`IGitCommandRunner` + `GitCommandRunner`:** единая точка запуска `git` по рабочему каталогу. `MainWindowViewModel` использует runner для телеметрии и MCP (`RunGitCommandAsync`).

### Фаза 1 — Git-панель (**сделано**)

- Состояние и команды вкладки **Git** — в **`Features/Git/GitPanelViewModel`**. Зависимости: `IGitCommandRunner`, `GetWorkspacePath`, `IIdeMcpActions`, `LoadSolution`, `RefreshGitSummaryAsync`.
- `MainWindowViewModel`: свойство **`GitPanel`**, видимость вкладки (`IsGitPanelVisible`) и настройки остаются на главном VM; при смене `SolutionPath` вызываются `GitPanel.RefreshRepositoryFlagAsync()` и при открытой вкладке — `RefreshGitPanelAsync()`.
- **Страница Git в MFD** (`MfdShellPageStack`): контент страницы с `DataContext="{Binding GitPanel}"` (нет отдельного `BottomPanelView` во Flight).

### Фаза 2 — Build output и Terminal (**сделано**)

- **`Features/Build/BuildOutputPanelViewModel`** — текст вывода сборки и связанных операций (`BuildOutput`). Видимость вкладки и команды сборки остаются в `MainWindowViewModel`; он по-прежнему заполняет `BuildOutputPanel.BuildOutput` (как для Git — оркестрация снаружи).
- **`Features/Terminal/TerminalPanelViewModel`** — `TerminalOutput`, `TerminalInput`, `RunTerminalCommandCommand` (рабочий каталог — из пути решения через замыкание).
- **Страницы Terminal и Build output в колонке MFD** (`MfdShellPageStack`): `DataContext="{Binding TerminalPanel}"` и `DataContext="{Binding BuildOutputPanel}"`; телеметрия Power на странице Terminal остаётся на `MainWindowViewModel`. *(В старой топологии использовались привязки в `BottomPanelView`.)*

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

## Feature archetype v1 (DoD новой фичи)

Чеклист «куда класть код» и шаблон каталогов — **[design/feature-archetype-v1.md](design/feature-archetype-v1.md)**. IDE chrome tokens — **[design/ide-chrome-tokens-v1.md](design/ide-chrome-tokens-v1.md)**. Цель: развитие в том же ритме, что CDS/CCU/DAL/IDS, без роста MWVM по умолчанию.

## Правила на время миграции

1. Новый код фич — **в срезе** `Features/<Имя>/`, не в конец `MainWindowViewModel`. См. [feature-archetype-v1.md](design/feature-archetype-v1.md). Панельный VM вешаем на UI через **`DataContext="{Binding ИмяПанели}"`** (и при compiled bindings — **`x:DataType`** на том же элементе), а не через десятки прокси-свойств на главном VM.
2. Повторное использование **git** — только через `IGitCommandRunner` (или обёртки), не копировать `Process`.
3. Изменения в `MainWindowViewModel` по старым фичам — по возможности сопровождать **микро-выносом** в сервис/панельный VM.
4. Добыча внешних данных (fs/process/json/toml/wire) — в **Data Acquisition Layer** (`Features/<Feature>/DataAcquisition`), а не в `Cockpit/ComputingUnits/*` (см. ADR 0102).
5. Крупную бизнес-логику в `MainWindowViewModel` не наращивать: сначала оркестратор/политика в `Features/<>` с узким контрактом; MV — только проверки контекста окна, вызов и обновление UI/шины (см. раздел «Стратегия: опора на целевой каркас» выше).

## Wave 1: MCP thinning (практический срез)

Цель волны: уменьшить плотность логики в `MainWindowViewModel.IdeMcpActions.*`, сохранив `MainWindowViewModel` как orchestration-слой.

- Вынести из `IdeMcpActions.*` подготовку payload/парсинг/валидацию в feature-application сервисы с узкими интерфейсами.
- Оставить в `MainWindowViewModel` только:
  - проверку preconditions контекста окна;
  - вызов соответствующего application-сервиса;
  - публикацию DataBus/UI обновлений.
- Порядок первой волны: `IdeMcpActions.Editor` -> `IdeMcpActions.Navigation` -> `IdeMcpActions.BuildTest`.
- Первый срез выполнен: JSON для `get_open_document_text` (поиск вкладки по пути через `IdeMcpEditorOrchestrator.BuildGetOpenDocumentTextResponse`), якорь control-flow для `get_code_navigation_context` (`IdeMcpNavigationOrchestrator.ResolveControlFlowLineColumn`), payload `get_solution_files` (`IdeMcpBuildTestOrchestrator.BuildSolutionFilesJson`).
- Второй срез: `HybridIndexScopeResolver` в `Features/HybridIndex/Application/` и `IdeMcpHybridIndexScope` для MCP `codebase_index_*` (`TryResolveForCodebaseIndexCommand` — без дубля логики в VM); MCP agent-notes через `IdeMcpAgentNotesOrchestrator`; `ResolveHybridIndexScope` в VM — делегирует ресолверу. Ошибки HCI и ping/rebuild хоста вынесены в оркестраторы (**v1.38**); дальнейший вынос build/test/UI — точечно при росте API.
- Третий срез: `Services.IdeMcpSolutionPathAvailability.IsRunnableSolutionFile` для MCP build/test/code-cleanup (I/O вне статического оркестратора, CASCOPE031); мутации UI тестов — `IdeMcpBuildTestOrchestrator.IdeMcpTestRunInstrumentationMutation`.
- Четвёртый срез: единый контур **`BuildStateChanged` → DataBus → `RebuildIdeHealth`** (ADR 0099): локальная сборка решения и MCP code cleanup не шлют «сырой» `_ideDataBus.Publish` без пересборки полосы; MCP-пути после `ConfigureAwait(false)` используют `PublishIdeBuildStateOnUiAsync`.
- Пятый срез: MCP git целиком на **`IdeMcpGitWorkspaceSession`** (`Features/IdeMcp/Application/`), VM — только workspace + `RefreshGitSummaryAsync`; список команд preflight-fix — приватная константа в сессии (CASCOPE030: не статическое поле в оркестраторе).
- Шестой срез: **`IdeMcpIdeStateUiCapture`** + `BuildIdeStatePayload(capture, diagnostics)`; `get_ide_state` — diagnostics вне UI, снимок UI одним `CaptureIdeMcpIdeStateUi` (включая CDS через `BuildCockpitSurfaceSnapshot`).
- Седьмой срез: **`IdeMcpDapSnapshotUiPlan`** / `IdeMcpDebugOrchestrator.BuildDapSnapshotUiPlan` для `ApplyDapDebugSnapshotToUi` (без `File.Exists` в Application, CASCOPE031); **`get_debug_snapshot`** — сериализация без очереди на UI (`GetSnapshot` под lock).

## Wave 1: UI clusters thinning

Цель волны: сократить связность крупных partial-кластеров `MainWindowViewModel` без big-bang.

- Кластер `Presentation*`:
  - удерживать вычисления раскладки/видимости в compositor/policy-сервисах;
  - на VM оставить свойства-проекции и orchestration вызовы.
  - первый срез (**v1.40**): **`MainWindowPresentationSurfaceProjection`** (`Features/Shell/Application`) — заголовок окна, mount-style/топология, контур MFD, телеметрия-подписи, безопасность агента, mount-контекст IDE Health; плейсхолдеры риска/результата и дефолты в **ShellState.AutonomousAgentStripe** через константу проекции.
  - второй срез (**v1.40b**): **`IdeHealthStripPresentationProjection`** — строки полосы IDE Health (build/tests/debug line + cockpit-short) из **`IdeHealthInputSnapshot?`**; геттеры **`MainWindowViewModel.Presentation`** только проксируют последний снимок.
  - третий срез (**v1.40c**): кластер **`PresentationLayout`** разнесён на partial: топология/MainGrid (**`PresentationLayout.cs`**), **`PresentationLayout.HostShell`**, **`PresentationLayout.HostWindowBounds`**, **`PresentationLayout.CockpitSurfaceSnapshot`** (CDS-сборка без изменения логики).
  - четвёртый срез (**v1.40d**): **`MainWindowPresentationCapabilitiesProjection`** — булевы цепочки и подписи из **`UiModeCapabilities`** (IDE Health strip / EICAS chrome / instrumentation / risk-result карточки / Skia overlay / safety ordinal / LOC-бейдж); тесты **`MainWindowPresentationCapabilitiesProjectionTests`**.
- Кластер `ShellState`:
  - состояния панелей и режимов — в отдельные state-модули по доменам, не в один monolith-файл.
  - четвёртый срез: **`MainWindowViewModel.ShellState.AiProviders.cs`** — режим ИИ (`AiMode`, облачный провайдер, вычисляемые флаги выбора) и поля API-ключей; геометрия регионов и видимость страниц MFD после v1.39 — **`ShellState.RegionAndContour`**.
  - пятый срез: **`MainWindowViewModel.ShellState.ChatAndSessionConfig.cs`** — клавиша отправки чата, thinking/минимальный контекст, JSON внешних MCP, `AcpAutoInjectIdeMcp`.
  - шестой срез: **`MainWindowViewModel.ShellState.AutonomousAgentStripe.cs`** — активная задача агента, риск/результат/шаг, `SafetyLevel`, LOC-бейдж, сводка/бейдж тестов для полосы IDE Health.
  - седьмой срез (**v1.39**): enum **`CascadeIDE.Models.Shell.CommandPaletteHost`** и partial **`ShellState.RegionAndContour`** / **`ShellState.UiSessionChrome`** / **`ShellState.ModelPullMarkdown`**; «базовый» **`ShellState.cs`** сведён к краткой сводке + привязкам текстов вторичных групп редакторов.
- Кластер `WorkspaceNavigationMap`:
  - graph/data трансформации — в сервисы/CCU;
  - в VM оставить binding-state и команды поверхности.
- Первый срез (карта PFD): **`CodeNavigationMapPresentationProjection`** + статические `CodeNavigationMapSettings.ViewWantsList` / `ViewWantsGraph`; биндинги list/graph/бейдж/has-related в `MainWindowViewModel.WorkspaceNavigationMap` делегируют в проекцию.
- Второй срез: **`WorkspaceNavigationMapContextJsonBuilder`** (ветвление related / subgraph / control-flow JSON внутри фонового refresh) и **`CodeNavigationMapViewportPolicy`** (пороги ширины viewport мини-карты); тесты `WorkspaceNavigationMapContextAndViewportTests`.
- Третий срез: **`WorkspaceNavigationMapRefreshComposer`** — разбор JSON refresh, композиция сцены + trace-flow, related-строки; **снимок CDS для control-flow** собирается на **UI-потоке** вместе с контекстом refresh и передаётся в композитор как `CockpitSurfaceState` (без чтения VM с пула); тесты `WorkspaceNavigationMapRefreshComposerTests`.
- Четвёртый срез (**v1.40e**): partial **`MainWindowViewModel.WorkspaceNavigationMap.Refresh`** — debounce、`RunWorkspaceNavigationMapRefreshAsync` и viewport width; файл привязок/команд карты (**`WorkspaceNavigationMap.cs`**) укорочен до состояния и проекций.
  - пятый срез (**v1.40f**): **`MainWindowPresentationSurfaceProjection`** — видимость сплита main grid (`IsMainGridSplitColumnVisible`), флаги Skia-mount IDE Health (колонка / окно-хост), **`ResolveInstrumentMountStyleForSlot`**; отдельно **`MainWindowPresentationDapProjection`** для паузы/«running» DAP; геттеры **`MainWindowViewModel.Presentation`** только делегируют; тесты **`MainWindowPresentationDapProjectionTests`** и доп. кейсы в **`MainWindowPresentationSurfaceProjectionTests`**.
  - шестой срез (**v1.40g**): **`IMainWindowHostSurfaceInput`** (`Cockpit/Composition/HostSurface`) — **`MainWindowHostSurfaceProjection`** принимает контракт вместо ссылки на **`MainWindowViewModel`**; реализация на VM через partial; тест **`MainWindowHostSurfaceProjectionTests`**.
- Кластер **Solution / Build / Startup / UiMode** (завершение Wave 1 UI, **v1.45–v1.48**):
  - **`StartupProjectRefreshProjection`** — восстановление стартового `.csproj` после `LoadSolution`; **`MainWindowViewModel.StartupProject`** — делегирование.
  - **`SolutionLoadUiApplyProjection`** — первая страница MFD после загрузки решения; **`MainWindowViewModel.SolutionBuild`** (`LoadSolutionAsync`).
  - **`MainWindowBuildSolutionPrepProjection`** — `CanBuild` / prep заголовка и страницы Build; сборка по-прежнему через **`DotnetSolutionChunkedBuildOrchestrator`**.
  - **`UiModeLayoutApplyProjection`** — план раскладки + persist workspace; **`ApplyUiModeLayout`** в **`UiGitWorkspace`**.

### Wave 1 — статус (текущий объём **закрыт**)

Фазы **0–5** и Wave 1 (**MCP thinning** + **UI clusters thinning** по плану v1.15–v1.48) считаются **выполненными** для зафиксированного scope strangler: главный VM остаётся композитором (подписки, дочерние VM, `IdeMcp` property), но крупные кластеры вынесены в `Features/*/Application` оркестраторами/проекциями.

### Wave 2 — Big Bang MCP (v1.53, этап 1)

**Цель:** убрать реализацию `IIdeMcpActions` с `MainWindowViewModel` — только композиция и `public IIdeMcpActions IdeMcp => _ideMcpHost`.

**Сделано (этап 1):**

- **`MainWindowIdeMcpHost`** (`Features/IdeMcp/Application/`, partial’ы бывших `MainWindowViewModel.IdeMcpActions.*`) — единственная реализация `IIdeMcpActions`.
- **`MainWindowViewModel.McpHostBridge`** — internal-доступ к приватным полям/методам VM для host (git, editor providers, DataBus, breakpoints, debug stack).
- **`IdeMcpCommandExecutor`** принимает `(MainWindowViewModel vm, IIdeMcpActions actions)`; generated/handlers вызывают `_actions`, не cast VM.
- **`ProtocolDocGen`** генерирует `_actions` в pass-through хендлерах.
- Call sites: `App.axaml.cs` (MCP stdio), `AgentContractHeadlessRuntime`, hotkeys, command palette, Intercom, Git panel (`IdeMcp`), DAP callback → `_ideMcpHost.ApplyDapDebugSnapshotToUi`.
- RelayCommand превью Markdown остались на MWVM (`MainWindowViewModel.IdeMcpUiRelayCommands.cs`).

**Этап 2 (v1.54):**

- **`EditorWorkspaceViewModel`** (`Features/Editor/`) — `CurrentFilePath`, `EditorText`, selection, `IsLoadingCurrentFile`, markdown preview flags.
- MWVM — прокси-свойства + ретрансляция `PropertyChanged` (Views/MCP без массовой правки).

**Этап 3 (v1.55):**

- **`ShellChromeViewModel`** (`Features/Shell/`) — регионы MainGrid, видимость MFD-страниц, `UiMode`, `EditorGroupCount`, `IsBuilding`, `CurrentMfdShellPage`, …
- **`ShellChromePresentationNotifyMap`** — presentation-зависимости на MWVM (бывшие `NotifyPropertyChangedFor` на shell-полях).
- MWVM — `Shell` + прокси + `OnShellChromePropertyChanged`; reactive side-effects (настройки, MFD-навигация, bloom UI mode) в `MainWindowViewModel.ShellSession`.
- Удалены `MainWindowViewModel.ShellState.RegionAndContour` / `UiSessionChrome` (поля перенесены).

**Этап 4 (v1.56):**

- **Shell:** layout/MFD/UI mode RelayCommand → `ShellChromeViewModel.Commands.*`; MWVM — `MainWindowViewModel.FeatureCommandProxies` (палитра, MCP, hotkeys без смены имён команд).
- **Documents:** вкладки/группы → `DocumentsWorkspaceViewModel.Commands`; `CloseDocument` API → `CloseDocumentByPath`.
- **Safety L1–L3** — пока на MWVM (`RelayCommands.UiMode`).
- Удалены `RelayCommands.Layout` / `RelayCommands.Documents`; `ToggleMfdRegionExpanded` убран из `SolutionBuild`.

**Этап 5 (v1.57):**

- **Application shell:** `MainWindowApplicationShellViewModel` — меню (открыть решение/папку/файл), темы, язык UI, окна-хосты; прокси команд на MWVM.
- **Debug:** `MainWindowDebugSessionViewModel` — F5/attach/step/stop; `DebugLaunchInteractiveAsync` → `Debug.*`; `StateChanged` → `Debug.NotifyRelayCommandsChanged()`.
- **Build:** `MainWindowBuildSessionViewModel` — `BuildSolutionAsync`, `HideBuildOutput`; bridge `HostDotnetRunner`.
- **Editor slice:** `EditorWorkspaceViewModel.DebugGlyphs` (брейкпоинты, `DebugPosition*`), `EditorWorkspaceViewModel.Hud` (баннер диагностик/вхождений); прокси на MWVM.
- Удалены `RelayCommands.Shell`, `RelayCommands.Debug`; build-команды убраны из `SolutionBuild`; удалены `Breakpoints.cs`, `EditorHud.cs` на MWVM.
- `ShowDebugInfoAsync`, `GetWorkspacePath` — internal на host; тесты MCP → `vm.IdeMcp`.

**Этап 6 (v1.58):**

- **Shell:** `ShellChromeViewModel.Commands.Safety` (L1–L3), `Commands.MarkdownPreview` (MFD preview / отдельное окно).
- **Editor:** `EditorWorkspaceViewModel.DebugHints`, `InlineHints`, `StabilizedInput`; прокси на MWVM.
- Удалены `RelayCommands.UiMode`, `IdeMcpUiRelayCommands`, `EditorDebugHints` / `EditorInlineHints` / `EditorStabilizedInput` на MWVM.
- Bridge: `HostUpdateCodeNavigationMapCaretOffset`.

**Дальше:** метрики LOC в таблице partial MWVM; крупные кластеры (navigation map, HCI) — Plan B по мере фич.

**Backlog MWVM (закрыт, v1.49–v1.52):**

- **v1.49** — **`SolutionWorkspaceViewModel`** в `Features/Workspace/`; **`SolutionLoadSessionApplyProjection`** — применение загрузки решения (дерево + сброс редактора + MFD); тесты **`SolutionLoadSessionApplyProjectionTests`**.
- **v1.50** — **`IdeMcpCommandExecutor`** перенесён в **`Features/IdeMcp/Execution/`** (диспетчер вне `ViewModels/`; генератор **`ProtocolDocGen`** и **`CascadeIDE.csproj`** синхронизированы). MWVM по-прежнему владеет экземпляром и маршалит UI.
- **v1.51** — Точечный вынос MCP/UI при росте API — **правило на поддержку** ([feature-archetype-v1.md](design/feature-archetype-v1.md)); не big-bang.
- **v1.52** — MEF/плагины и опциональный дополнительный батчинг Problems — **вне scope** (политика ADR 0009 / фаза 5 «основное сделано»); коалесcing BuildOutput и Diagnostics уже в продукте.

Новые фичи — по **feature-archetype**; MWVM не раздуваем без выноса.

## Версионирование

- **v1** — карта срезов и фазы 0–4.  
- **v1.1** — фаза 1 (Git-панель) отмечена как выполненная.
- **v1.2** — фаза 2 (Build output / Terminal как отдельные VM + привязки во Flight — страницы `MfdShellPageStack`; исторически упоминался `BottomPanelView`).
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
- **v1.19** — Settings DAL: `UserSettingsPaths`, `TextFileReadWrite`, `HotkeyTomlLoader` в `Features/Settings/DataAcquisition/`; `McpExternalServersJsonResolver` читает файл через DAL. Workspace: `SolutionParser`, `ProjectFileTreeBuilder`, `SolutionFileLocator`, `FolderWorkspaceTreeBuilder`, `DebugWorkspacePath` — `Features/Workspace/DataAcquisition/`; `McpSolutionTree` — `Features/Workspace/Application/`; `global using` для Settings и Workspace; `SolutionFileLocator` не зависит от `EditorTextCoordinateUtilities` (локальное сравнение путей).
- **v1.20** — Wave MCP thinning (первый срез): оркестраторы `IdeMcpEditorOrchestrator` / `IdeMcpNavigationOrchestrator` / `IdeMcpBuildTestOrchestrator` + тесты `IdeMcpOrchestratorThinningTests`.
- **v1.21** — MCP thinning второй заход: `HybridIndexScopeResolver`, `IdeMcpHybridIndexScope`, `IdeMcpAgentNotesOrchestrator`; тесты `HybridIndexScopeAndIdeMcpScopeTests`.
- **v1.22** — `IdeMcpSolutionPathAvailability`, `IdeMcpTestRunInstrumentationMutation`; тесты `IdeMcpSolutionPathAvailabilityTests`.
- **v1.23** — `PublishIdeBuildStateOnUiAsync`; MCP `RunCodeCleanupAsync` обрамлён `BuildStateChanged`; `BuildSolutionAsync` — `PublishToIdeDataBusAndRebuild` вместо «тихой» публикации в шину.
- **v1.24** — `IdeMcpGitWorkspaceSession` (+ тесты `IdeMcpGitWorkspaceSessionTests`); `MainWindowViewModel.IdeMcpActions.Git` — делегирование в сессию.
- **v1.25** — `IdeMcpIdeStateUiCapture`; `IdeMcpWorkspaceOrchestrator.BuildIdeStatePayload` по снимку; тест в `IdeMcpOrchestratorThinningTests`.
- **v1.26** — DAP UI-план + MCP `GetDebugSnapshotAsync` без `UiScheduler.InvokeAsync`.
- **v1.27** — Wave UI clusters: `CodeNavigationMapPresentationProjection`, `CodeNavigationMapSettings.ViewWants*`; тесты `CodeNavigationMapPresentationProjectionTests`.
- **v1.28** — Wave UI clusters: `WorkspaceNavigationMapContextJsonBuilder`, `CodeNavigationMapViewportPolicy`; тесты `WorkspaceNavigationMapContextAndViewportTests`.
- **v1.29** — Явная **стратегия целевого каркаса** (CDS / CCU / DAL / IDS), приоритет упрощающего буфера вокруг `MainWindowViewModel` и правило 5 раздела «Правила на время миграции» — чтобы strangler совпадал с реальным приближением к устоявшейся линии слоёв, а не откладывался потоком фич.
- **v1.30** — Wave UI clusters: `WorkspaceNavigationMapRefreshComposer` (пост-JSON конвейер карты PFD); тесты `WorkspaceNavigationMapRefreshComposerTests`.
- **v1.31** — Тот же срез: **CDS-снимок для trace-flow** на карте захватывается на UI до фонового парсинга/компоновки, контракт `Compose(.., CockpitSurfaceState?)` без `Func` с пула.
- **v1.32** — Wave UI clusters: вынесен partial **`MainWindowViewModel.ShellState.AiProviders`** (режим ИИ и облачные ключи) из `ShellState.cs`.
- **v1.33** — Wave UI clusters: **`MainWindowViewModel.ShellState.ChatAndSessionConfig`** (чат + MCP/ACP конфиг автономной сессии).
- **v1.34** — Wave UI clusters: **`MainWindowViewModel.ShellState.AutonomousAgentStripe`** (полоса автономного агента + тесты/LOC для IDE Health).
- **v1.35** — Терминология раскладки: **три зоны** (PFD · Forward · MFD); терминал/сборка/Git — **вторичный контур колонки MFD**, не отдельная «нижняя панель». Уточнены xmldoc `ShellState` и флаги страниц MFD.
- **v1.36** — Переименование: `IsBottomPanelVisible` → **`IsMfdContourContentVisible`** (флаги контента стека вторичного контура MFD).
- **v1.37** — Разметка и снимки: `Border#BottomPanelShell` → **`MfdContourStackHost`** (`MfdShellView.axaml`); ключ **`layout_regions`** в MCP/DeepSnapshot обновлён; доки без отсылки к вымышленной «нижней панели» главного окна во Flight.
- **v1.38** — Wave MCP thinning (завершение волны в текущем объёме): **`IdeMcpBuildTestOrchestrator`** — поверхность панели при missing solution / ошибке сборки; **`IdeMcpHostOrchestrator`** — JSON `ping`/рестарт MCP; **`IdeMcpHybridCodebaseIndexOrchestrator`** — литералы ошибок и `SerializeReindexFailed`; дедуп **`PublishIdeMcpTestRunMutation`** в `IdeMcpActions.BuildTest`.
- **v1.39** — Wave UI clusters: доменное разнесение **`MainWindowViewModel.ShellState`**: регион/контур MFD (**`ShellState.RegionAndContour.cs`**), режим UI и сборка (**`UiSessionChrome.cs`**), Kroki/modelfetch (**`ModelPullMarkdown.cs`**); enum палитры в **`Models/Shell/CommandPaletteHost.cs`** (хост-окна + тесты пользуются из `CascadeIDE.Models.Shell`).
- **v1.40** — Wave UI clusters, кластер **Presentation**: статическая проекция **`MainWindowPresentationSurfaceProjection`** для вычисляемых свойств **`MainWindowViewModel.Presentation`**, тесты **`MainWindowPresentationSurfaceProjectionTests`**.
- **v1.40b** — тот же кластер: **`IdeHealthStripPresentationProjection`** + тесты **`IdeHealthStripPresentationProjectionTests`**; VM не дублирует разбор вложенного снимка в шести геттерах.
- **v1.40c** — **`MainWindowViewModel.PresentationLayout`**: несколько **`PresentationLayout.*`** partial-файлов (топология, host-shell инвалидация, сохранённые bounds окон-хостов, CDS snapshot).
- **v1.40d** — **`MainWindowPresentationCapabilitiesProjection`** + тесты; геттеры **`Presentation`** делегируют цепочки capabilities/Skia/safety/LOC.
- **v1.40e** — **`MainWindowViewModel.WorkspaceNavigationMap.Refresh`**: поток обновления карты отделён от partial с привязками PFD.
- **v1.40f** — расширение **`MainWindowPresentationSurfaceProjection`** (split/Mount-style/IDE Health mount) + **`MainWindowPresentationDapProjection`**; VM без локальных помощников резолва mount-style.
- **v1.40g** — **`IMainWindowHostSurfaceInput`** + проекция host surface без типа VM; связность Cockpit → ViewModels ослаблена на границе shell/host кадра.
- **v1.40h** — MCP **`BuildAsync`** / **`RunCodeCleanupAsync`**: общий каркас **`WithIdeMcpPublishedBuildStateAsync`** (пара **`BuildStateChanged`** на шину IDE Health, ADR 0099) вместо дублирующего try/finally в **`MainWindowViewModel.IdeMcpActions.BuildTest`**.
- **v1.40i** — MCP UI automation: **`IdeMcpUiAutomationOrchestrator`** — **`TryGetRemoveBreakpointNormalizedPath`**, **`ShouldSkipToggleBreakpointInEditor`**, **`InvokeStringResultOnUiAsync`**, **`EditChatAssistantMessageOnUiAsync`**; **`MainWindowViewModel.IdeMcpActions.UiAutomation`** короче.
- **v1.40j** — палитра команд: типы строки списка **`IdeCommandPaletteRowKind`** / **`IdeCommandPaletteRowViewModel`** вынесены в **`ViewModels/IdeCommandPaletteRowViewModel.cs`**; **`MainWindowViewModel.CommandPalette.cs`** остаётся логикой фильтрации и исполнения.
- **v1.40k** — Launch / MCP: **`DebugLaunchByProfileMcpOrchestrator`** (`Features/Launch/Application`) — сценарий **`debug_launch`** по `target_path` или профилю без длинного тела в **`MainWindowViewModel.StartupProject`**; HCI: **`HybridIndexHisPresentationProjection.LampItem`** + тесты, **`MainWindowViewModel.HybridIndex`** — однострочный геттер.
- **v1.40l** — Workspace: **`SolutionLoadCrashLog`** (`Features/Workspace/Application`) — запись **`LoadSolution`** crash в **`.cascade-ide/crash-log.txt`** вне **`MainWindowViewModel.SolutionBuild`**; штамп UTC через **`InvariantCulture`**.
- **v1.41a** — Launch / F5: **`DebugLaunchForF5Orchestrator`** (`Features/Launch/Application`) — pre-resolve + MSBuild для F5 вне **`MainWindowViewModel.StartupProject`**; **`LaunchProjectPathResolver.NormalizeExistingProjectFileFullPath`** (DAL) — проверка существования стартового `.csproj` без `File.Exists` в Application-оркестраторе (CASCOPE031).
- **v1.41b** — Палитра команд: **`IdeCommandPaletteFilterOrchestrator`** / **`IdeCommandPaletteExecutionOrchestrator`** + **`CommandPaletteGoToAsyncHandle`** (`Features/Search/Application`) — фильтрация (каталог / melody / go-to + ripgrep) и исполнение выбора вне **`MainWindowViewModel.CommandPalette`**.
- **v1.41c** — CascadeChord (ADR 0060): **`CascadeChordIntentSession`** (`Features/Shell/Application`) — фаза, хвост, таймер и разбор клавиш; **`MainWindowViewModel.CascadeChord`** — привязки и туннель к сессии.
- **v1.41d** — Presentation: **`MainWindowPresentationSurfaceProjection.ComposeHostSurfaceFrame`** — сборка **`MainWindowHostSurfaceFrame`** по **`IMainWindowHostSurfaceInput`** и нормализованному UI-режиму вне **`MainWindowViewModel.Presentation`**.
- **v1.41e** — Настройки: **`ShellSettingsReactiveSideEffects`** (`Features/Settings/Application`) — длинные цепочки **внешний MCP / autonomous**, **AI mode / cloud provider**, **HCI index dir + scope** вместо тел в **`MainWindowViewModel.SettingsReactive`**.
- **v1.41f** — Relay-команды: нарезка **`MainWindowViewModel.RelayCommands`** на **`RelayCommands.Shell`** / **`Layout`** / **`Documents`** / **`UiMode`** (+ **`RelayCommands.Debug`** без изменений логики); **`ApplyMfdRegionExpanded`** / **`ApplyPfdRegionExpanded`** вместо прямых присваиваний (CASCOPE003).
- **v1.41g** — MCP executor: **`IdeMcpCommandExecutor.Handlers.Chrome`** разнесён на **`Chrome.OutputFocus`** / **`Chrome.UiVisibility`** / **`Chrome.MenuToolbar`** (регистрация хендлеров без изменения поведения).
- **v1.41h** — Главное окно: тело конструктора вынесено в **`MainWindowViewModel.ShellConstruction`**; **CASCOPE003** — в белом списке **`ShellConstruction`** (наряду с `MainWindowViewModel.cs`).
- **v1.41i** — MCP executor: **`IdeMcpCommandExecutor.Handlers.PowerDocuments`** разнесён на **`PowerDocuments.FocusPowerAgent`** (фокус, автономность, чат, Ollama) и **`PowerDocuments.Documents`** (вкладки документов).
- **v1.41j** — MCP **`IIdeMcpActions`**: **`IdeMcpActions.BuildTest`** разнесён на **`BuildTest.Build`** (диагностики, дерево решения, сборка, structured, code cleanup) и **`BuildTest.Tests`** (тесты и **`PublishIdeMcpTestRunMutation`**).
- **v1.41k** — MCP UI automation: **`IdeMcpActions.UiAutomation`** разнесён на **`UiAutomation.EditorPreview`** (редактор, брейкпоинты, превью) и **`UiAutomation.Providers`** (подтверждения, тема, провайдеры контролов и чата).
- **v1.41l** — MCP executor: **`Handlers.DapDebug`** → **`DapDebug.LaunchAttach`** / **`DapDebug.Stepping`**; **`Handlers.Editor`** → **`Editor.ToolCatalog`** (статический **`RegisterCore`**), **`FilesAndChat`**, **`StateContent`**, **`EditNavigation`**; **`Chrome.MenuToolbar`** → **`MenuToolbar.DialogsApp`**, **`ThemeLanguage`**, **`PanelsLayout`**; **`CascadeIDE.csproj`** — **`DependentUpon`** для вложенных partial.
- **v1.41m** — Workspace: **`BlankSolutionCreator`** (`Features/Workspace/Application`) — новое пустое **`.sln`** через **`dotnet new sln`**; меню **Файл → Создать новое решение…**, MCP **`create_new_solution_dialog`**, тесты **`BlankSolutionCreatorTests`**; **`MainWindowViewModel.TryCreateBlankSolutionAtPathAsync`**.
- **v1.41n** — Роли слоёв: атрибут **`[PresentationProjection]`** (`CascadeIDE.Contracts`), анализатор **`LayerRoleConsistencyAnalyzer`** (**CASCOPE032–042**); проекции в `Features/*/Application` переведены с `[ComputingUnit]` на `[PresentationProjection]`; оркестраторы палитры/F5/debug — `[ApplicationOrchestrator]`; I/O-исключения (`ExpandedMarkdownFileExportProjection`, `StartupProjectDebugInferenceProjection`) остаются на `[ComputingUnit]`.
- **v1.41o** — **`ShellSettingsPresentationProjection`** (был `ShellSettingsOrchestrator`); **CASCOPE039** не предупреждает MCP/application-фасады с `[ApplicationOrchestrator]` и делегированием в `*Service` (и разбором `JsonElement`/`GraphDocument`).
- **v1.42** — [design/feature-archetype-v1.md](design/feature-archetype-v1.md) + [design/ide-chrome-tokens-v1.md](design/ide-chrome-tokens-v1.md); глобальные стили `cascadeSection` / UiKit **`CascadeSection`**, **`CascadeStatusChip`**; ADR **0076**, **0121** → Accepted.
- **v1.43** — ADR **0120** (primary work surface Intercom/Editor) → Accepted · Implemented; IOP manifest синхронизирован.
- **v1.44** — ADR **0119** (chat slash commands) → Accepted · Implemented (фазы A, A′, B); **0121** таблица зрелости обновлена.
- **v1.45** — Strangler UI: **`StartupProjectRefreshProjection`**; MWVM **`StartupProject`** укорочен.
- **v1.46** — **`SolutionLoadUiApplyProjection`** + **`MainWindowBuildSolutionPrepProjection`**; MWVM **`SolutionBuild`** без прямого `File.Exists` для CanBuild.
- **v1.47** — **`UiModeLayoutApplyProjection`**; **`ApplyUiModeLayout`** в **`UiGitWorkspace`**.
- **v1.48** — Wave 1 strangler MWVM (текущий объём) отмечен **закрытым**; backlog зафиксирован в разделе «Wave 1 — статус».
- **v1.49** — **`SolutionWorkspaceViewModel`** + **`SolutionLoadSessionApplyProjection`** (см. backlog MWVM).
- **v1.50** — **`IdeMcpCommandExecutor`** → **`Features/IdeMcp/Execution/`**.

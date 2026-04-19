# Concept → Implementation Map v1 (CascadeIDE)

Scope: **CascadeIDE UI concepts** (Focus/Balanced/Power) mapped to current implementation in:

- `Views/MainWindow.axaml`
- `Views/MainWindow.axaml.cs` (+ `MainWindow.PresentationHostWindows.axaml.cs` — вторичные `TopLevel` PFD/MFD)
- `ViewModels/MainWindowViewModel.cs` (partial-классы)
- `Views/TaskCockpitView.axaml`, `Views/ChatPanelView.axaml`, `Views/SolutionExplorerView.axaml`, `Views/WorkspaceHealthStripView.axaml`, `Views/BottomPanelView.axaml`

Отдельно [§6](#6-мультиоконность-adr-0017--второй-toplevel-mfd): **ADR 0017** — `presentation`, хосты `PfdHostWindow` / `MfdHostWindow`, топология мониторов.

This map is intended to drive incremental alignment work with clear acceptance checks.

## Legend

- **Status**
  - ✅ implemented
  - 🟨 partial / heuristic data
  - ❌ missing

---

## 1) Global layout / docking

| Concept element | XAML control (x:Name / binding) | VM property/command | Status | Notes |
|---|---|---|---|---|
| Main grid columns (left / center / right) | `MainGrid` `ColumnDefinitions="220,4,*,4,340"` | `IsSolutionExplorerVisible`, `IsChatPanelExpanded` | ✅ | Ширина чата: рантайм + правила семьи; глобальные числа — `UiModes/workspace.toml` / `UiWorkspaceLayoutRuntimeMetrics` (см. ADR 0010). |
| Mode hotkeys | `<Window.KeyBindings>` | `SetUiModeByIndexCommand` (порядок из `UiModeCatalog.OrderedModeIds`), `CycleUiModeCommand` | ✅ | `Alt+1`…`Alt+9` — 1…9-й режим в каталоге; `Ctrl+Alt+M` — цикл. |
| Mode switch UI | Toolbar ComboBox + menu radio items | `UiMode`, `UiModeOptionsList` | ✅ | `ModeBadge`: классы `.power` / `.agentchat` / `.debug` от **`UiModeFamily`** (`UiModeFamilyEq` + параметр enum). |

---

## 2) Focus concept (`concept-generated/cascadeide-ui-concept-focus.png`)

| Concept element | XAML control | VM property/command | Status | Notes |
|---|---|---|---|---|
| Top bar: task title + status pill | `TaskCockpitView` (`Grid.Row="2"`) | `ActiveTaskTitle`, `ActiveTaskStatus`, `ActiveTaskProgress` | ✅ | `ShowTaskBar => true` — полоска видна в Focus. |
| Minimal left navigation | `SolutionExplorerBorder` | `IsSolutionExplorerVisible` | ✅ | Toggle via menu. |
| Dominant editor | `Editor` (`AvaloniaEdit`) | `EditorText`, `CurrentFilePath` | ✅ | Inline markdown preview (`InlinePreviewBorder`). |
| Agent panel (plan / next / confirmation) | `ChatPanelView` rows 0–1, 3 | `FocusPlanItems`, `NextActionSummary`, confirm commands | ✅ | Отдельные карточки в Focus; полный блок «Agent Operations» — в Balanced (`ShowAgentOperationsBlock`). |
| Bottom status pills (Build/Test/Debug/Git) | `WorkspaceHealthStripView` (`UiModeFamilyNe` + `Power`) | `WorkspaceHealthBuildText`, `WorkspaceHealthTestsText`, `WorkspaceHealthDebugText`, `WorkspaceHealthGitText` | ✅ | Компактная полоса под редактором, когда семья **не** Power (Focus/Balanced/AgentChat/Debug и т.д.). |
| Док инструментирования (События / Тесты / Отладка) | `BottomPanelView` tabs | `ShowInstrumentationTabs`, `IsInstrumentationDockVisible` | ✅ | В Focus доступны при включённом доке (меню «Док инструментирования»); детали — вкладки, сводка — полоса Workspace Health (состояние воркспейса). |
| Карточка Safety L1–L3 | `ChatPanelView` row 5 (`modeCard` / Power: `safetyLevelIsland`) | `ShowSafetyControls`, `SetSafetyL*Command` | ✅ | Focus/Balanced — компактные кнопки; Power — отдельная панель-док, объёмные L1–L3 с тенью. |

---

## 3) Balanced concept (`concept-generated/cascadeide-ui-concept-balanced.png`)

| Concept element | XAML control | VM property/command | Status | Notes |
|---|---|---|---|---|
| Quick actions | `TaskCockpitView` | `FixFailingTestsCommand`, …, `QuickActions` | ✅ | **`QuickActions`** на VM из **`Capabilities.QuickActions`** (TOML `quick_actions`, дефолты по семье; у Balanced обычно true). |
| Editor badges (Complexity / Impacted / Files) | `TaskCockpitView` | `ComplexityBadge`, `ImpactedTestsBadge`, `FilesChangedBadge` | 🟨 | **Реальные эвристики:** строки текущего файла на диске; упавшие тесты последнего `dotnet test`; число путей из `git status --short`. Подсказки на бейджах в XAML. |
| Agent operations card | `ChatPanelView` row 2 | `ShowAgentOperationsBlock` | ✅ | Balanced only. |
| Build/Test/Debug + event timeline | `WorkspaceHealthStripView` (Power cockpit) + вкладка «События» | `EventTimeline`, `IsTerminalVisible` | ✅ | В Power дубль Workspace Health на вкладке «Терминал» отключён (`WorkspaceHealthOnTerminalTab`), чтобы не сжимать консоль; лента — «События». |
| Dependency mini-map / solution graph | — | — | ❌ | Не реализовано; см. шаг 4 в «Next steps». |

---

## 4) Power concept (“cockpit”) (`concept-generated/cascadeide-ui-concept-power.png`)

| Concept element | XAML control | VM property/command | Status | Notes |
|---|---|---|---|---|
| Task bar / status cockpit | `TaskCockpitView` | `ShowTaskBar` | ✅ | Включая блок Autonomous (Power). |
| Terminal / cockpit output control (Power) | Toolbar + hint in cockpit | `TelemetryButtonText`, `ToggleTerminalCommand`, `ShowTelemetryHiddenHint` | ✅ | Не путать с каналом Workspace Health — это явный доступ к выводу (терминал). |
| Agent Trace Timeline | `ChatPanelView` | `ShowAgentTrace`, `AgentTraceSteps`, … | ✅ | |
| Safety Level + Emergency Stop | `ChatPanelView` row 5: отдельный док `safetyLevelIsland` (Power) + `modeCard` (Focus/Balanced) | L1/L2/L3 (`powerSafetyTierFace`, кольцо активного уровня); Power: **EMERGENCY STOP** в `PanelChromeHeader` (`ShowEmergencyStop`), Focus/Balanced — кнопка под L1–L3 | ✅ UI / 🟨 enforcement | Фон дока: `PowerSafetyDockBackground` ← `safety_dock_background`. Политика инструментов — отдельно. |
| Bottom Workspace Health strip (cockpit + JSON) | `WorkspaceHealthStripView` (Power) | `WorkspaceHealth*CockpitShort`, `WorkspaceSnapshotJson` | ✅ | |
| Task queue list | `SolutionExplorerView` (Power) | `PowerTaskQueueItems` | 🟨 | Заполняется при появлении очереди от агента. |
| Window title | `MainWindow` `Title` | `WindowTitle` | ✅ | |
| **Panel headers** (полоса + разделитель + ⋯) | `Views/PanelChromeHeader.axaml`, стили в `App.axaml` | `panel_chrome` в JSON темы | 🟨 | Меню по ⋯: заглушка + «Копировать заголовок»; `UppercaseTitle` для коротких меток. Glow / декоративные дуги — вне scope. |
| **Рамки рабочей области** (колонки, вертикальные сплиттеры, шов с нижней панелью, карточки `modeCard`) | `CascadeTheme.WorkspacePanelBorderBrush` в `App.axaml`; `MainWindow`, `SolutionExplorerView`, `DocumentsDockView`, `ChatPanelView`, `BottomPanelView` | `workspace_layout.border_brush` (если нет — `editor_column.border_brush`) | ✅ | Power: чуть ярче кайма колонок (`#00C8E8`) vs внутренние линии редактора. |
| **Power: Workspace Health в полосе хрома** | `WorkspaceChromeBandView`: `WorkspaceHealthStripView` на всю ширину контейнера (раньше планировался `Grid.ColumnSpan` по колонкам main grid — свойство удалено). Чат — одна строка с редактором (`ChatPanelMainGridRowSpan` = 1). | `MainWindowViewModel`, `Capabilities.WorkspaceHealthMainColumnSpan` в TOML режимов (`workspace_health_main_column_span`) | ✅ | Ширина сегментов кокпита по-прежнему из capabilities; не через отдельное свойство VM для span главной сетки. |
| **Power: острова, gutter, градиентные каймы** | `App.axaml`: `PowerEditorIslandFrameBrush`, `PowerChatIslandFrameBrush`, `PowerSolutionIslandFrameBrush`; `DocumentsDockView`, `SolutionExplorerView`, `ChatPanelView` — `Panel` + внутренний `Border` (`#…IslandInner`) с `Classes.power`, `Margin` 6–8 у `UserControl.power`; полоса хрома (Workspace Health) — `CornerRadius` 14, усиленный `BoxShadow`; низ — `BottomPanelShell` скругление сверху в Power | — | ✅ | Focus/Balanced: без градиентных рамок, скругления ~10px у колонок. |

### 4.1) Визуальный хром Power: концепт (PNG) vs текущий XAML

Речь не о отсутствии функций, а о **внешнем виде строк, выделения, плотности** — как на крупном референсе дерева: `concept-screens/power-project-explorer-tree-concept.png` (рядом см. автогенерацию `concept-generated/cascadeide-ui-concept-power.png`).

| Элемент концепта | Где в коде | Status | Notes |
|---|---|---|---|
| **Дерево решения / Project Explorer** | `SolutionExplorerView.axaml` + `App.axaml`: в Power — тёмный фон острова (`PowerSolutionTreePanelBackground`), строки `TreeViewItem` (padding, `MinHeight`, hover/selected фоны, **левый акцент** `PowerNeonBorder` 3px), иконки **20×20** (`solutionExplorerTreeIcon`), заголовки `PanelChromeHeader` с классом **`powerSolutionExplorer`** (полоса как у Workspace Health, светлый текст). Очередь задач в том же визуальном ряду. | 🟨 | Остаётся **Fluent-шаблон** `TreeViewItem` (не полная замена control theme); если акцент/фон не пробиваются в рантайме — точечный `ControlTheme` / копия шаблона. |
| **Центр: редактор** | `DocumentsDockView` + AvaloniaEdit | 🟨 | Концепт: выразительный gutter, inline diagnostics / блок предупреждений в теле. Сейчас — возможности редактора по умолчанию, без полного «кино»-хрома макета. |
| **Правая колонка: карточки трассы** | `ChatPanelView` (trace / safety) | 🟨 | Состав блоков и кнопки соответствуют идее; **плотность, неон, типографика** карточек могут отличаться от PNG. |
| **Нижняя полоса Workspace Health** | `WorkspaceHealthStripView` | 🟨 | Данные кокпита и JSON есть; **спарклайны / капс-лейблы / «глянец»** из концепта — частично или упрощённо. |
| **Заголовки панелей (⋯, полоса)** | `PanelChromeHeader` | 🟨 | Уже в таблице выше; детали glow / дуг — вне текущего scope. |

---

## 5) Concrete next alignment steps (remaining)

1. **Dependency mini-map** (Balanced): опциональная панель графа зависимостей / solution graph.
2. **Task queue feed**: наполнение `PowerTaskQueueItemViewModel` из MCP при мульти-задачной оркестрации.
3. **Бейджи глубже:** цикломатика / реально «затронутые» тесты по графу изменений — отдельный pipeline (сейчас эвристики).
4. **Живая сложность при редактировании:** при необходимости обновлять proxy сложности по `EditorText` с debounce (сейчас — при смене файла с диска).
5. **Хром дерева решения (Power):** сделано стилями на `TreeViewItem` + кисти; при необходимости — **полный** кастомный шаблон `TreeViewItem`, если тема Fluent перекрывает `Background`/`BorderThickness`.
6. **Редактор / трасса / Workspace Health:** точечное выравнивание с PNG-концептами по приоритету (см. §4.1).

---

<a id="6-мультиоконность-adr-0017--второй-toplevel-mfd"></a>

## 6) Мультиоконность (ADR 0017) — вторичные `TopLevel` (PFD / MFD)

Норматив: [ADR 0017](../adr/0017-multi-window-workspace-and-agent-surfaces.md), подробная таблица «что в коде» — [«Состояние реализации»](../adr/0017-multi-window-workspace-and-agent-surfaces.md#adr0017-implementation-status).

| Элемент | Где в коде | VM / настройки | Status | Notes |
|---|---|---|---|---|
| Строка `presentation` / `zone_screen_layout` + `[presentation_grammar]` | `CascadeIdeSettings`, merge в VM | `GetEffectivePresentationLine()`, `PresentationParse` | ✅ | Парсер: `Services/Presentation/PresentationParser.cs`, внутри экрана — `PresentationInnerEtoGrammar` (Eto.Parse). |
| Главное окно на весь экран по топологии `presentation` | `MainWindow` `Loaded` | `PresentationRequestsMainWindowMaximized` ← `PresentationLayoutAnalyzer.ShouldMaximizeMainWindowAtStartup` (P+F на первом экране — в т.ч. `(P+F+M)`; `(P)(F)(M)` на трёх экранах) | ✅ | `WindowState.Maximized`; иначе дефолт размера из XAML. |
| Второе окно зоны Mfd (полный вторичный контур) | `Views/MfdHostWindow.axaml` → `MfdShellView` | `DataContext` = `MainWindowViewModel` (тот же, что у `MainWindow`) | ✅ | ADR п. 8: не «узкий» одностраничный хост. |
| Окно зоны Pfd (Semantic Map / PFD) | `Views/PfdHostWindow.axaml` | тот же `MainWindowViewModel` | ✅ | Тройной пресет `(P)(F)(M)` — PFD на отдельном экране; см. ADR 0017. |
| Автозапуск и переключение хостов; MCP `toggle_mfd_host_window` при топологии с выносом Mfd | `MainWindow.PresentationHostWindows.axaml.cs` | MFD: `ToggleMfdHostWindowCommand` (`CanExecute` ← `PresentationRequestsMfdHostWindow`), `TryOpenMfdHostWindowOnStartup`, `OpenMfdHostWindowOnStartup`. PFD: `TogglePfdHostWindow` / `TryOpenPfdHostWindowOnStartup`, `OpenPfdHostWindowOnStartup` | ✅ | Отдельного пункта меню нет — раскладка в `settings.toml`. |
| Колонки Pfd/Mfd в главном окне при открытых хостах | `MainWindow` / layout | `SetPfdHostWindowShellOpen` / `SetMfdHostWindowShellOpen` → скрыть дубль колонки | ✅ | `AttentionLayoutSurfaceKind.MainWindowPlusPfdHostTopLevel`, `MainWindowPlusMfdHostTopLevel`, `MainWindowPlusPfdMfdHostTopLevel`. |
| Плейсмент на дисплее (общий для обоих хостов) | `PresentationHostWindowPlacement.PlaceOrRestore` / `PlaceNearMain` | `PfdHostPresentationScreenIndex` / `MfdHostPresentationScreenIndex` из разбора `presentation`; `[display]` `maximize_presentation_host_windows_on_dedicated_screens` | ✅ | Топология экранов: `PresentationMonitorTopology.OrderScreensForPresentation`. Fallback: «другой экран», не главный. При `false` — заполнение рабочей области без `Maximized`. |
| Сохранение геометрии хостов | `Closing` → `PersistPfdHostWindowBounds` / `PersistMfdHostWindowBounds` | `PfdHostWindow*` / `MfdHostWindow*` (пиксели и размер) в `settings.toml` | ✅ | Восстановление с clamp к `WorkingArea`. |
| MCP / агент | `UiLayoutSnapshot`, highlight | роли `pfd_host` / `mfd_host`, паритет подсветки | ✅ | `ide_get_ui_layout` — все top-level. |
| Параметры AI и чата в зоне Mfd | `MfdShellView` → страница `AiChatSettings` → `AiChatSettingsPanelView` | `OpenSettingsCommand` → `TryNavigateToSecondaryShellPage` при `ai_chat_settings_presentation = mfd` в `settings.toml`; иначе `SettingsWindow` | ✅ | Палитра: «Вторичный контур: Параметры AI…»; полное окно — кнопка на странице или `window`. |

**Остаётся вне этой таблицы (roadmap ADR):** продуктовая доводка UX на втором экране, полнота fallback при смене мониторов, детали подтверждений агента при двух окнах — см. [открытые вопросы 0017](../adr/0017-multi-window-workspace-and-agent-surfaces.md#adr0017-open-questions).

---

Версия карты: **2026-04-02** (термины режима: `UiModeFamily` / capabilities вместо `Is*Mode`; §4.1 без изменений по смыслу). **2026-04-11** — в русских таблицах канон терминов: **Workspace Health** / «состояние воркспейса» вместо разговорной «телеметрии» для этого канала. **2026-04-11** — [ADR 0017](../adr/0017-multi-window-workspace-and-agent-surfaces.md) **Accepted**. **2026-04-11** — добавлен **§6** (второй `TopLevel`, `MfdHostWindow`, сервисы топологии/плейсмента); устаревшая оговорка «в репозитории ещё нет второго окна» снята. **2026-04-11** — §6: страница **AI и чата** во вторичном контуре (`SecondaryShellPage.AiChatSettings`), ключ `ai_chat_settings_presentation` (`mfd` / `window`). **2026-04-17** — §6: `PfdHostWindow`, общий `PresentationHostWindows` partial и `PresentationHostWindowPlacement`; роли `pfd_host` / `mfd_host` в снимках.

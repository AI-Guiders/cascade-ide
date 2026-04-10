# Cascade IDE — макет главного окна (v1)

Описание раскладки главного окна для согласования с MCP и онбординга. Соответствует `Views/MainWindow.axaml` и текущему поведению. Режимы интерфейса (id, семья, TOML): см. **[ui-modes-overview-v1.md](ui-modes-overview-v1.md)** и [ADR 0010](../adr/0010-ui-modes-toml-configuration.md).

---

## 1. Общая структура

Окно: **DockPanel** → сверху **Menu**, ниже **Grid** (`MainGrid`) с строками и колонками.

**Размер по умолчанию:** 1000×600. Ресайз по границам окна и сплиттеры между панелями.

---

## 2. Верхняя полоса (DockPanel.Top)

- **Меню (Menu):**
  
  - **Файл:** Открыть решение…, Выход.
  - **Вид:** Обозреватель решения, Вывод сборки, Чат, Терминал, док инструментирования (вкладки «События / Тесты / Отладка» при включённом доке — во всех режимах, в т.ч. Focus); Режим интерфейса (Focus / Balanced / Power / Agent Chat / Debug — радио); Тема (Светлая, Тёмная, Как Cursor, Открыть файл темы…); Превью в отдельном окне.
  - **Настройки:** Параметры AI и чата…
  - **Справка:** О программе.

- **Баннер MCP (опционально):** если IDE запущена как MCP-сервер — полоска под меню: «Управляется агентом (MCP). Не закрывайте окно — подключение будет потеряно.»

---

## 3. MainGrid — сетка

**Строки (RowDefinitions):** `Auto, Auto, Auto, *, Auto, 4, Auto`

| Row | Содержимое |
| --- | --- |
| 0   | Баннер MCP (при `IsMcpServerMode`) |
| 1   | Toolbar |
| 2   | Task cockpit (`TaskCockpitView`): задача, прогресс, Quick Actions (Balanced), бейджи, блок Autonomous (Power); `ShowTaskBar` всегда true |
| 3   | **Основная область** — Solution Explorer, редактор (Dock), чат |
| 4   | Полоса Workspace Health (`WorkspaceHealthStripView`): Build/Test/Debug/Git в Focus/Balanced; кокпит Power в Power |
| 5–6 | Нижняя панель (`BottomPanelView`, `RowSpan=2`): сплиттер + вкладки Terminal / Build / Git / События / Тесты / Отладка |

**Колонки (ColumnDefinitions):** `220, 4, *, 4, 340`

| Column | Содержимое                                                                                                                                                                                      |
| ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 0      | **Solution Explorer** (ширина 220): дерево «Решение»; в **Power** под деревом — **очередь задач** (`PowerTaskQueueItems`).                                                 |
| 1      | GridSplitter (4px)                                                                                                                                                                              |
| 2      | **Колонка редактора** — вкладки документов, редактор (AvaloniaEdit), опционально 2-я и 3-я группы (2-up, 3-up), вывод сборки, панель отладки (стек + переменные). Контроль: `EditorColumnGrid`. |
| 3      | GridSplitter (4px)                                                                                                                                                                              |
| 4      | **Чат** — правая панель 340px. Agent Operations, Agent Trace, Safety Level (по режиму); список сообщений; поле ввода и кнопка «Отправить». Контроль: `ChatPanel`.                               |

---

## 4. Колонка редактора (детально)

Внутри — **Grid** по строкам:

- **Вкладки группы 1** (горизонтальный скролл) + кнопка «Reopen closed».
- **Путь текущего файла** (TextBlock).
- **Область редактора:** 1–3 группы (Group1/2/3) со сплиттерами; в группе 1 — TextEditor + опционально Inline Markdown Preview.
- **GridSplitter** (вывод сборки).
- **Вывод сборки** — кнопка «Скрыть вывод», TextBox с логом. Контроль видимости: `IsBuildOutputVisible`.
- **GridSplitter** (панель отладки).
- **Панель отладки** — «Стек вызовов» и «Переменные» (списки). Данные: `InstrumentationPanel`; видимость смысловая — `InstrumentationPanel.IsDebugPanelVisible`.

Имена для MCP: вкладки — `Group1Tabs`, `Group2Tabs`, `Group3Tabs`; редактор — `Editor`; превью — `InlinePreviewBorder`; вывод сборки и отладка — по привязкам в layout.

---

## 5. Чат (правая панель)

- Сверху вниз (при включённых блоках): Agent Operations, Agent Trace Timeline, Safety Level Control.
- Сплиттер и область сообщений (ItemsControl `ChatMessages`).
- Кнопка «▶ Чат» для разворота свёрнутой панели.
- Поле ввода: `ChatInputBox`; кнопка «Отправить».

Ширина панели по умолчанию 340px; меняется сплиттером между колонкой редактора и чатом. `ide_set_panel_size(panel: "chat", width: …)`.

---

## 6. Нижняя панель (`BottomPanelView`)

**TabControl** (индексы `BottomPanelTabIndex`): при необходимости показываются только вкладки с `IsVisible=true`.

| Индекс | Вкладка | Видимость | Содержимое |
| ------ | ------- | --------- | ----------- |
| 0 | Terminal | `IsTerminalVisible` | Опционально Telemetry (Power), вывод, `TerminalInputBox`. |
| 1 | Build output | `IsBuildOutputVisible` | Текст `BuildOutput`. |
| 2 | Git | `IsGitPanelVisible` | Ветка, корень git vs каталог решения, `git status --short`, diff, submodule update/sync, открытие `.sln` в submodule, `git submodule status`, коммит/push (меню **Вид → Git**). |
| 3 | События | `ShowInstrumentationTabs` | Лента `EventTimeline` (Balanced/Power, не Focus). |
| 4 | Тесты | `ShowInstrumentationTabs` | Накопленный лог `TestResultsOutput` после `dotnet test`. |
| 5 | Отладка | `ShowInstrumentationTabs` | `DebugStackFrames`, `DebugVariables`. |

`ShowInstrumentationTabs` = Balanced или Power, не Focus, и `IsInstrumentationDockVisible` (меню **Вид → Док инструментирования**).  
`IsBottomPanelVisible` = терминал или сборка или `ShowInstrumentationTabs` или **Git** (`IsGitPanelVisible`).

Высота — сплиттер (строка 4). `ide_set_panel_size(panel: "terminal", height: …)`.

**Power (семья `Power`):** на корне `BottomPanelView` — `Classes.power` через привязку к **`UiModeFamily`** и конвертер **`UiModeFamilyEq`** с параметром `Power` (в `MainWindow` у контрола `DataContext` — `MainWindowViewModel`, чтобы биндинг не терялся). У `Border#BottomPanelShell` в Power — скругление верхних углов и усиленная тень (`views|BottomPanelView.power` в стилях). Сборка/тесты/отладка/Git — в `WorkspaceHealthStripView` под редактором (канал **Workspace Health**); дубль на вкладке «Терминал» не показывается (`WorkspaceHealthOnTerminalTab`), при входе в Power выбирается вкладка «Терминал» (`BottomPanelTabIndex = 0`).

---

## 7. Режимы интерфейса (встроенные пресеты)

Шорткаты по умолчанию: Alt+1…3 циклируют Focus / Balanced / Power; полный список id — в комбо и в меню (в т.ч. **Agent Chat**, **Debug**). Детали оси семьи и данных TOML — **[ui-modes-overview-v1.md](ui-modes-overview-v1.md)**.

| Режим (id)   | Хоткей (часть цикла) | Особенности                                                                                                                                      |
| ------------ | -------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Focus**    | Alt+1                | Правый столбец: **План** (чеклист), **Следующий шаг**, **Подтверждение** (Подтвердить/Отменить + объяснение/стоп); тулбар: Запуск (сборка), Контрольная точка, Откат; полоска Workspace Health (build/tests/debug/git) видна. Блок «Agent Operations» скрыт (есть в Balanced/Power). |
| **Balanced** | Alt+2                | Дерево, 2 группы редактора, чат; нижняя панель: **терминал и журнал сборки включены по умолчанию** (меню «Вид» можно снять), плюс вкладки **События / Тесты / Отладка** при включённом доке инструментирования. |
| **Power**    | Alt+3                | Заголовок окна «Power Mode [Autonomous Agent Cockpit]»; task bar, Quick Actions, бейджи; слева очередь задач под деревом; справа **Agent Trace** (таймстемпы, объяснить/откат по шагу), **Safety**; полоса Workspace Health + **снимок workspace (JSON)**. |
| **AgentChat**| —                    | Акцент на чате агента; отдельные метрики ширины чата и оформление бейджа режима (см. `workspace.toml` / темы). |
| **Debug**    | —                    | Инструментирование и гипотезы; полоса задачи и заголовок окна под отладку (см. шипнутый `UiModes/Debug.toml`). |

Переключение: меню «Вид → Режим интерфейса» или комбобокс в тулбаре; бейдж режима (`ModeBadge`) подсвечивает классами `.power` / `.agentchat` / `.debug` в зависимости от **`UiModeFamily`**.

---

## 8. Ключевые контролы для MCP

| Контроль              | Имя / примечание                                 |
| --------------------- | ------------------------------------------------ |
| Дерево решения        | `SolutionExplorerBorder`                         |
| Колонка редактора     | `EditorColumnGrid`                               |
| Редактор кода         | `Editor` (AvaloniaEdit)                          |
| Вкладки документов    | `Group1Tabs`, `Group2Tabs`, `Group3Tabs`         |
| Чат                   | `ChatPanel`                                      |
| Поле ввода чата       | `ChatInputBox`                                   |
| Терминал (ввод)       | `TerminalInputBox`                               |
| Подсветка под агентом | `AgentHighlightOverlay` на `AgentHighlightLayer` |

Панели для `ide_set_panel_size`: `solution_explorer`, `chat`, `build_output`, `terminal`.

---

## 9. Оверлей подсветки

Слой `AgentHighlightLayer` (Canvas, ZIndex 1000) поверх всего грида; внутри `AgentHighlightOverlay` — рамка вокруг контрола, на который указывает агент (`ide_highlight_control`). IsHitTestVisible=false, чтобы не перехватывать клики.

---

## 10. Концепт Power: визуал ≠ код (острова уже есть, хром списков — нет)

Раскладка и **рамки островов** (градиенты, скругления, тени) в Power уже близки к `concept-generated/cascadeide-ui-concept-power.png`. **Внутри колонок** многие списки и редактор всё ещё на **дефолтном стиле Avalonia Fluent**, тогда как на детальных PNG (например **`docs/ux/concept-screens/power-project-explorer-tree-concept.png`**) видны:

- дерево: **своя** строка выделения (часто **вертикальный cyan/teal accent** слева), больше **padding** по вертикали, другой заголовок панели;
- редактор / трасса / Workspace Health: иные акценты и плотность, чем в текущих `TreeView` / AvaloniaEdit / `WorkspaceHealthStripView`.

Полная сводка по зонам: **`concept-to-implementation-map-v1.md` §4.1**. План доработок — тот же файл, раздел **5** (пп. 5–6).

---

*Версия: 1.1. Соответствует MainWindow.axaml по состоянию на 2026-04. При изменении разметки обновить этот документ.*
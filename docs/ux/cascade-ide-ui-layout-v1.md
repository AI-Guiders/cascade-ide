# Cascade IDE — макет главного окна (v1)

Описание раскладки главного окна для согласования с MCP и онбординга. Соответствует `Views/MainWindow.axaml` и текущему поведению.

---

## 1. Общая структура

Окно: **DockPanel** → сверху **Menu**, ниже **Grid** (`MainGrid`) с строками и колонками.

**Размер по умолчанию:** 1000×600. Ресайз по границам окна и сплиттеры между панелями.

---

## 2. Верхняя полоса (DockPanel.Top)

- **Меню (Menu):**
  
  - **Файл:** Открыть решение…, Выход.
  - **Вид:** Обозреватель решения, Вывод сборки, Чат, Терминал, док инструментирования (Balanced/Power: события, тесты, отладка); Режим интерфейса (Focus / Balanced / Power — радио); Тема (Светлая, Тёмная, Как Cursor, Открыть файл темы…); Превью в отдельном окне.
  - **Настройки:** Параметры AI и чата…
  - **Справка:** О программе.

- **Баннер MCP (опционально):** если IDE запущена как MCP-сервер — полоска под меню: «Управляется агентом (MCP). Не закрывайте окно — подключение будет потеряно.»

---

## 3. MainGrid — сетка

**Строки (RowDefinitions):** `Auto, Auto, Auto, *, 4, Auto`

| Row | Содержимое                                                                                                              |
| --- | ----------------------------------------------------------------------------------------------------------------------- |
| 0   | Баннер MCP (при `IsMcpServerMode`)                                                                                      |
| 1   | Toolbar (горизонтальный скролл: кнопки, комбобокс режима, бейдж режима, путь к решению, статус Ollama)                  |
| 2   | Task bar / status cockpit (только в Power/Balanced при `ShowTaskBar`: активная задача, прогресс, Quick Actions, бейджи) |
| 3   | **Основная область** — колонки с панелями (см. ниже)                                                                    |
| 4   | GridSplitter 4px между основной областью и терминалом                                                                   |
| 5   | Панель терминала (скрываемая)                                                                                           |

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

---

## 7. Режимы интерфейса (Focus / Balanced / Power)

| Режим        | Хоткей | Особенности                                                                                                                                      |
| ------------ | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Focus**    | Alt+1  | Правый столбец: **План** (чеклист), **Следующий шаг**, **Подтверждение** (Подтвердить/Отменить + объяснение/стоп); тулбар: Запуск (сборка), Контрольная точка, Откат; полоска телеметрии (build/tests/debug/git) видна. Блок «Agent Operations» скрыт (есть в Balanced/Power). |
| **Balanced** | Alt+2  | Дерево, 2 группы редактора, чат; нижняя док-панель с вкладками **События / Тесты / Отладка** (плюс терминал и вывод сборки по флажкам).                                                                          |
| **Power**    | Alt+3  | Заголовок окна «Power Mode [Autonomous Agent Cockpit]»; task bar, Quick Actions, бейджи; слева очередь задач под деревом; справа **Agent Trace** (таймстемпы, объяснить/откат по шагу), **Safety**; полоса телеметрии + **снимок workspace (JSON)**. |

Переключение: меню «Вид → Режим интерфейса» или комбобокс в тулбаре; бейдж режима рядом.

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

*Версия: 1. Соответствует MainWindow.axaml по состоянию на 2026-03. При изменении разметки обновить этот документ.*
w.axaml по состоянию на 2026-03. При изменении разметки обновить этот документ.*
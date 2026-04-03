# Протокол MCP: агент ↔ IDE

Идея зафиксирована в каноне заметок (`knowledge/work/projects/current-projects/cascade-ide/README.md` в репозитории **agent-notes**): IDE — тонкий клиент, которым управляет агент через MCP. Агент (в Cursor или во встроенном чате) вызывает инструменты MCP; IDE отображает результат и выполняет действия.

## Роль сторон

- **Агент** — клиент MCP (Cursor, встроенный чат с моделью с tool calling и т.п.).
- **CascadeIDE** — MCP-сервер: предоставляет инструменты для управления редактором, брейкпоинтами, превью, подтверждениями.

**Принцип по отладке:** состояние брейкпоинтов и останова, которое видит агент через тулы, должно в перспективе совпадать с тем, что видит человек в UI (единый слой). Подробнее: [debug-human-agent-parity-v1.md](debug-human-agent-parity-v1.md).

Тулы-действия (ide_set_ui_theme, ide_click_control, ide_set_control_text и т.д.) при успехе возвращают строку `OK`, при ошибке — текст причины («Control not found», «Invalid JSON», «Missing argument: …» и т.п.). В ответе вызова при ошибке выставляется `IsError: true`, чтобы агент однозначно видел сбой и мог по сообщению скорректировать запрос.

## Транспорт

- **stdio**: агент (или хост вроде Cursor) запускает CascadeIDE с аргументом `--mcp-stdio`. Обмен идёт по stdin/stdout процесса IDE. Так Cursor может добавить CascadeIDE как MCP-сервер в настройках и вызывать тулы от имени агента.

## Инструменты IDE (tools)

| Имя | Описание | Аргументы |
|-----|----------|-----------|
| `ide_open_file` | Открыть файл в редакторе | `path` — полный путь к файлу |
| `ide_load_solution` | Загрузить решение (.sln / .slnx), обновить дерево проектов | `path` — полный путь к решению |
| `ide_select` | Выделить диапазон в редакторе | `file_path`, `start_line`, `start_column`, `end_line`, `end_column` (1-based) |
| `ide_get_editor_state` | Состояние редактора (файл, каретка, выделение) | —; возвращает JSON |
| `ide_get_open_document_text` | Полный текст любой **открытой** вкладки из модели документа (не только активной) | опционально `file_path` (иначе текущий), `max_chars` для обрезки; JSON: `file_path`, `length`, `truncated`, `is_dirty`, `text` или `error` |
| `ide_apply_edit` | Применить правку в открытом файле | `file_path`, `start_line`, `start_column`, `end_line`, `end_column`, `new_text` (1-based) |
| `ide_go_to_position` | Перейти на позицию (и опционально выделить) | `file_path`, `line`, `column`; опционально `end_line`, `end_column` |
| `ide_get_solution_info` | Информация о решении и открытом файле | —; возвращает JSON (solution_path, current_file_path, project_paths) |
| `ide_get_workspace_state` | Единая сводка состояния IDE: solution/current file/selection/debug/build output/diagnostics | —; возвращает JSON |
| `ide_build` | Запустить сборку решения (dotnet build). **Структурированный результат:** JSON: success, exit_code, errors[] (file, line, column?, code?, message), warnings[], raw_output (обрезано). Агент получает ошибки без парсинга лога. | —; возвращает JSON |
| `ide_get_build_output` | Текст панели «Вывод сборки» и цвета (background, foreground) | —; возвращает JSON: text, theme |
| `ide_run_tests` | Запустить тесты решения (dotnet test; при необходимости выполняет сборку). **Структурированный результат:** JSON: success, total, passed, failed, skipped, failed_tests[] (name, message?, duration_ms?). Агент получает упавшие тесты без парсинга лога. | —; возвращает JSON |
| `ide_run_affected_tests` | Запустить затронутые тесты по `changed_paths` (фильтр `FullyQualifiedName~...`). Если токены не извлечены — fallback на полный прогон. Возвращает JSON: `success`, `total`, `passed`, `failed`, `skipped`, `failed_tests[]`, `mode`, `filter`, `tokens`. | опционально `changed_paths` (массив путей) |
| `ide_run_code_cleanup` | Запустить code cleanup через `dotnet format` для текущего решения. Возвращает JSON: `success`, `exit_code`, `raw_output` (обрезано). | опционально `include_path` для точечной чистки через `--include` |
| `ide_get_code_metrics` | Метрики кода (LOC, class_count, method_count, cyclomatic complexity) для `current_file/file/path/solution` | опционально `scope`, `path`; возвращает JSON |
| `ide_git_status` | Git status в каталоге решения/workspace | —; возвращает JSON: success, exit_code, output |
| `ide_git_diff` | Git diff в каталоге решения/workspace | опционально `path`, `staged`; возвращает JSON |
| `ide_git_commit` | Git commit в каталоге решения/workspace | `message`, опционально `paths`; возвращает JSON |
| `ide_git_push` | Git push в каталоге решения/workspace | опционально `remote`, `branch`; возвращает JSON |
| `ide_focus_editor` | Передать фокус в редактор | — |
| `ide_get_ui_theme` | Параметры темы UI + глубокий снимок (resolved-тема, окно, регионы, **dock_open_documents** — все вкладки из VM + `model_text_preview`, **dock_text_editors** — только смонтированные TextEditor) | —; возвращает JSON |
| `ide_set_ui_theme` | Применить тему UI на лету (JSON в формате get_ui_theme) | `theme` — JSON-строка |
| `ide_get_ui_layout` | Дерево элементов UI: тип, имя, видимость, границы (x,y,w,h), контент, дети | —; возвращает JSON |
| `ide_get_colors_under_cursor` | Цвета под курсором: background, foreground и effective_background, effective_foreground (как на экране) | —; возвращает JSON |
| `ide_get_control_appearance` | Снимок любого контрола: содержимое, фон, цвет текста, границы, шрифт, рамка. Без аргументов — под курсором; с `name` — по имени из layout | опционально `name`; возвращает JSON |
| `ide_set_control_layout` | Изменить положение и видимость контрола (margin, grid_row/column, canvas_left/top, dock, **visible** — true/false). После выбора layout — сохранить в БД/настройки и восстанавливать при запуске | `name`, `layout` (JSON) |
| `ide_set_control_text` | Установить текст в контрол с вводом (TextBox по имени) | `name`, `text` |
| `ide_click_control` | Клик по кнопке: без name — элемент под курсором (Button), с name — по имени | опционально `name` |
| `ide_send_keys` | Отправить сочетание клавиш в эффективный контрол (под курсором или по name). keys — текст: Ctrl+Enter, Alt+F4 | `keys`; опционально `name` |
| `ide_set_focus` | Передать фокус на эффективный контрол (по имени или под курсором); далее клавиши идут в него | опционально `name` |
| `ide_highlight_control` | Подсветить эффективный контрол рамкой (как в Comet), чтобы пользователь видел, где агент | опционально `name` |
| `ide_set_panel_size` | Изменить размер панели: solution_explorer/chat — width (px); build_output/terminal — height (px) | `panel`; опционально `width`, `height` |
| `ide_add_control` | **(Только Debug)** Добавить контрол в конец панели: Button, TextBlock или Border. parent_name, control_type, content, опционально name | `parent_name`, `control_type`; опционально `content`, `name` |
| `ide_set_breakpoint` | Поставить брейкпоинт | `file_path`, `line` (1-based), опционально `condition` |
| `ide_remove_breakpoint` | Снять брейкпоинт | `file_path`, `line` (1-based) |
| `ide_show_preview` | Показать Markdown в отдельном окне превью (планы, заметки, отчёты) | `title`, `content` (Markdown) |
| `ide_show_editor_preview` | Показать превью **текущего файла из редактора** в отдельном окне. Контент берётся из IDE (не передаётся по MCP) — удобно для длинных .md с таблицами. | — |
| `ide_request_confirmation` | Запросить подтверждение у пользователя (модальный диалог в UI) | `message`; возвращает `ok` или `cancel` |
| `ide_write_agent_notes` | Записать заметки агента. Агент сам решает, когда, что и в каком формате (markdown, json, текст). Хранятся в каталоге решения в `.cascade-ide/agent-notes.md`. Без открытого решения — ошибка. Для непрерывности между сессиями и до суммаризации. | `content` — полное содержимое (перезаписывает файл); при успехе — `OK` |
| `ide_read_agent_notes` | Прочитать заметки агента из `.cascade-ide/agent-notes.md`. Возвращает содержимое или пустую строку. Агент восстанавливает контекст в новом чате. | —; возвращает текст файла или `""` |
| `ide_execute_command` | Унифицированный вызов IDE-команды по коду (`command_id`) с аргументами (`args`) в формате выбранного инструмента. Нужен для единой точки входа (MCP/меню/хоткеи). | `command_id`, опционально `args` (JSON object) |

**Меню, тулбар, task bar и чат через `ide_execute_command`:** те же `command_id`, что заданы в `IdeCommands.cs`.

<!-- GENERATED:IdeCommands START -->

> Этот блок сгенерирован из XML-doc в `Services/IdeCommands.cs`.

### Core

| command_id | Описание |
|-----------:|----------|
| `apply_edit` | Применить текстовую правку в открытом документе. args: file_path:string, start_line:integer, start_column:integer, end_line:integer, end_column:integer, new_text:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs","start_line":1,"start_column":1,"end_line":1,"end_column":1,"new_text":"// hi\n"}. |
| `build` | Сборка решения (структурированный результат). returns: json. |
| `build_structured` | Сборка решения (структурированный результат). То же, что `build`; выделено для совместимости/алиасов. returns: json. |
| `focus_editor` | Передать фокус в редактор (чтобы клавиши/ввод шли в него). returns: text. |
| `get_build_output` | Текст панели «Вывод сборки» + цвета оформления. returns: json. |
| `get_code_metrics` | Метрики кода (LOC/классы/методы/цикломатика). args: scope?:string, path?:string; returns: json; example: {"scope":"solution","path":"."}. |
| `get_current_file_diagnostics` | Диагностики текущего открытого .cs (ошибки/предупреждения). returns: json. |
| `get_editor_content_range` | Текст активного редактора по диапазону строк (1-based). args: start_line:integer, end_line:integer; returns: json; example: {"start_line":1,"end_line":40}. |
| `get_editor_state` | Состояние активного редактора: файл, каретка, выделение. args: max_preview_chars?:integer; returns: json; example: {"max_preview_chars":0}. |
| `get_open_document_text` | Полный текст открытого документа по пути (или текущего). Модель вкладки, не снимок темы. returns: text. |
| `get_solution_files` | Список файлов и дерево решения (Solution Explorer). returns: json. |
| `get_solution_info` | Короткая информация о текущем решении/файле/выделении в дереве. returns: json. |
| `get_workspace_state` | Единая сводка состояния IDE (solution/editor/build/diagnostics...). returns: json. |
| `git_commit` | Git commit в каталоге решения/workspace. args: message:string, paths?:string[]; returns: text; example: {"message":"chore: update","paths":["a.txt"]}. |
| `git_diff` | Git diff в каталоге решения/workspace. args: path?:string, staged?:boolean; returns: json; example: {"path":"README.md","staged":false}. |
| `git_push` | Git push в каталоге решения/workspace. args: remote?:string, branch?:string; returns: text; example: {"remote":"origin","branch":"main"}. |
| `git_status` | Git status в каталоге решения/workspace. returns: json. |
| `go_to_position` | Перейти на позицию (и опционально выделить диапазон). args: file_path:string, line:integer, column:integer, end_line?:integer, end_column?:integer; returns: text; example: {"file_path":"C:\\tmp\\a.cs","line":10,"column":1}. |
| `list_tools` | Список MCP-тулов, которые IDE публикует (name/description/inputSchema). returns: json. |
| `load_solution` | Загрузить решение (.sln/.slnx/.slnf) и обновить дерево решения. args: path:string; returns: text; example: {"path":"D:\\Experiments\\PersonalCursorFolder\\Financial\\software\\open\\cascade-ide\\CascadeIDE.slnx"}. |
| `open_file` | Открыть файл в редакторе IDE. args: path:string; returns: text; example: {"path":"C:\\tmp\\a.txt"}. |
| `remove_breakpoint` | Снять брейкпоинт. args: file_path:string, line:integer; returns: text; example: {"file_path":"C:\\tmp\\a.cs","line":42}. |
| `request_confirmation` | Запросить подтверждение у пользователя. args: message:string; returns: text; example: {"message":"Продолжить?"}. Возвращает `ok`/`cancel`. |
| `run_affected_tests` | Запустить затронутые тесты по changed_paths (или fallback на полный прогон). args: changed_paths?:string[]; returns: json; example: {"changed_paths":["a.cs","b.cs"]}. |
| `run_code_cleanup` | Запустить code cleanup (`dotnet format`). args: include_path?:string; returns: json; example: {"include_path":"src"}. |
| `run_tests` | Запустить тесты решения. returns: json. |
| `select` | Выделить диапазон в редакторе (1-based). args: file_path:string, start_line:integer, start_column:integer, end_line:integer, end_column:integer; returns: text; example: {"file_path":"C:\\tmp\\a.cs","start_line":1,"start_column":1,"end_line":1,"end_column":10}. |
| `set_breakpoint` | Поставить брейкпоинт. args: file_path:string, line:integer, condition?:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs","line":42}. |
| `set_build_output_visible` | Явно показать/скрыть журнал сборки. args: visible:boolean; returns: text; example: {"visible":true}. |
| `set_terminal_visible` | Явно показать/скрыть терминал (без переключения). args: visible:boolean; returns: text; example: {"visible":true}. |
| `set_ui_mode` | Режим UI (как меню «Вид → Режим интерфейса»). args: mode:string; returns: text; example: {"mode":"Power"}. |
| `show_editor_preview` | Показать превью текущего файла из редактора в отдельном окне (контент берётся из IDE). returns: text. |
| `show_preview` | Показать Markdown-превью в отдельном окне. args: title:string, content:string; returns: text; example: {"title":"Plan","content":"- step 1\n- step 2"}. |
| `toggle_build_output` | Как меню «Вид → Вывод сборки». returns: text. |
| `toggle_solution_explorer` | Как меню «Вид → Обозреватель решения». returns: text. |
| `toggle_terminal` | Как меню «Вид → Терминал» (переключатель). returns: text. |

### Меню «Файл» / приложение (те же RelayCommand, что в UI)

| command_id | Описание |
|-----------:|----------|
| `exit_application` | Закрыть приложение (как меню Файл → Выход). returns: none. |
| `open_solution_dialog` | Открыть диалог выбора решения (как меню Файл → Открыть решение...). returns: text. |

### Вид: панели (явная установка + переключатели)

| command_id | Описание |
|-----------:|----------|
| `set_chat_panel_expanded` | Развернуть/свернуть чат-панель. args: visible:boolean; returns: text; example: {"visible":true}. |
| `set_git_panel_visible` | Показать/скрыть панель Git (нижняя вкладка). args: visible:boolean; returns: text; example: {"visible":true}. |
| `set_instrumentation_dock_visible` | Показать/скрыть док инструментирования (Events/Tests/Debug). args: visible:boolean; returns: text; example: {"visible":true}. |
| `set_solution_explorer_visible` | Показать/скрыть обозреватель решения. args: visible:boolean; returns: text; example: {"visible":true}. |
| `toggle_chat_panel` | Переключить сворачивание чата (toggle). returns: text. |
| `toggle_git_panel` | Переключить видимость панели Git (toggle). returns: text. |
| `toggle_instrumentation_dock` | Переключить видимость дока инструментирования (toggle). returns: text. |

### Вид: режим (дублируют хоткеи Alt+1/2/3, Ctrl+Alt+M)

| command_id | Описание |
|-----------:|----------|
| `cycle_ui_mode` | Циклически переключить UI mode (hotkey). returns: text. |
| `set_balanced_mode` | Установить Balanced UI mode (hotkey). returns: text. |
| `set_focus_mode` | Установить Focus UI mode (hotkey). returns: text. |
| `set_power_mode` | Установить Power UI mode (hotkey). returns: text. |

### Вид: тема

| command_id | Описание |
|-----------:|----------|
| `apply_cursor_like_theme` | Применить тему «как Cursor». returns: text. |
| `apply_dark_theme` | Применить тёмную тему. returns: text. |
| `apply_light_theme` | Применить светлую тему. returns: text. |
| `apply_power_classic_theme` | Применить классическую Power-тему (циан). returns: text. |
| `open_theme_file_dialog` | Открыть диалог выбора файла темы. returns: text. |

### Вид: язык UI

| command_id | Описание |
|-----------:|----------|
| `reset_ui_language_to_system` | Сбросить язык UI к системному. returns: text. |
| `set_ui_language` | Установить язык UI. args: culture:string; returns: text; example: {"culture":"ru-RU"}. |

### Меню: превью, настройки, справка

| command_id | Описание |
|-----------:|----------|
| `about` | Показать диалог «О программе». returns: text. |
| `open_preview_window` | Открыть отдельное окно превью (Markdown). returns: text. |
| `open_settings` | Открыть окно настроек. returns: text. |

### Тулбар: показать панели / скрыть вывод сборки

| command_id | Описание |
|-----------:|----------|
| `hide_build_output_panel` | Скрыть панель вывода сборки (toolbar). returns: text. |
| `show_build_output_panel` | Явно показать панель вывода сборки (toolbar). returns: text. |
| `show_chat_panel` | Явно показать чат-панель (toolbar). returns: text. |
| `show_solution_explorer_panel` | Явно показать обозреватель решения (toolbar). returns: text. |
| `show_terminal_panel` | Явно показать терминал (toolbar). returns: text. |

### Тулбар: группы редакторов

| command_id | Описание |
|-----------:|----------|
| `build_solution_ui` | Кнопка «Собрать» в тулбаре: `dotnet build` в панель вывода (не structured build). returns: text. |
| `set_dual_editor_group` | Две группы редакторов (2-up). returns: text. |
| `set_single_editor_group` | Одна группа редакторов (1-up). returns: text. |
| `set_triple_editor_group` | Три группы редакторов (3-up). returns: text. |

### Focus / Power: чат и автономный режим

| command_id | Описание |
|-----------:|----------|
| `cancel_focus_step` | Отменить текущий шаг плана (Focus). returns: text. |
| `confirm_focus_step` | Подтвердить текущий шаг плана (Focus). returns: text. |
| `emergency_stop` | Экстренно остановить автономные действия/выполнение (Emergency stop). returns: text. |
| `explain_current_step` | Пояснить текущий шаг (Focus/Power). returns: text. |
| `explain_trace_step` | Шаг трассы по индексу в `AgentTraceSteps` (0 — самый старый). args: step_index:integer; returns: text; example: {"step_index":0}. |
| `fix_failing_tests` | Quick action: починить упавшие тесты. returns: text. |
| `focus_checkpoint` | Создать контрольную точку (Focus). returns: text. |
| `focus_rollback` | Откатить к последней контрольной точке (Focus). returns: text. |
| `install_ollama_model` | Скачать модель Ollama (как в настройках). args: model:string; returns: text; example: {"model":"qwen2.5-coder:7b"}. |
| `investigate_nullref` | Quick action: расследовать NullReferenceException. returns: text. |
| `pause_autonomous` | Поставить автономный режим на паузу. returns: text. |
| `prepare_commit` | Quick action: подготовить коммит (сводка/план/проверки). returns: text. |
| `refresh_workspace_snapshot` | Обновить снимок рабочего состояния (Power cockpit). returns: text. |
| `resume_autonomous` | Продолжить автономный режим после паузы. returns: text. |
| `rollback_trace_step` | Откатить состояние по шагу трассы. returns: text. |
| `send_chat` | Кнопка отправки чата; опционально `message` — записать в поле ввода перед отправкой. returns: text. |
| `set_safety_l1` | Установить Safety L1. returns: text. |
| `set_safety_l2` | Установить Safety L2. returns: text. |
| `set_safety_l3` | Установить Safety L3. returns: text. |
| `start_autonomous` | Запустить автономный режим (agent run). returns: text. |

### Документы (контекстное меню / док)

| command_id | Описание |
|-----------:|----------|
| `activate_document` | Активировать документ (переключить вкладку). args: file_path:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs"}. |
| `add_control` | Добавить контрол в UI (Debug). args: parent_name:string, control_type:string, content?:string, name?:string; returns: text; example: {"parent_name":"Root","control_type":"TextBlock","content":"Hi"}. |
| `append_agent_notes` | Добавить блок в конец заметок агента. args: content:string; returns: text; example: {"content":"\\n# Update\\n..."}. |
| `append_knowledge_file` | Добавить блок в конец knowledge-файла. args: file_path:string, content:string, canon_path?:string, save_revision?:boolean; returns: text; example: {"file_path":"META/x.md","content":"more","save_revision":true}. |
| `click_control` | Клик по кнопке (под курсором или по имени). args: name?:string; returns: text; example: {"name":"BuildButton"}. |
| `close_document` | Закрыть документ. args: file_path:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs"}. |
| `compact_hot_context` | Ужать hot-context (preview/apply). args: apply?:boolean; returns: json; example: {"apply":false}. |
| `delete_knowledge_file` | Удалить knowledge-файл. args: file_path:string, canon_path?:string; returns: text; example: {"file_path":"tmp.md"}. |
| `delete_knowledge_section` | Удалить секцию из knowledge-файла. args: file_path:string, section_id:string, canon_path?:string; returns: text; example: {"file_path":"index.md","section_id":"foo"}. |
| `extract_from_archive` | Поиск по архивной ревизии заметок с контекстом строк. args: query:string, revision_file?:string, head_limit?:integer, context_lines?:integer; returns: json; example: {"query":"ActiveProjectId","head_limit":10,"context_lines":2}. |
| `get_colors_under_cursor` | Цвета под курсором (прямые и effective). returns: json. |
| `get_control_appearance` | Снимок внешнего вида контрола (под курсором или по имени). args: name?:string; returns: json; example: {"name":"BuildButton"}. |
| `get_supported_editor_languages` | Список поддерживаемых языков подсветки редактора. returns: json. |
| `get_ui_layout` | Дерево UI-элементов (layout) с bounds/visibility/content. returns: json. |
| `get_ui_theme` | Снимок темы UI и лэйаута (включая resolved-ресурсы). returns: json. |
| `highlight_control` | Подсветить контрол рамкой (под курсором или по имени). args: name?:string; returns: text; example: {"name":"BuildButton"}. |
| `list_agent_notes_revisions` | Список ревизий заметок агента. args: limit?:integer; returns: json; example: {"limit":20}. |
| `list_knowledge_files` | Список knowledge-файлов в каталоге решения (опционально subdir). args: subdir?:string; returns: json; example: {"subdir":"work"}. |
| `memory_health` | Health-check памяти: размер hot-context и рекомендации. args: active_scope?:string; returns: json; example: {"active_scope":"current-projects"}. |
| `move_document_to_group_1` | Переместить документ в группу 1. args: file_path:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs"}. |
| `move_document_to_group_2` | Переместить документ в группу 2. args: file_path:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs"}. |
| `move_document_to_group_3` | Переместить документ в группу 3. args: file_path:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs"}. |
| `read_agent_notes` | Прочитать заметки агента из каталога решения. returns: text. |
| `read_hot_context` | Прочитать только hot-context (L0/L1) без архивного хвоста. args: active_scope?:string; returns: json; example: {"active_scope":"current-projects"}. |
| `read_knowledge_file` | Прочитать knowledge-файл из каталога решения. args: file_path:string; returns: text; example: {"file_path":"META/integrity-core.md"}. |
| `reopen_closed_document` | Переоткрыть недавно закрытый документ. returns: text. |
| `rollback_agent_notes` | Откатить заметки к ревизии (или к последней). args: revision_file?:string; returns: text; example: {"revision_file":"20260402-120000-write-acde123.md"}. |
| `route_context` | Router-first контекст пакет по запросу. args: query:string, active_scope?:string, max_sections?:integer, max_chars?:integer; returns: json; example: {"query":"CascadeIDE notes structure","max_sections":5,"max_chars":12000}. |
| `search_agent_notes` | Поиск по заметкам агента (case-insensitive) с возвратом совпадающих строк. args: query:string, head_limit?:integer; returns: json; example: {"query":"ActiveProjectId","head_limit":20}. |
| `send_keys` | Отправить хоткей в контрол. args: keys:string, name?:string; returns: text; example: {"keys":"Ctrl+S"}. |
| `set_control_layout` | Изменить раскладку/позицию контрола. args: name:string, layout:string; returns: text; example: {"name":"BuildButton","layout":"{\"x\":10,\"y\":10}"}. |
| `set_control_text` | Установить текст в контроле ввода. args: name:string, text:string; returns: text; example: {"name":"ChatInput","text":"hi"}. |
| `set_focus` | Передать фокус контролу (под курсором или по имени). args: name?:string; returns: text; example: {"name":"Editor"}. |
| `set_panel_size` | Изменить размер панели. args: panel:string, width?:integer, height?:integer; returns: text; example: {"panel":"terminal","height":300}. |
| `set_ui_theme` | Применить тему UI из JSON. args: theme:string; returns: text; example: {"theme":"{\"name\":\"MyTheme\"}"}. |
| `show_breakpoints` | Показать брейкпоинты отладчика в IDE. args: breakpoints:object[]; returns: text; example: {"breakpoints":[{"file_path":"C:\\tmp\\a.cs","line":1}]}. |
| `show_debug_position` | Показать текущую позицию отладки (файл/строка). args: file_path?:string, line?:integer; returns: text; example: {"file_path":"C:\\tmp\\a.cs","line":1}. |
| `show_debug_state` | Показать стек/переменные отладки в панели Debug. args: stack_frames?:object[], variables?:object[]; returns: text; example: {"stack_frames":[],"variables":[]}. |
| `toggle_pin_document` | Закрепить/открепить документ (pin). args: file_path:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs"}. |
| `upsert_agent_notes_section` | Вставить/обновить секцию заметок агента по section_id (маркерный блок). args: section_id:string, content:string; returns: text; example: {"section_id":"active","content":"ActiveProjectId: cascade-ide"}. |
| `upsert_knowledge_section` | Вставить/обновить секцию в knowledge-файле по section_id. args: file_path:string, section_id:string, content:string, canon_path?:string, save_revision?:boolean; returns: text; example: {"file_path":"index.md","section_id":"foo","content":"body"}. |
| `write_agent_notes` | Записать заметки агента в каталог решения. args: content:string; returns: text; example: {"content":"notes"}. |
| `write_knowledge_file` | Записать knowledge-файл в канон (полная замена). args: file_path:string, content:string, canon_path?:string, save_revision?:boolean; returns: text; example: {"file_path":"META/x.md","content":"# Hi","save_revision":true}. |
<!-- GENERATED:IdeCommands END -->

Проверка: `ide_get_workspace_state` — помимо `terminal.is_visible`, `ui_mode`, есть `panels` (видимость колонок), `safety_level`, `editor_group_count`, `agent_trace_step_count`, `is_autonomous_running`.

## Подключение из Cursor

1. **Self-contained exe (рекомендуется):** в корне репо заданы `RuntimeIdentifier=win-x64` и `SelfContained=true`. Опубликуйте и используйте junction (как у dotnet-debug-mcp, roslyn-mcp):
   - `dotnet publish -c Release -o publish` из каталога `cascade-ide`;
   - junction: `D:\cascade-ide` → каталог `publish` (полный путь к нему в репо);
   - в `mcp.json`: `command`: `D:\cascade-ide\CascadeIDE.exe`, `args`: `["--mcp-stdio"]`.
   - **Debug (для отладки MCP-обработчиков):** `dotnet publish -c Debug -o publish-debug`; junction `D:\cascade-ide-debug` → каталог `publish-debug`; в Cursor — сервер `cascade-ide-debug` с `command`: `D:\cascade-ide-debug\CascadeIDE.exe`. Включать, когда нужно отлаживать тулы IDE.

2. В настройках MCP Cursor (например, `.cursor/mcp.json` или глобальные настройки) добавьте сервер:

```json
{
  "mcpServers": {
    "cascade-ide": {
      "command": "<полный_путь_к_CascadeIDE.exe>",
      "args": ["--mcp-stdio"]
    }
  }
}
```

Вариант через `dotnet run` (из каталога решения/репо):

```json
{
  "mcpServers": {
    "cascade-ide": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "D:/path/to/cascade-ide/CascadeIDE.csproj",
        "--",
        "--mcp-stdio"
      ]
    }
  }
}
```

3. Запустите CascadeIDE с `--mcp-stdio` (или позвольте Cursor запустить его по этой команде). После подключения агент в Cursor сможет вызывать `ide_open_file`, `ide_set_breakpoint` и др.

## Запуск «сначала IDE, потом MCP»

Сейчас Cursor при включении сервера сам запускает процесс (`CascadeIDE.exe --mcp-stdio`). Вариант «сначала запускаешь IDE сама, потом включаешь MCP в Cursor» без доработок не поддерживается. **Доработки** могли бы быть:

- **Со стороны Cursor (клиент):** поддержка подключения к уже запущенному MCP-серверу (attach), а не только запуск процесса. Тогда пользователь стартует IDE вручную, Cursor подключается к существующему процессу (например по сокету или именованному каналу).
- **Со стороны Cascade IDE (сервер):** второй транспорт — не только stdio, но и, например, TCP/socket. IDE при старте открывает порт и ждёт подключения; в настройках Cursor указывается URL вместо command. Тогда «сначала IDE, потом включить MCP» работало бы без изменений в Cursor.

Пока достаточно сценария «Cursor запускает IDE»; при работе в этом режиме в окне IDE отображается подсказка: «Управляется агентом (MCP). Не закрывайте окно — подключение будет потеряно.»

## Пример тёмной темы (ide_set_ui_theme)

Валидный JSON для переключения на тёмную тему (передать в `theme`):

```json
{"main_window":{"background":"#1E1E1E"},"menu":{"background":"#252526","foreground":"#CCCCCC"},"button":{"background":"#3C3C3C","foreground":"#CCCCCC","border_brush":"#555","hover_background":"#505050","disabled_background":"#2D2D2D","disabled_foreground":"#858585"},"toolbar":{"background":"#2D2D2D"},"toolbar_text":{"foreground":"#CCCCCC","error_foreground":"#F48771"},"editor":{"background":"#1E1E1E","foreground":"#D4D4D4"},"editor_column":{"border_brush":"#3F3F46","background":"#1E1E1E","current_file_foreground":"#9D9D9D"},"workspace_layout":{"border_brush":"#3F3F46"},"markdown_preview_panel":{"background":"#252526","border_brush":"#3F3F46"},"solution_explorer":{"border_brush":"#3F3F46","header_foreground":"#CCCCCC"},"build_output":{"background":"#252526","foreground":"#D4D4D4","border_brush":"#3F3F46"},"chat_panel":{"background":"#252526","label_foreground":"#CCCCCC","message_bubble_background":"#2D2D2D","message_content_foreground":"#D4D4D4","send_button_background":"#0E639C","send_button_foreground":"#FFF"},"terminal":{"background":"#1E1E1E","foreground":"#CCCCCC","input_background":"#2D2D2D"},"mcp_banner":{"background":"#094771","foreground":"#6CB6F0"},"preview_window":{"background":"#252526"}}
```

Секция **`workspace_layout.border_brush`** задаёт рамки колонок (решение / редактор / чат), вертикальные сплиттеры и шов с нижней панелью (`CascadeTheme.WorkspacePanelBorderBrush`). Если секции нет, используется `editor_column.border_brush`.

Тул возвращает «OK» при успехе или текст ошибки при невалидном JSON (например, `Invalid JSON: ...`) — агент видит причину в ответе вызова.

## Связка с другими MCP

Агент может одновременно использовать:

- **CascadeIDE** (MCP-сервер) — управление IDE: открытие файлов, брейкпоинты, превью.
- **dotnet-debug-mcp** — отладка .NET (launch, breakpoints, step, variables).
- **roslyn-mcp** — рефакторинг и анализ C# (find usages, rename, code actions).

Один и тот же workspace: агент в Cursor вызывает CascadeIDE для отображения и dotnet-debug/roslyn для отладки и кода.

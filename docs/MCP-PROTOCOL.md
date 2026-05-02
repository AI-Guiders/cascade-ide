# Протокол MCP: агент ↔ IDE

Идея зафиксирована в каноне заметок (`knowledge/work/projects/door-to-singularity/cascade-ide/README.md` в репозитории **agent-notes**): IDE — тонкий клиент, которым управляет агент через MCP. Агент (в Cursor или во встроенном чате) вызывает инструменты MCP; IDE отображает результат и выполняет действия.

## Роль сторон

- **Агент** — клиент MCP (Cursor, встроенный чат с моделью с tool calling и т.п.).
- **CascadeIDE** — MCP-сервер: предоставляет инструменты для управления редактором, брейкпоинтами, превью, подтверждениями.

**Принцип по отладке:** состояние брейкпоинтов и останова, которое видит агент через тулы, должно в перспективе совпадать с тем, что видит человек в UI (единый слой). Подробнее: [debug-human-agent-parity-v1.md](debug-human-agent-parity-v1.md).

Тулы-действия (ide_set_ui_theme, ide_click_control, ide_set_control_text и т.д.) при успехе возвращают строку `OK`, при ошибке — текст причины («Control not found», «Invalid JSON», «Missing argument: …» и т.п.). В ответе вызова при ошибке выставляется `IsError: true`, чтобы агент однозначно видел сбой и мог по сообщению скорректировать запрос.

## Транспорт

- **stdio**: агент или **ProcessHost** (внешний процесс вроде Cursor, поднимающий процесс IDE) запускает CascadeIDE с аргументом `--mcp-stdio`. Обмен идёт по stdin/stdout процесса IDE. Так Cursor может добавить CascadeIDE как MCP-сервер в настройках и вызывать тулы от имени агента. **ProcessHost** здесь — про жизненный цикл процесса и транспорт, не про контролы кокпита (`EicasAlertsBarView` и т.д.); словарь: [ADR 0021 §1.1](adr/0021-pfd-mfd-cockpit-attention-model.md#glossary-channel-presentation).

### CLI контракта без MCP (ADR 0052)

Без GUI и без stdio можно вывести **тот же JSON**, что вернул бы соответствующий тул: `CascadeIDE.exe --agent-contract [--workspace <dir>] <command>`. Команды без workspace: `get_ui_modes_diagnostics`, `get_supported_editor_languages`, `get_solution_info` (краткая сводка решения, паритет с `ide_get_solution_info`), `get_cockpit_surface` (только CDS — тот же объект, что поле `cockpit_surface` в ответе ниже), `get_ide_state` (полная сводка, паритет с `ide_get_ide_state`). Read-only git (тот же JSON, что `ide_git_*`): `git_status`, `git_diff`, `git_log`, `git_branch` (list), `git_show` — для них `--workspace` задаёт корень репозитория (по умолчанию текущий каталог). В CI: см. `.gitlab-ci.yml` (Windows runner), плюс привычный **`dotnet script`** — [`docs/samples/agent-contract-ci.csx`](samples/agent-contract-ci.csx); вариант на PowerShell: [`docs/samples/agent-contract-ci.ps1`](samples/agent-contract-ci.ps1). Подробности и снапшот-тесты: [ADR 0052](adr/0052-agent-contract-cli-and-snapshot-tests.md).

### Видимость MCP для агента (на будущее: свои MCP в IDE)

У внешних хостов (например Cursor) встречается рассинхрон: сервер **подключён** (список тулов в UI есть), а **конкретный чат с ассистентом** не получает те же MCP в tool calling. Это не обязательно ошибка транспорта — разные контуры: глобальные настройки, воркспейс, режим Agent/Composer.

**При проработке линии «пользователь подключает свои MCP в Cascade IDE»** имеет смысл заложить явно:

1. **Семантика состояния** — отдельно: «транспорт жив, `tools/list` отвечает» и «выбранный агент/сессия может вызывать эти серверы».
2. **UI** — не опираться только на индикатор «подключено»; показывать, для **какого** контура (встроенный агент, чат, автономный режим) сервер разрешён.
3. **Диагностика** — возможность увидеть тот же набор тулов, что увидит агент (тот же путь кода, что и при вызове), чтобы не гадать.

**Паритет восстановления транспорта** (человек перезапускает MCP в хосте vs что может агент) — отдельная ось от паритета отладки; границы хоста и направление решений: [ADR 0043](adr/0043-mcp-transport-recovery-human-agent-parity.md).

### Внешние MCP в IDE (`settings.toml`): inline JSON или отдельный файл

В `%LocalAppData%\CascadeIDE\settings.toml` в секции **`[mcp]`** задаётся inline JSON массива серверов (`external_servers_json`). В той же секции опционально **`external_servers_json_path`** — путь к **JSON-файлу** того же формата (массив объектов с `name`, `command`, `arguments`, `enabled`, опционально `toolPrefix`). Если путь непустой и файл **успешно читается**, его содержимое **подставляется вместо** inline JSON для `McpClientService` и Cursor ACP; иначе используется inline. Относительные пути считаются от каталога настроек (`…\CascadeIDE\`). Пример: [samples/settings.toml](samples/settings.toml). (Ранний вариант с отдельной секцией `[mcp_external_file]` заменён: ключ перенесён в `[mcp]`.)

### Переопубликация для Cursor (`mcp.json`)

Чтобы процесс в Cursor указывал на свежий бинарник **без пробелов в пути**, в корне каталога **CascadeIDE** (рядом с `CascadeIDE.csproj`; в монорепе обычно `Financial/software/open/cascade-ide/`):

- **`scripts/deploy/publish-debug.ps1`** — `dotnet publish` **Debug**, self-contained **win-x64**, вывод в `publish-debug/`, затем зеркалирование в **`D:\cascade-ide-debug`** по умолчанию (удобная цель для `command` в `mcp.json`).
- **`scripts/deploy/publish-release.ps1`** — то же для **Release** и цели **`D:\cascade-ide`** по умолчанию.

Запуск из каталога проекта CascadeIDE: `.\scripts\deploy\publish-debug.ps1`. Опции **`-SkipDocGen`** и **`-Target`** — в комментариях у скриптов; пример фрагмента конфигурации — [mcp-cursor-example.json](mcp-cursor-example.json).

## Инструменты IDE (tools)

| Имя | Описание | Аргументы |
|-----|----------|-----------|
| `ide_open_file` | Открыть файл в редакторе | `path` — полный путь к файлу |
| `ide_load_solution` | Загрузить workspace: решение (.sln/.slnx/.slnf), проект (.csproj/.fsproj) или каталог; обновить обозреватель | `path` — полный путь |
| `ide_select` | Выделить диапазон в редакторе | `file_path`, `start_line`, `start_column`, `end_line`, `end_column` (1-based) |
| `ide_get_editor_state` | Состояние редактора (файл, каретка, выделение) | —; возвращает JSON |
| `ide_get_open_document_text` | Полный текст любой **открытой** вкладки из модели документа (не только активной) | опционально `file_path` (иначе текущий), `max_chars` для обрезки; JSON: `file_path`, `length`, `truncated`, `is_dirty`, `text` или `error` |
| `ide_apply_edit` | Применить правку в открытом файле | `file_path`, `start_line`, `start_column`, `end_line`, `end_column`, `new_text` (1-based) |
| `ide_go_to_position` | Перейти на позицию (и опционально выделить) | `file_path`, `line`, `column`; опционально `end_line`, `end_column` |
| `ide_get_solution_info` | Информация о решении и открытом файле | —; возвращает JSON (solution_path, current_file_path, project_paths) |
| `ide_get_ide_state` | Единая сводка состояния IDE: solution/current file/selection/debug/build output/diagnostics; **`cockpit_surface`** — CDS-снимок кабины (тот же `CockpitSurfaceState`, что `BuildCockpitSurfaceSnapshot` / Skia) | —; возвращает JSON |
| `ide_get_ui_modes_diagnostics` | Диагностика загрузки UI-режимов: путь к `UiModes`, наличие `index.toml`/`Flight.toml`, источник бандла (TOML vs встроенный fallback), `ordered_mode_ids`, признак Flight в меню | —; возвращает JSON |
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
| `ide_get_ui_theme` | Параметры темы UI + глубокий снимок (resolved-тема, окно, регионы, **dock_open_documents**, **dock_text_editors**, **top_levels** — все открытые `Window`: `role` main/mfd_host/other, позиция, размер, активность) | —; возвращает JSON |
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
| `ide_set_panel_size` | Изменить размер панели: pfd_region/mfd_region — width (px); build_output/terminal — height (px) | `panel`; опционально `width`, `height` |
| `ide_add_control` | **(Только Debug)** Добавить контрол в конец панели: Button, TextBlock или Border. parent_name, control_type, content, опционально name | `parent_name`, `control_type`; опционально `content`, `name` |
| `ide_set_breakpoint` | Поставить брейкпоинт: при необходимости загрузить найденное решение над файлом, записать точку для отладки MCP, открыть файл и перейти к строке | `file_path`, `line` (1-based), опционально `condition` |
| `ide_remove_breakpoint` | Снять брейкпоинт | `file_path`, `line` (1-based) |
| `ide_show_preview` | Показать Markdown в отдельном окне превью (планы, заметки, отчёты) | `title`, `content` (Markdown) |
| `ide_show_editor_preview` | Показать превью **текущего файла из редактора** в отдельном окне. Контент берётся из IDE (не передаётся по MCP) — удобно для длинных .md с таблицами. | — |
| `ide_request_confirmation` | Запросить подтверждение у пользователя (модальный диалог в UI) | `message`; возвращает `ok` или `cancel` |
| `ide_write_agent_notes` | Записать заметки агента. Агент сам решает, когда, что и в каком формате (markdown, json, текст). Хранятся в каталоге решения в `.cascade-ide/agent-notes.md`. Без открытого решения — ошибка. Для непрерывности между сессиями и до суммаризации. | `content` — полное содержимое (перезаписывает файл); при успехе — `OK` |
| `ide_read_agent_notes` | Прочитать заметки агента из `.cascade-ide/agent-notes.md`. Возвращает содержимое или пустую строку. Агент восстанавливает контекст в новом чате. | —; возвращает текст файла или `""` |
| `ide_execute_command` | Унифицированный вызов IDE-команды по коду (`command_id`) с аргументами (`args`) в формате выбранного инструмента. Нужен для единой точки входа (MCP/меню/хоткеи). | `command_id`, опционально `args` (JSON object) |

**Меню, тулбар, task bar и чат через `ide_execute_command`:** те же `command_id`, что заданы в частичном классе `IdeCommands` (`Services/IdeCommands.cs`, `Services/IdeCommands.*.cs`).

### Подведение итогов сессии чата

Агенту **не нужно** притворяться, что «всё помнит без сжатия»: для длинного треда нормально **предложить** явное подведение итогов. Поддерживаемый сценарий:

1. Вызвать `ide_execute_command` с `command_id` **`chat_export_readable`** и при необходимости `args`: `write_file` (boolean, по умолчанию false), `file_name` (string, опционально). При `write_file: true` файл попадает в `.cascade-ide/chat-sessions/exports/`; иначе содержимое приходит в ответе JSON.
2. По экспорту (или по прочитанному файлу) дать **краткое смысловое резюме** — решения, открытые вопросы, следующие шаги.
3. **Согласовать** с пользователем формулировку итога; при необходимости зафиксировать в `.cascade-ide/agent-notes.md` (`ide_write_agent_notes` / `ide_append_agent_notes`) или в KB по правилам канона (репозиторий agent-notes, `knowledge/`), а не подменять прозрачный экспорт непрозрачным «внутренним» сжатием истории.

Пошаговый плейбук для агента: `knowledge/playbook-session-summary-and-chat-export-v1.md` (в каноне agent-notes — тот же путь под `knowledge/`). Если **MCP Cascade IDE в сессии нет**, тот же смысл (экспорт → резюме → согласование) выполняется **вне IDE**: поиск нужного `*.jsonl` в доступных `agent-transcripts` через `rg`/grep по запомненной фразе, затем читаемый экспорт скриптом **`tools/Export-CursorJsonlTranscript.ps1`** (в корне репозитория / в каноне agent-notes — каталог `tools/`; опционально отдельный архив вроде `cursor-agent-transcripts-archive`) — детали в том же плейбуке, **ветка B**.

<!-- GENERATED:IdeCommands START -->

> Этот блок сгенерирован из XML-doc в частичном классе `IdeCommands`: `Services/IdeCommands.cs` и `Services/IdeCommands.*.cs` (склейка как в генераторе).

### Core

| command_id | Описание |
|-----------:|----------|
| `append_agent_notes` | Добавить блок в конец заметок агента. args: content:string; returns: text; example: {"content":"\\n# Update\\n..."}. |
| `build` | Сборка решения (структурированный результат). returns: json. |
| `build_structured` | Сборка решения (структурированный результат). То же, что `build`; выделено для совместимости/алиасов. returns: json. |
| `compact_hot_context` | Ужать hot-context (preview/apply). args: apply?:boolean; returns: json; example: {"apply":false}. |
| `extract_from_archive` | Поиск по архивной ревизии заметок с контекстом строк. args: query:string, revision_file?:string, head_limit?:integer, context_lines?:integer; returns: json; example: {"query":"ActiveProjectId","head_limit":10,"context_lines":2}. |
| `get_build_output` | Текст панели «Вывод сборки» + цвета оформления. returns: json. |
| `get_code_metrics` | Метрики кода (LOC/классы/методы/цикломатика). args: scope?:string, path?:string; returns: json; example: {"scope":"solution","path":"."}. |
| `list_agent_notes_revisions` | Список ревизий заметок агента. args: limit?:integer; returns: json; example: {"limit":20}. |
| `memory_health` | Health-check памяти: размер hot-context и рекомендации. args: active_scope?:string; returns: json; example: {"active_scope":"door-to-singularity"}. |
| `read_agent_notes` | Прочитать заметки агента из каталога решения. returns: text. |
| `read_hot_context` | Прочитать только hot-context (L0/L1) без архивного хвоста. args: active_scope?:string; returns: json; example: {"active_scope":"door-to-singularity"}. |
| `rollback_agent_notes` | Откатить заметки к ревизии (или к последней). args: revision_file?:string; returns: text; example: {"revision_file":"20260402-120000-write-acde123.md"}. |
| `route_context` | Router-first контекст пакет по запросу. args: query:string, active_scope?:string, max_sections?:integer, max_chars?:integer; returns: json; example: {"query":"CascadeIDE notes structure","max_sections":5,"max_chars":12000}. |
| `run_affected_tests` | Запустить затронутые тесты по changed_paths (или fallback на полный прогон). args: changed_paths?:string[]; returns: json; example: {"changed_paths":["a.cs","b.cs"]}. |
| `run_code_cleanup` | Запустить code cleanup (`dotnet format`). args: include_path?:string; returns: json; example: {"include_path":"src"}. |
| `run_tests` | Запустить тесты решения. returns: json. |
| `search_agent_notes` | Поиск по заметкам агента (case-insensitive) с возвратом совпадающих строк. args: query:string, head_limit?:integer; returns: json; example: {"query":"ActiveProjectId","head_limit":20}. |
| `upsert_agent_notes_section` | Вставить/обновить секцию заметок агента по section_id (маркерный блок). args: section_id:string, content:string; returns: text; example: {"section_id":"active","content":"ActiveProjectId: cascade-ide"}. |
| `write_agent_notes` | Записать заметки агента в каталог решения. args: content:string; returns: text; example: {"content":"notes"}. |

### Меню «Файл» / приложение (те же RelayCommand, что в UI)

| command_id | Описание |
|-----------:|----------|
| `exit_application` | Закрыть приложение (как меню Файл → Выход). returns: none. |
| `open_file_dialog` | Открыть диалог выбора файла и показать его в редакторе (как меню Файл → Открыть файл...). returns: text. |
| `open_folder_dialog` | Открыть диалог выбора папки как workspace (как меню Файл → Открыть папку...). returns: text. |
| `open_solution_dialog` | Открыть диалог выбора решения (как меню Файл → Открыть решение...). returns: text. |

### Вид: панели (явная установка + переключатели)

| command_id | Описание |
|-----------:|----------|
| `set_git_panel_visible` | Показать/скрыть панель Git (нижняя вкладка). args: visible:boolean; returns: text; example: {"visible":true}. |
| `set_instrumentation_dock_visible` | Показать/скрыть док инструментирования (Events/Tests/Debug). args: visible:boolean; returns: text; example: {"visible":true}. |
| `set_mfd_region_expanded` | Развернуть/свернуть регион Mfd в main grid. args: visible:boolean; returns: text; example: {"visible":true}. |
| `set_pfd_region_expanded` | Развернуть/свернуть регион Pfd в main grid (карта намерений в зоне Pfd). args: visible:boolean; returns: text; example: {"visible":true}. |
| `toggle_git_panel` | Переключить видимость панели Git (toggle). returns: text. |
| `toggle_instrumentation_dock` | Переключить видимость дока инструментирования (toggle). returns: text. |
| `toggle_mfd_region_expanded` | Переключить развёрнут/свёрнут регион Mfd (toggle). returns: text. |

### Вид: режим

| command_id | Описание |
|-----------:|----------|
| `close_environment_readiness_page` | Перейти с страницы «готовность окружения» на первую другую разрешённую страницу вторичного контура. returns: text. |
| `cycle_ui_mode` | Циклически переключить UI mode (hotkey). returns: text. |
| `set_mfd_shell_page` | Активная страница оболочки Mfd: имя значения MfdShellPage (Chat, Terminal, …). Якорь на экране — пресет (v1 — колонка зоны Mfd). args: page:string; returns: text; example: {"page":"Chat"}. |
| `set_secondary_shell_page` | Устаревший идентификатор MCP-команды; поведение совпадает с `set_mfd_shell_page`. args: page:string; returns: text; example: {"page":"Chat"}. |
| `show_environment_readiness_page` | Показать страницу «готовность окружения» во вторичном контуре (зона Mfd; ADR 0023). Разворачивает регион Mfd при необходимости. returns: text. |
| `show_markdown_preview_page` | Показать Markdown preview как страницу во вторичном контуре/MFD. returns: text. |
| `toggle_command_palette` | Открыть или закрыть палитру команд (как Ctrl+Q / пункт меню «Вид»). returns: text. |

### Вид: тема

| command_id | Описание |
|-----------:|----------|
| `apply_cursor_like_theme` | Применить тему «как Cursor». returns: text. |
| `apply_dark_theme` | Применить тёмную тему. returns: text. |
| `apply_light_theme` | Применить светлую тему. returns: text. |
| `apply_power_classic_theme` | Применить классическую Power-тему (циан). returns: text. |
| `export_expanded_markdown` | Экспортировать текущий Markdown с развёрнутыми include-директивами. returns: text. |
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
| `toggle_mfd_host_window` | Открыть или активировать окно-хост зоны Mfd, если строка `presentation` / `zone_screen_layout` задаёт топологию с выносом Mfd (ADR 0017); иначе не выполняется. Отдельного пункта меню нет — источник истины раскладка. returns: text. |
| `toggle_pm_split_host_window` | Открыть или активировать окно сплита P+M при пресете `(xP+yM)(F)` / `(F)(xP+yM)` (ADR 0017). returns: text. |

### Тулбар: показать панели / скрыть вывод сборки

| command_id | Описание |
|-----------:|----------|
| `hide_build_output_panel` | Скрыть панель вывода сборки (toolbar). returns: text. |
| `show_build_output_panel` | Явно показать панель вывода сборки (toolbar). returns: text. |
| `show_chat_page` | Развернуть регион Mfd и перейти на страницу Chat (toolbar). returns: text. |
| `show_pfd_region_panel` | Развернуть регион Pfd / карту намерений (toolbar). returns: text. |
| `show_related_files_mfd_page` | Развернуть регион Mfd и открыть страницу «Связанные файлы» (related; workspace). returns: text. |
| `show_solution_explorer_page` | Развернуть регион Mfd и перейти на страницу обозревателя решения (toolbar). returns: text. |
| `show_terminal_panel` | Явно показать терминал (toolbar). returns: text. |

### Тулбар: группы редакторов

| command_id | Описание |
|-----------:|----------|
| `build_solution_ui` | Кнопка «Собрать» в тулбаре: dotnet build в панель вывода (не structured build). returns: text. |
| `set_dual_editor_group` | Две группы редакторов (2-up). returns: text. |
| `set_single_editor_group` | Одна группа редакторов (1-up). returns: text. |
| `set_triple_editor_group` | Три группы редакторов (3-up). returns: text. |

### DAP / netcoredbg (паритет с dotnet-debug-mcp)

| command_id | Описание |
|-----------:|----------|
| `add_control` | Добавить контрол в UI (Debug). args: parent_name:string, control_type:string, content?:string, name?:string; returns: text; example: {"parent_name":"Root","control_type":"TextBlock","content":"Hi"}. |
| `append_knowledge_file` | Добавить блок в конец knowledge-файла. args: file_path:string, content:string, canon_path?:string, save_revision?:boolean; returns: text; example: {"file_path":"META/x.md","content":"more","save_revision":true}. |
| `click_control` | Клик по кнопке (под курсором или по имени). args: name?:string; returns: text; example: {"name":"BuildButton"}. |
| `debug_attach` | Подключиться к процессу по PID. args: workspace_path:string, process_id:integer, target_path?:string, netcoredbg_path?:string; returns: text; example: {"workspace_path":"D:\\\\proj","process_id":12345}. |
| `debug_continue` | Продолжить выполнение (DAP continue). returns: text. |
| `debug_launch` | Запустить отладку (netcoredbg DAP). args: workspace_path?:string, target_path?:string, profile_name?:string, netcoredbg_path?:string, program_args?:string[] . returns: text; example: {"workspace_path":"D:\\\\proj","target_path":"samples\\\\DebugTarget\\\\bin\\\\Debug\\\\net10.0\\\\DebugTarget.dll"}. |
| `debug_ping` | Проверка доступности встроенной отладки. returns: text. |
| `debug_stack_trace` | Стек вызовов (DAP stackTrace). returns: text. |
| `debug_step_into` | Шаг с заходом (DAP stepIn). returns: text. |
| `debug_step_out` | Шаг с выходом (DAP stepOut). returns: text. |
| `debug_step_over` | Шаг через строку (DAP next). returns: text. |
| `debug_stop` | Завершить сессию отладки (dispose DAP). returns: text. |
| `debug_variables` | Переменные кадра. args: frame_index?:integer; returns: text; example: {"frame_index":0}. |
| `delete_knowledge_file` | Удалить knowledge-файл. args: file_path:string, canon_path?:string; returns: text; example: {"file_path":"tmp.md"}. |
| `delete_knowledge_section` | Удалить секцию из knowledge-файла. args: file_path:string, section_id:string, canon_path?:string; returns: text; example: {"file_path":"index.md","section_id":"foo"}. |
| `fetch_web_public_url` | Загрузить публичный HTTPS-документ по URL и вернуть тело как читаемый текст (HTML упрощается до текста, поле extraction в JSON). Запрос из машины оператора; только https; локальные/частные хосты блокируются базово (не полная SSRF-защита). args: url:string, max_chars?:integer; returns: json; example: {\"url\":\"https://learn.microsoft.com/en-us/dotnet/\"}. |
| `get_colors_under_cursor` | Цвета под курсором (прямые и effective). returns: json. |
| `get_control_appearance` | Снимок внешнего вида контрола (под курсором или по имени). args: name?:string; returns: json; example: {"name":"BuildButton"}. |
| `get_debug_snapshot` | JSON: канонический снимок встроенной DAP-сессии (ADR 0002). returns: json. |
| `get_supported_editor_languages` | Список поддерживаемых языков подсветки редактора. returns: json. |
| `get_ui_layout` | Дерево UI по всем окнам верхнего уровня: JSON с массивом windows (role, window_type, title, is_active, root — то же дерево, что раньше для MainWindow). returns: json. |
| `get_ui_theme` | Снимок темы UI и лэйаута (включая resolved-ресурсы). returns: json. |
| `git_branch` | Git branch в каталоге решения/workspace. args: action?:string, name?:string, start_point?:string, force?:boolean; returns: json; example: {"action":"list"}. |
| `git_commit` | Git commit в каталоге решения/workspace. args: message:string, paths?:string[]; returns: text; example: {"message":"chore: update","paths":["a.txt"]}. |
| `git_diff` | Git diff в каталоге решения/workspace. args: path?:string, staged?:boolean; returns: json; example: {"path":"README.md","staged":false}. |
| `git_fetch` | Git fetch в каталоге решения/workspace. args: remote?:string, all?:boolean, prune?:boolean, dry_run?:boolean; returns: json; example: {"prune":true,"dry_run":true}. |
| `git_log` | Git log в каталоге решения/workspace. args: n?:integer; returns: json; example: {"n":20}. |
| `git_preflight` | Git preflight в каталоге решения/workspace. args: staged?:boolean, include_untracked?:boolean, include_patches?:boolean; returns: json; example: {"staged":false,"include_untracked":true,"include_patches":true}. |
| `git_preflight_fix_safe` | Git preflight safe-fix (renormalize) в каталоге решения/workspace. args: include_patches?:boolean; returns: json; example: {"include_patches":true}. |
| `git_pull` | Git pull в каталоге решения/workspace. args: remote?:string, branch?:string, ff_only?:boolean, dry_run?:boolean; returns: json; example: {"ff_only":true}. |
| `git_push` | Git push в каталоге решения/workspace. args: remote?:string, branch?:string, dry_run?:boolean; returns: text; example: {"remote":"origin","branch":"main","dry_run":true}. |
| `git_show` | Git show в каталоге решения/workspace. args: rev:string, path?:string, stat_only?:boolean; returns: json; example: {"rev":"HEAD","stat_only":true}. |
| `git_status` | Git status в каталоге решения/workspace (git status --short --branch). returns: json. |
| `git_submodule` | Git submodule в каталоге решения/workspace. args: action?:string, path?:string, recursive?:boolean; returns: json; example: {"action":"status"}. |
| `highlight_control` | Подсветить контрол рамкой в том окне, где он находится (главное, окно-хост Mfd и т.д.). args: name?:string; returns: text; example: {"name":"BuildButton"}. |
| `list_knowledge_files` | Список knowledge-файлов в каталоге решения (опционально subdir). args: subdir?:string; returns: json; example: {"subdir":"work"}. |
| `read_knowledge_file` | Прочитать knowledge-файл из каталога решения. args: file_path:string, offset?:integer, limit?:integer; returns: text; example: {"file_path":"META/integrity-core.md","offset":2,"limit":20}. |
| `search_web_public_query` | Краткая веб-справка через открытый Instant Answer DuckDuckGo (запрос уходит во внешнюю сеть; не замена полнотекстового поиска). args: query:string; returns: json; example: {\"query\":\"C# file scoped types\"}. |
| `send_keys` | Отправить хоткей в контрол. args: keys:string, name?:string; returns: text; example: {"keys":"Ctrl+S"}. |
| `set_control_layout` | Изменить раскладку/позицию контрола. args: name:string, layout:string; returns: text; example: {"name":"BuildButton","layout":"{}"}. |
| `set_control_text` | Установить текст в контроле ввода. args: name:string, text:string; returns: text; example: {"name":"ChatInput","text":"hi"}. |
| `set_focus` | Передать фокус контролу (под курсором или по имени). args: name?:string; returns: text; example: {"name":"Editor"}. |
| `set_panel_size` | Изменить размер панели. args: panel:string, width?:integer, height?:integer; returns: text; example: {"panel":"terminal","height":300}. |
| `set_ui_theme` | Применить тему UI из JSON. args: theme:string; returns: text; example: {"theme":"{}"}. |
| `upsert_knowledge_section` | Вставить/обновить секцию в knowledge-файле по section_id. args: file_path:string, section_id:string, content:string, canon_path?:string, save_revision?:boolean; returns: text; example: {"file_path":"index.md","section_id":"foo","content":"body"}. |
| `write_knowledge_file` | Записать knowledge-файл в канон (полная замена). args: file_path:string, content:string, canon_path?:string, save_revision?:boolean; returns: text; example: {"file_path":"META/x.md","content":"# Hi","save_revision":true}. |

### MCP / редактор

| command_id | Описание |
|-----------:|----------|
| `apply_edit` | Применить текстовую правку в открытом документе. args: file_path:string, start_line:integer, start_column:integer, end_line:integer, end_column:integer, new_text:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs","start_line":1,"start_column":1,"end_line":1,"end_column":1,"new_text":"// hi\n"}. |
| `get_editor_content_range` | Текст активного редактора по диапазону строк (1-based). args: start_line:integer, end_line:integer; returns: json; example: {"start_line":1,"end_line":40}. |
| `get_editor_state` | Состояние активного редактора: файл, каретка, выделение. args: max_preview_chars?:integer; returns: json; example: {"max_preview_chars":0}. |
| `get_open_document_text` | Полный текст открытого документа по пути (или текущего). Модель вкладки, не снимок темы. returns: text. |
| `go_to_position` | Перейти на позицию (и опционально выделить диапазон). args: file_path:string, line:integer, column:integer, end_line?:integer, end_column?:integer; returns: text; example: {"file_path":"C:\\tmp\\a.cs","line":10,"column":1}. |
| `list_tools` | Список MCP-тулов, которые IDE публикует (name/description/inputSchema). returns: json. |
| `load_solution` | Загрузить решение (.sln/.slnx/.slnf), один проект (.csproj/.fsproj) или каталог как workspace (дерево без .sln) — обновить обозреватель. args: path:string; returns: text; example: {"path":"D:\\repo\\MyApp.csproj"}. |
| `open_file` | Открыть файл в редакторе IDE. args: path:string; returns: text; example: {"path":"C:\\tmp\\a.txt"}. |
| `ping` | Живость MCP-хоста IDE (без аргументов). Имя MCP-тула: `ide_ping`. returns: json. |
| `remove_breakpoint` | Снять брейкпоинт. args: file_path:string, line:integer; returns: text; example: {"file_path":"C:\\tmp\\a.cs","line":42}. |
| `request_confirmation` | Запросить подтверждение у пользователя. args: message:string; returns: text; example: {"message":"Продолжить?"}. Возвращает `ok`/`cancel`. |
| `restart_mcp_clients` | Пересоздать клиентов внешних MCP и сбросить сессию Cursor ACP (после сбоев транспорта). Имя MCP-тула: `ide_restart_mcp_clients`. returns: json. |
| `select` | Выделить диапазон в редакторе (1-based). args: file_path:string, start_line:integer, start_column:integer, end_line:integer, end_column:integer; returns: text; example: {"file_path":"C:\\tmp\\a.cs","start_line":1,"start_column":1,"end_line":1,"end_column":10}. |
| `set_breakpoint` | Поставить брейкпоинт: при необходимости загрузка найденного .sln/.slnx/.slnf, запись в JSON отладки, открытие файла и переход к строке. args: file_path:string, line:integer, condition?:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs","line":42}. |
| `show_editor_preview` | Показать превью текущего файла из редактора в отдельном окне (контент берётся из IDE). returns: text. |
| `show_preview` | Показать Markdown-превью в отдельном окне. args: title:string, content:string; returns: text; example: {"title":"Plan","content":"- step 1\n- step 2"}. |

### Focus / Power / автономка / чат

| command_id | Описание |
|-----------:|----------|
| `cancel_focus_step` | Отменить текущий шаг плана (Focus). returns: text. |
| `confirm_focus_step` | Подтвердить текущий шаг плана (Focus). returns: text. |
| `emergency_stop` | Экстренно остановить автономные действия/выполнение (Emergency stop). returns: text. |
| `explain_current_step` | Пояснить текущий шаг (Focus/Power). returns: text. |
| `explain_trace_step` | Шаг трассы по индексу в AgentTraceSteps (0 — самый старый). args: step_index:integer; returns: text; example: {"step_index":0}. |
| `fix_failing_tests` | Quick action: починить упавшие тесты. returns: text. |
| `focus_checkpoint` | Создать контрольную точку (Focus). returns: text. |
| `focus_rollback` | Откатить к последней контрольной точке (Focus). returns: text. |
| `fork_chat_thread` | Новая ветка чата: args: parent_message_id?:string. Пишет thread_forked; следующее user-сообщение может ссылаться на родителя. returns: text; example: {} |
| `install_ollama_model` | Скачать модель Ollama (как в настройках). args: model:string; returns: text; example: {"model":"qwen2.5-coder:7b"}. |
| `investigate_nullref` | Quick action: расследовать NullReferenceException. returns: text. |
| `open_chat_clarification_batch` | Открыть structured clarification batch в чате. args: batch_json:string. returns: text; example: {"batch_json":"{\"id\":\"...\",\"title\":\"Нужно уточнить\",\"items\":[]}"}. |
| `pause_autonomous` | Поставить автономный режим на паузу. returns: text. |
| `prepare_commit` | Quick action: подготовить коммит (сводка/план/проверки). returns: text. |
| `refresh_workspace_snapshot` | Обновить снимок рабочего состояния (Power cockpit). returns: text. |
| `resume_autonomous` | Продолжить автономный режим после паузы. returns: text. |
| `rollback_trace_step` | Откатить состояние по шагу трассы. args: step_index:integer; returns: text; example: {"step_index":0}. |
| `send_chat` | Чат: args: message?:string, role?:string. role assistant — только строка ассистента из MCP; иначе user и отправка; при ai.mode = mcp_only встроенный LLM не вызывается. returns: text; example: {"message":"hello"}. |
| `set_safety_l1` | Установить Safety L1. returns: text. |
| `set_safety_l2` | Установить Safety L2. returns: text. |
| `set_safety_l3` | Установить Safety L3. returns: text. |
| `start_autonomous` | Запустить автономный режим (agent run). returns: text. |
| `submit_chat_clarification_response` | Отправить structured clarification response для активного batch. args: response_json:string. returns: text; example: {"response_json":"{\"batchId\":\"...\",\"answersByItemId\":{\"scope\":\"mfd\"}}"}. |

### Документы

| command_id | Описание |
|-----------:|----------|
| `activate_document` | Активировать документ (переключить вкладку). args: file_path:string; returns: text; example: {"file_path":"C:\\\\tmp\\\\a.cs"}. |
| `capture_window` | Снимок окон IDE в PNG (по умолчанию главное окно; при scope=all — все top-level, в т.ч. окно-хост Mfd и прочие). args: scope?:string, workspace_path?:string, output_path?:string; returns: json. example: {"scope":"all","workspace_path":"D:\\\\tmp\\\\ws","output_path":".cascade-ide/window-{n}.png"}. |
| `chat_edit_message` | Заменить текст ответа ассистента по стабильному message_id; в лог пишется message_edited. args: message_id:string, new_content:string, reason?:string; returns: json; example: {"message_id":"a1b2c3d4e5f6789012345678901234ab","new_content":"fixed text"}. |
| `chat_export_readable` | Экспорт текущего чата в читаемый Markdown (роли, индексы, message_id). Поддерживаемый сценарий — явно подвести итоги длинной сессии: экспорт, затем краткое смысловое резюме и согласование с пользователем (см. MCP-PROTOCOL.md, раздел «Подведение итогов сессии чата»). args: write_file?:boolean, file_name?:string; returns: json; example: {"write_file":true}. |
| `chat_get_selected_message` | Получить выбранное сообщение чата (индекс, роль, контент) в JSON. returns: json. |
| `chat_open_selected_thread` | Открыть detail выбранной темы. returns: text. |
| `chat_select_message` | Выбрать сообщение в чате по индексу (0-based), в т.ч. для Skia-поверхности. args: index:integer; returns: text; example: {"index":0}. |
| `chat_select_next_message` | Сместить выбор на следующее сообщение чата (keyboard-first). returns: text. |
| `chat_select_next_thread` | Выбрать следующую тему в overview (циклически). returns: text. |
| `chat_select_prev_message` | Сместить выбор на предыдущее сообщение чата (keyboard-first). returns: text. |
| `chat_select_prev_thread` | Выбрать предыдущую тему в overview (циклически). returns: text. |
| `chat_show_thread_overview` | Вернуться в overview тем (карточки). returns: text. |
| `chat_toggle_selected_thinking` | Переключить у выбранного thinking-сообщения свёрнутый/полный вид. returns: text. |
| `chat_toggle_show_thinking_in_history` | Переключить настройку show_thinking_in_history (keyboard-first toggle). returns: text. |
| `close_document` | Закрыть документ. args: file_path:string; returns: text; example: {"file_path":"C:\\\\tmp\\\\a.cs"}. |
| `cycle_code_navigation_map_detail_level` | Карта намерений: цикл детализации glance → normal → inspect (Ctrl+K → S → D). returns: text. |
| `cycle_code_navigation_map_level` | Карта намерений: переключить уровень file ↔ controlFlow (Ctrl+K → S → F). returns: text. |
| `cycle_code_navigation_map_presentation` | Карта намерений: цикл вида list → graph → both (палитра; быстрый путь — Ctrl+K → S → P). returns: text. |
| `focus_editor` | Передать фокус в редактор (чтобы клавиши/ввод шли в него). returns: text. |
| `get_cockpit_surface` | Только CDS (`CockpitSurfaceState`): тот же payload, что поле `cockpit_surface` в `get_ide_state`. returns: json. Для `--agent-contract` без полной сводки. |
| `get_code_navigation_context` | Контекст навигации по коду (ADR 0039, CNC): связанные файлы или мини-подграф. Виды связей — partial_peer project_peer xaml_codebehind_pair test_counterpart same_namespace same_directory. Имена preset — из settings.toml `[code_navigation]` / `[[code_navigation.presets]]`. args: mode:string, file_path?:string, line?:integer, column?:integer, max_related?:integer, max_nodes?:integer, max_edges?:integer, preset?:string, include_kinds?:string[], exclude_kinds?:string[], level?:string; returns: json; example: {"mode":"related","file_path":"src/Foo.cs","preset":"no_namespace_noise","level":"controlFlow"}. |
| `get_current_file_diagnostics` | Диагностики текущего открытого .cs (ошибки/предупреждения). returns: json. |
| `get_ide_state` | Единая сводка состояния IDE (solution/editor/build/diagnostics...). returns: json. |
| `get_solution_files` | Список файлов и дерево решения (Solution Explorer). returns: json. |
| `get_solution_info` | Короткая информация о текущем решении/файле/выделении в дереве. returns: json. |
| `get_ui_modes_diagnostics` | Диагностика загрузки UI-режимов: пути к UiModes, TOML vs встроенный fallback, список id в меню (почему может не быть Flight). returns: json. |
| `move_document_to_group_1` | Переместить документ в группу 1. args: file_path:string; returns: text; example: {"file_path":"C:\\\\tmp\\\\a.cs"}. |
| `move_document_to_group_2` | Переместить документ в группу 2. args: file_path:string; returns: text; example: {"file_path":"C:\\\\tmp\\\\a.cs"}. |
| `move_document_to_group_3` | Переместить документ в группу 3. args: file_path:string; returns: text; example: {"file_path":"C:\\\\tmp\\\\a.cs"}. |
| `reopen_closed_document` | Переоткрыть недавно закрытый документ. returns: text. |
| `search_workspace_text` | Поиск текста по workspace через ripgrep: вызывается команда `rg` из PATH (Windows/Linux/macOS — поставь пакетом или с релиза). Явный путь: только `rg_path`. args: pattern:string, subpath?:string, fixed_string?:boolean, glob?:string, max_matches?:integer, rg_path?:string; returns: json; example: {\"pattern\":\"LoadSolution\",\"glob\":\"*.cs\",\"max_matches\":50}. |
| `set_build_output_visible` | Явно показать/скрыть журнал сборки. args: visible:boolean; returns: text; example: {"visible":true}. |
| `set_terminal_visible` | Явно показать/скрыть терминал (без переключения). args: visible:boolean; returns: text; example: {"visible":true}. |
| `set_ui_mode` | Режим UI (как меню «Вид → Режим интерфейса»). args: mode:string; returns: text; example: {"mode":"Flight"}. |
| `toggle_build_output` | Как меню «Вид → Вывод сборки». returns: text. |
| `toggle_pfd_region_expanded` | Переключить развёрнут/свёрнут регион Pfd (как меню «Вид → Карта намерений (PFD)»). returns: text. |
| `toggle_pin_document` | Закрепить/открепить документ (pin). args: file_path:string; returns: text; example: {"file_path":"C:\\\\tmp\\\\a.cs"}. |
| `toggle_terminal` | Как меню «Вид → Терминал» (переключатель). returns: text. |
| `toggle_workspace_splitters_lock` | Сплиттеры рабочей области: переключить ON GND / IN AIR (мелодия tol, лампа TOL в task cockpit). returns: text. |
<!-- GENERATED:IdeCommands END -->

**Семантическая навигация (`get_code_navigation_context`):** пресеты задаются в `%LocalAppData%\CascadeIDE\settings.toml` в секции `[code_navigation]` (`[[code_navigation.presets]]` в TOML). В ответе смотри `kind_filter` (эффективные списки) и в режиме `subgraph` — `kind` на узлах и `related_kind` на рёбрах. Подробный cookbook: [workspace-navigation-mcp-cookbook.md](design/workspace-navigation-mcp-cookbook.md).

Проверка: `ide_get_ide_state` — помимо `terminal.is_visible`, `ui_mode`, есть `panels` (видимость колонок), `safety_level`, `editor_group_count`, `agent_trace_step_count`, `is_autonomous_running`, **`cockpit_surface`** (CDS: `schema_version`, зоны, топология, `instruments` и т.д., см. `docs/design/cds-contract-v0.md`).

## Подключение из Cursor

1. **Self-contained exe (рекомендуется):** в корне проекта заданы `RuntimeIdentifier=win-x64` и `SelfContained=true`.
   - **Release:** из каталога `cascade-ide` запусти **`.\scripts\deploy\publish-release.ps1`** — публикует в `publish` и зеркалит в **`D:\cascade-ide`** (копирование; путь без пробелов для Cursor). В конце печатается фрагмент для `mcp.json`. Без регенерации MCP-док из `IdeCommands`: **`.\scripts\deploy\publish-release.ps1 -SkipDocGen`**. Вручную то же самое: `dotnet publish -c Release -r win-x64 --self-contained true -o publish` и копирование в `D:\cascade-ide` (или junction на каталог `publish`).
   - **Debug (отладка MCP-обработчиков):** **`.\scripts\deploy\publish-debug.ps1`** — вывод в `publish-debug`, зеркало **`D:\cascade-ide-debug`**. В Cursor: `command` — `D:\cascade-ide-debug\CascadeIDE.exe`, **`args`: `["--mcp-stdio"]`**. Ускоренная сборка: **`.\scripts\deploy\publish-debug.ps1 -SkipDocGen`**.

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

Пока достаточно сценария «клиент (Cursor, ACP и т.д.) запускает IDE» с `--mcp-stdio`: открывается **главное окно** Cascade IDE — так Pilot Monitoring может смотреть в ту же IDE, куда стучится агент по MCP. Параллельно поднимается MCP на stdin/stdout (включается **только** флагом `--mcp-stdio`, отдельной настройки «включить stdio» нет). В окне отображается подсказка: «Управляется агентом (MCP). Не закрывайте окно — подключение будет потеряно.»

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

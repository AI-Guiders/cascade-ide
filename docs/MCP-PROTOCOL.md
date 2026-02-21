# Протокол MCP: агент ↔ IDE

По идее из [IDEAS.md](../../IDEAS.md): IDE — тонкий клиент, которым управляет агент через MCP. Агент (в Cursor или во встроенном чате) вызывает инструменты MCP; IDE отображает результат и выполняет действия.

## Роль сторон

- **Агент** — клиент MCP (Cursor, встроенный чат с моделью с tool calling и т.п.).
- **CascadeIDE** — MCP-сервер: предоставляет инструменты для управления редактором, брейкпоинтами, превью, подтверждениями.

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
| `ide_apply_edit` | Применить правку в открытом файле | `file_path`, `start_line`, `start_column`, `end_line`, `end_column`, `new_text` (1-based) |
| `ide_go_to_position` | Перейти на позицию (и опционально выделить) | `file_path`, `line`, `column`; опционально `end_line`, `end_column` |
| `ide_get_solution_info` | Информация о решении и открытом файле | —; возвращает JSON (solution_path, current_file_path, project_paths) |
| `ide_build` | Запустить сборку решения (dotnet build). **Структурированный результат:** JSON: success, exit_code, errors[] (file, line, column?, code?, message), warnings[], raw_output (обрезано). Агент получает ошибки без парсинга лога. | —; возвращает JSON |
| `ide_get_build_output` | Текст панели «Вывод сборки» и цвета (background, foreground) | —; возвращает JSON: text, theme |
| `ide_run_tests` | Запустить тесты решения (dotnet test; при необходимости выполняет сборку). **Структурированный результат:** JSON: success, total, passed, failed, skipped, failed_tests[] (name, message?, duration_ms?). Агент получает упавшие тесты без парсинга лога. | —; возвращает JSON |
| `ide_focus_editor` | Передать фокус в редактор | — |
| `ide_get_ui_theme` | Параметры темы UI (цвета, фоны, кнопки, шрифты) | —; возвращает JSON |
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
| `ide_show_preview` | Показать Markdown в отдельном окне превью (планы, заметки, отчёты) | `title`, `content` (Markdown) |
| `ide_show_editor_preview` | Показать превью **текущего файла из редактора** в отдельном окне. Контент берётся из IDE (не передаётся по MCP) — удобно для длинных .md с таблицами. | — |
| `ide_request_confirmation` | Запросить подтверждение у пользователя | `message`; возвращает ответ (ok/cancel или текст) |
| `ide_write_agent_notes` | Записать заметки агента. Агент сам решает, когда, что и в каком формате (markdown, json, текст). Хранятся в каталоге решения в `.cascade-ide/agent-notes.md`. Без открытого решения — ошибка. Для непрерывности между сессиями и до суммаризации. | `content` — полное содержимое (перезаписывает файл); при успехе — `OK` |
| `ide_read_agent_notes` | Прочитать заметки агента из `.cascade-ide/agent-notes.md`. Возвращает содержимое или пустую строку. Агент восстанавливает контекст в новом чате. | —; возвращает текст файла или `""` |

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
{"main_window":{"background":"#1E1E1E"},"menu":{"background":"#252526","foreground":"#CCCCCC"},"button":{"background":"#3C3C3C","foreground":"#CCCCCC","border_brush":"#555","hover_background":"#505050","disabled_background":"#2D2D2D","disabled_foreground":"#858585"},"toolbar":{"background":"#2D2D2D"},"toolbar_text":{"foreground":"#CCCCCC","error_foreground":"#F48771"},"editor":{"background":"#1E1E1E","foreground":"#D4D4D4"},"editor_column":{"border_brush":"#3F3F46","background":"#1E1E1E","current_file_foreground":"#9D9D9D"},"markdown_preview_panel":{"background":"#252526","border_brush":"#3F3F46"},"solution_explorer":{"border_brush":"#3F3F46","header_foreground":"#CCCCCC"},"build_output":{"background":"#252526","foreground":"#D4D4D4","border_brush":"#3F3F46"},"chat_panel":{"background":"#252526","label_foreground":"#CCCCCC","message_bubble_background":"#2D2D2D","message_content_foreground":"#D4D4D4","send_button_background":"#0E639C","send_button_foreground":"#FFF"},"terminal":{"background":"#1E1E1E","foreground":"#CCCCCC","input_background":"#2D2D2D"},"mcp_banner":{"background":"#094771","foreground":"#6CB6F0"},"preview_window":{"background":"#252526"}}
```

Тул возвращает «OK» при успехе или текст ошибки при невалидном JSON (например, `Invalid JSON: ...`) — агент видит причину в ответе вызова.

## Связка с другими MCP

Агент может одновременно использовать:

- **CascadeIDE** (MCP-сервер) — управление IDE: открытие файлов, брейкпоинты, превью.
- **dotnet-debug-mcp** — отладка .NET (launch, breakpoints, step, variables).
- **roslyn-mcp** — рефакторинг и анализ C# (find usages, rename, code actions).

Один и тот же workspace: агент в Cursor вызывает CascadeIDE для отображения и dotnet-debug/roslyn для отладки и кода.

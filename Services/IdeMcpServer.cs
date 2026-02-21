using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CascadeIDE.Services;

public static class IdeMcpServer
{
    public static McpServerOptions BuildOptions(IIdeMcpActions actions)
    {
        static JsonElement Schema(object schema) => JsonSerializer.SerializeToElement(schema);

        var toolsList = new List<Tool>
        {
            new()
            {
                Name = "ide_open_file",
                Description = "Открыть файл в редакторе IDE по пути.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { path = new { type = "string", description = "Полный путь к файлу." } },
                    required = new[] { "path" }
                })
            },
            new()
            {
                Name = "ide_load_solution",
                Description = "Загрузить решение по пути (.sln или .slnx). Дерево проектов в IDE обновится.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { path = new { type = "string", description = "Полный путь к файлу решения." } },
                    required = new[] { "path" }
                })
            },
            new()
            {
                Name = "ide_select",
                Description = "Выделить диапазон в открытом файле в редакторе (строки и столбцы 1-based).",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        file_path = new { type = "string", description = "Полный путь к файлу (если не открыт — будет открыт)." },
                        start_line = new { type = "integer", description = "Начальная строка (1-based)." },
                        start_column = new { type = "integer", description = "Начальный столбец (1-based)." },
                        end_line = new { type = "integer", description = "Конечная строка (1-based)." },
                        end_column = new { type = "integer", description = "Конечный столбец (1-based)." }
                    },
                    required = new[] { "file_path", "start_line", "start_column", "end_line", "end_column" }
                })
            },
            new()
            {
                Name = "ide_set_breakpoint",
                Description = "Поставить брейкпоинт в файле на указанной строке.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        file_path = new { type = "string" },
                        line = new { type = "integer", description = "Номер строки (1-based)." },
                        condition = new { type = "string", description = "Опциональное условие." }
                    },
                    required = new[] { "file_path", "line" }
                })
            },
            new()
            {
                Name = "ide_show_preview",
                Description = "Показать Markdown в отдельном окне превью. Удобно показывать пользователю планы, заметки, отчёты в читаемом виде (как в Cursor).",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        title = new { type = "string", description = "Заголовок окна." },
                        content = new { type = "string", description = "Текст в формате Markdown." }
                    },
                    required = new[] { "title", "content" }
                })
            },
            new()
            {
                Name = "ide_show_editor_preview",
                Description = "Показать превью текущего файла из редактора в отдельном окне. Контент берётся из IDE (не передаётся по MCP) — удобно для длинных .md с таблицами. Если открыт не .md — окно покажет текущий текст редактора.",
                InputSchema = Schema(new { type = "object", properties = new { }, required = Array.Empty<string>() })
            },
            new()
            {
                Name = "ide_request_confirmation",
                Description = "Запросить подтверждение у пользователя. Возвращает ответ пользователя (ok/cancel или текст).",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { message = new { type = "string" } },
                    required = new[] { "message" }
                })
            },
            new()
            {
                Name = "ide_get_editor_state",
                Description = "Состояние редактора: file_path, каретка, выделение, content_length, is_empty, content_preview (если max_preview_chars > 0). По умолчанию превью 2000 символов; 0 = без превью.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { max_preview_chars = new { type = "integer", description = "Сколько символов превью (0 = нет, по умолчанию 2000)." } },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_get_editor_content_range",
                Description = "Содержимое редактора по диапазону строк (1-based). JSON: file_path, start_line, end_line, content. Чтобы не тянуть весь файл — запросить нужные строки.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        start_line = new { type = "integer", description = "Начальная строка (1-based)." },
                        end_line = new { type = "integer", description = "Конечная строка (1-based)." }
                    },
                    required = new[] { "start_line", "end_line" }
                })
            },
            new()
            {
                Name = "ide_apply_edit",
                Description = "Применить правку в открытом файле: заменить диапазон (1-based line/column) на новый текст.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        file_path = new { type = "string" },
                        start_line = new { type = "integer" },
                        start_column = new { type = "integer" },
                        end_line = new { type = "integer" },
                        end_column = new { type = "integer" },
                        new_text = new { type = "string" }
                    },
                    required = new[] { "file_path", "start_line", "start_column", "end_line", "end_column", "new_text" }
                })
            },
            new()
            {
                Name = "ide_go_to_position",
                Description = "Перейти на позицию в файле (и опционально выделить до end). Строки/столбцы 1-based.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        file_path = new { type = "string" },
                        line = new { type = "integer" },
                        column = new { type = "integer" },
                        end_line = new { type = "integer", description = "Опционально." },
                        end_column = new { type = "integer", description = "Опционально." }
                    },
                    required = new[] { "file_path", "line", "column" }
                })
            },
            new()
            {
                Name = "ide_get_solution_info",
                Description = "Информация о решении: solution_path, current_file_path, project_paths, selected_solution_path (путь узла, выделенного в обозревателе). JSON.",
                InputSchema = Schema(new { type = "object", properties = new { }, required = Array.Empty<string>() })
            },
            new()
            {
                Name = "ide_get_solution_files",
                Description = "Файлы и дерево решения. file_entries — массив { path, title, relative_path } (relative_path от каталога решения). solution_tree — иерархия (solution → projects → folders → files) с теми же полями. Для поиска .md или узла по пути и открытия через ide_open_file.",
                InputSchema = Schema(new { type = "object", properties = new { }, required = Array.Empty<string>() })
            },
            new()
            {
                Name = "ide_get_current_file_diagnostics",
                Description = "Диагностики (ошибки и предупреждения) по текущему открытому файлу. Только .cs; для остальных — []. JSON: массив { id, message, severity, line, column } (line/column 1-based). Live-анализ Roslyn по содержимому редактора.",
                InputSchema = Schema(new { type = "object", properties = new { }, required = Array.Empty<string>() })
            },
            new()
            {
                Name = "ide_build",
                Description = "Запустить сборку решения (dotnet build). Возвращает JSON: success, exit_code, errors[] (file, line, column?, code?, message), warnings[], raw_output (обрезано до 4000 символов). Агент получает структурированные ошибки без парсинга лога.",
                InputSchema = Schema(new { type = "object", properties = new { }, required = Array.Empty<string>() })
            },
            new()
            {
                Name = "ide_get_build_output",
                Description = "Текущее содержимое панели «Вывод сборки»: полный текст вывода и цвета оформления (background, foreground). JSON: text, theme. Чтобы агент видел, что сейчас отображается в панели сборки.",
                InputSchema = Schema(new { type = "object", properties = new { }, required = Array.Empty<string>() })
            },
            new()
            {
                Name = "ide_run_tests",
                Description = "Запустить тесты решения (dotnet test; при необходимости выполняет сборку). Возвращает JSON: success, total, passed, failed, skipped, failed_tests[] (name, message?, duration_ms?). Агент получает структурированный список упавших тестов без парсинга лога.",
                InputSchema = Schema(new { type = "object", properties = new { }, required = Array.Empty<string>() })
            },
            new()
            {
                Name = "ide_focus_editor",
                Description = "Передать фокус в редактор.",
                InputSchema = Schema(new { type = "object", properties = new { }, required = Array.Empty<string>() })
            },
            new()
            {
                Name = "ide_get_ui_theme",
                Description = "Получить параметры темы UI: цвета, фоны, кнопки, шрифты (JSON). Чтобы не гадать при правках оформления.",
                InputSchema = Schema(new { type = "object", properties = new { }, required = Array.Empty<string>() })
            },
            new()
            {
                Name = "ide_set_ui_theme",
                Description = "Применить тему UI на лету. JSON в том же формате, что возвращает ide_get_ui_theme (можно вызвать get, изменить нужные поля, передать в set).",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { theme = new { type = "string", description = "JSON темы (объект с main_window, menu, button, toolbar, editor, chat_panel, terminal, mcp_banner, preview_window и т.д.)." } },
                    required = new[] { "theme" }
                })
            },
            new()
            {
                Name = "ide_get_ui_layout",
                Description = "Получить дерево элементов UI: тип контрола, имя, видимость, границы (x, y, w, h в пикселях относительно окна), контент (текст/заголовок), дочерние. JSON. Чтобы видеть расположение кнопок, панелей, меню.",
                InputSchema = Schema(new { type = "object", properties = new { }, required = Array.Empty<string>() })
            },
            new()
            {
                Name = "ide_get_colors_under_cursor",
                Description = "Получить цвета под курсором: прямые (background, foreground) и эффективные (effective_background, effective_foreground — с учётом поддерева и предков, как на экране). JSON: type, name, background, foreground, effective_background, effective_foreground (hex #AARRGGBB).",
                InputSchema = Schema(new { type = "object", properties = new { }, required = Array.Empty<string>() })
            },
            new()
            {
                Name = "ide_get_control_appearance",
                Description = "Универсальный снимок любого контрола: прямой и эффективный цвет фона/текста (effective_*), содержимое, границы, видимость, шрифт, рамка, content_truncated (для TextBlock — из реального TextLayout контрола, HasOverflowed; иначе — оценка по измерению). Без аргументов — под курсором; с name — по имени. JSON: type, name, bounds, visible, content, content_truncated, background, foreground, effective_*, border_*, font_*.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { name = new { type = "string", description = "Имя контрола для поиска в дереве (опционально; если не задано — элемент под курсором)." } },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_set_control_layout",
                Description = "Изменить положение/раскладку контрола на лету. name — имя контрола (из ide_get_ui_layout, например SolutionExplorerBorder, ChatPanel, MainGrid). layout — JSON с полями: margin (объект left,top,right,bottom или массив [L,T,R,B]), grid_row, grid_column, grid_row_span, grid_column_span, canvas_left, canvas_top, dock (Left|Top|Right|Bottom). Применяются только переданные поля. Когда layout устраивает — его можно сохранить (БД/настройки) и восстанавливать при запуске.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Имя контрола." },
                        layout = new { type = "string", description = "JSON объект: margin, grid_row, grid_column, canvas_left, canvas_top, dock и т.д." }
                    },
                    required = new[] { "name", "layout" }
                })
            },
            new()
            {
                Name = "ide_set_control_text",
                Description = "Установить текст в контрол с вводом (TextBox и т.п.). name — имя контрола из ide_get_ui_layout (например TerminalInputBox, поле чата). text — новый текст.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Имя контрола (TextBox или с writable Content)." },
                        text = new { type = "string", description = "Новый текст." }
                    },
                    required = new[] { "name", "text" }
                })
            },
            new()
            {
                Name = "ide_click_control",
                Description = "Клик по контролу. Без name — клик по элементу под курсором (должен быть Button). С name — клик по кнопке с указанным именем (из ide_get_ui_layout). Поддерживается только Button.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { name = new { type = "string", description = "Имя кнопки (опционально; если не задано — элемент под курсором)." } },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_send_keys",
                Description = "Отправить сочетание клавиш в эффективный контрол (под курсором или по имени). keys — текст вида Ctrl+Enter, Alt+F4, Shift+Tab. Модификаторы: Ctrl, Alt, Shift, Meta/Win. Клавиши: Enter, Tab, F4, A и т.д.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Имя контрола (опционально; если не задано — элемент под курсором)." },
                        keys = new { type = "string", description = "Сочетание, например Ctrl+Enter, Alt+F4." }
                    },
                    required = new[] { "keys" }
                })
            },
            new()
            {
                Name = "ide_set_focus",
                Description = "Передать фокус на эффективный контрол: по имени (из ide_get_ui_layout) или на элемент под курсором. После этого клавиши и ввод идут в этот контрол.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { name = new { type = "string", description = "Имя контрола (опционально; если не задано — элемент под курсором)." } },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_highlight_control",
                Description = "Подсветить эффективный контрол рамкой (как в Comet): пользователь видит, где «находится» агент. Без name — элемент под курсором; с name — по имени. Подсветка исчезает через несколько секунд.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { name = new { type = "string", description = "Имя контрола (опционально; если не задано — элемент под курсором)." } },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_set_panel_size",
                Description = "Изменить размер панели (в пикселях). solution_explorer, chat — передать width; build_output, terminal — передать height. Агент может увеличить чат или вывод сборки под длинный текст.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        panel = new { type = "string", description = "Имя панели: solution_explorer, chat, build_output, terminal." },
                        width = new { type = "number", description = "Ширина в пикселях (для solution_explorer, chat)." },
                        height = new { type = "number", description = "Высота в пикселях (для build_output, terminal)." }
                    },
                    required = new[] { "panel" }
                })
            },
            new()
            {
                Name = "ide_get_supported_editor_languages",
                Description = "Список языков редактора с подсветкой синтаксиса. JSON: массив объектов { \"extension\": \".cs\", \"language\": \"C#\" }. Чтобы знать, для каких расширений файлов есть подсветка.",
                InputSchema = Schema(new { type = "object", properties = new { }, required = Array.Empty<string>() })
            },
            new()
            {
                Name = "ide_show_breakpoints",
                Description = "Показать в IDE брейкпоинты отладчика (из debug_set_breakpoints). Агент вызывает после установки брейкпоинтов, чтобы пользователь видел их в редакторе.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        breakpoints = new { type = "array", description = "Массив { file_path, line } (1-based).", items = new { type = "object", properties = new { file_path = new { type = "string" }, line = new { type = "integer" } }, required = new[] { "file_path", "line" } } }
                    },
                    required = new[] { "breakpoints" }
                })
            },
            new()
            {
                Name = "ide_show_debug_position",
                Description = "Показать текущую позицию отладки (файл, строка). IDE откроет файл при необходимости и подсветит строку. Сброс: file_path = null или пустая строка.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        file_path = new { type = "string", description = "Полный путь к файлу (null/пусто = сбросить подсветку)." },
                        line = new { type = "integer", description = "Номер строки (1-based)." }
                    },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_show_debug_state",
                Description = "Показать в панели отладки стек вызовов и переменные (после остановки на брейкпоинте). Агент передаёт данные из debug_stack_trace и debug_variables.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        stack_frames = new { type = "array", description = "Массив { name, file?, line }.", items = new { type = "object", properties = new { name = new { type = "string" }, file = new { type = "string" }, line = new { type = "integer" } } } },
                        variables = new { type = "array", description = "Массив { name, value }.", items = new { type = "object", properties = new { name = new { type = "string" }, value = new { type = "string" } }, required = new[] { "name", "value" } } }
                    },
                    required = Array.Empty<string>()
                })
            }
#if DEBUG
            ,
            new()
            {
                Name = "ide_add_control",
                Description = "[Только Debug-сборка] Добавить контрол в конец панели на лету. parent_name — имя Panel (из ide_get_ui_layout), control_type — Button, TextBlock или Border, content — текст, name — опциональное имя нового контрола. Для экспериментов с UI.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        parent_name = new { type = "string", description = "Имя родительской панели (Panel)." },
                        control_type = new { type = "string", description = "Button, TextBlock или Border." },
                        content = new { type = "string", description = "Текст (Content/Text)." },
                        name = new { type = "string", description = "Имя нового контрола (опционально)." }
                    },
                    required = new[] { "parent_name", "control_type" }
                })
            }
#endif
            ,
            new()
            {
                Name = "ide_write_agent_notes",
                Description = "Записать заметки агента. Агент сам решает, когда, что и в каком формате сохранять (markdown, json, текст). Хранятся в каталоге решения в .cascade-ide/agent-notes. Без открытого решения — ошибка. Для непрерывности между сессиями и до суммаризации.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { content = new { type = "string", description = "Полное содержимое заметок (перезаписывает файл)." } },
                    required = new[] { "content" }
                })
            },
            new()
            {
                Name = "ide_read_agent_notes",
                Description = "Прочитать заметки агента из .cascade-ide/agent-notes в каталоге решения. Возвращает содержимое или пустую строку, если файла нет или решение не загружено. Агент восстанавливает контекст в новом чате.",
                InputSchema = Schema(new { type = "object", properties = new { }, required = Array.Empty<string>() })
            },
            new()
            {
                Name = "ide_execute_command",
                Description = "Выполнить команду IDE по коду. Единая точка входа: command_id (например open_file, load_solution, get_editor_state), args — аргументы команды (те же, что у соответствующих ide_* тулов). Список кодов в IdeCommands. Экономит контекст агента: один тул вместо многих.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        command_id = new { type = "string", description = "Код команды (open_file, load_solution, select, set_breakpoint, show_preview, get_editor_state, apply_edit, go_to_position, build, …)." },
                        args = new { type = "object", description = "Аргументы команды (path, file_path, line, start_line, …). Опционально." }
                    },
                    required = new[] { "command_id" }
                })
            }
        };

        return new McpServerOptions
        {
            ServerInfo = new Implementation { Name = "CascadeIDE", Version = "0.1.0" },
            ProtocolVersion = "2024-11-05",
            Capabilities = new ServerCapabilities { Tools = new ToolsCapability { ListChanged = false } },
            Handlers = new McpServerHandlers
            {
                ListToolsHandler = (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = toolsList }),
                CallToolHandler = async (request, cancellationToken) =>
                {
                    var name = request.Params?.Name ?? "";
                    var args = request.Params?.Arguments is IReadOnlyDictionary<string, JsonElement> a ? a : null;
                    try
                    {
                        var text = name switch
                        {
                            "ide_open_file" => await actions.ExecuteCommandAsync(IdeCommands.OpenFile, args, cancellationToken),
                            "ide_load_solution" => await actions.ExecuteCommandAsync(IdeCommands.LoadSolution, args, cancellationToken),
                            "ide_select" => await actions.ExecuteCommandAsync(IdeCommands.Select, args, cancellationToken),
                            "ide_set_breakpoint" => await actions.ExecuteCommandAsync(IdeCommands.SetBreakpoint, args, cancellationToken),
                            "ide_show_preview" => await actions.ExecuteCommandAsync(IdeCommands.ShowPreview, args, cancellationToken),
                            "ide_show_editor_preview" => await actions.ExecuteCommandAsync(IdeCommands.ShowEditorPreview, args, cancellationToken),
                            "ide_request_confirmation" => await actions.ExecuteCommandAsync(IdeCommands.RequestConfirmation, args, cancellationToken),
                            "ide_get_editor_state" => await actions.ExecuteCommandAsync(IdeCommands.GetEditorState, args, cancellationToken),
                            "ide_get_editor_content_range" => await actions.ExecuteCommandAsync(IdeCommands.GetEditorContentRange, args, cancellationToken),
                            "ide_apply_edit" => await actions.ExecuteCommandAsync(IdeCommands.ApplyEdit, args, cancellationToken),
                            "ide_go_to_position" => await actions.ExecuteCommandAsync(IdeCommands.GoToPosition, args, cancellationToken),
                            "ide_get_solution_info" => await actions.ExecuteCommandAsync(IdeCommands.GetSolutionInfo, args, cancellationToken),
                            "ide_get_solution_files" => await actions.ExecuteCommandAsync(IdeCommands.GetSolutionFiles, args, cancellationToken),
                            "ide_get_current_file_diagnostics" => await actions.ExecuteCommandAsync(IdeCommands.GetCurrentFileDiagnostics, args, cancellationToken),
                            "ide_build" => await actions.ExecuteCommandAsync(IdeCommands.BuildStructured, args, cancellationToken),
                            "ide_get_build_output" => await actions.ExecuteCommandAsync(IdeCommands.GetBuildOutput, args, cancellationToken),
                            "ide_run_tests" => await actions.RunTestsAsync(),
                            "ide_focus_editor" => await actions.ExecuteCommandAsync(IdeCommands.FocusEditor, args, cancellationToken),
                            "ide_get_ui_theme" => await actions.ExecuteCommandAsync(IdeCommands.GetUiTheme, args, cancellationToken),
                            "ide_set_ui_theme" => await actions.ExecuteCommandAsync(IdeCommands.SetUiTheme, args, cancellationToken),
                            "ide_get_ui_layout" => await actions.ExecuteCommandAsync(IdeCommands.GetUiLayout, args, cancellationToken),
                            "ide_get_colors_under_cursor" => await actions.ExecuteCommandAsync(IdeCommands.GetColorsUnderCursor, args, cancellationToken),
                            "ide_get_control_appearance" => await actions.ExecuteCommandAsync(IdeCommands.GetControlAppearance, args, cancellationToken),
                            "ide_set_control_layout" => await actions.ExecuteCommandAsync(IdeCommands.SetControlLayout, args, cancellationToken),
                            "ide_set_control_text" => await actions.ExecuteCommandAsync(IdeCommands.SetControlText, args, cancellationToken),
                            "ide_click_control" => await actions.ExecuteCommandAsync(IdeCommands.ClickControl, args, cancellationToken),
                            "ide_send_keys" => await actions.ExecuteCommandAsync(IdeCommands.SendKeys, args, cancellationToken),
                            "ide_set_focus" => await actions.ExecuteCommandAsync(IdeCommands.SetFocus, args, cancellationToken),
                            "ide_highlight_control" => await actions.ExecuteCommandAsync(IdeCommands.HighlightControl, args, cancellationToken),
                            "ide_set_panel_size" => await actions.ExecuteCommandAsync(IdeCommands.SetPanelSize, args, cancellationToken),
                            "ide_get_supported_editor_languages" => await actions.ExecuteCommandAsync(IdeCommands.GetSupportedEditorLanguages, args, cancellationToken),
                            "ide_show_breakpoints" => await actions.ExecuteCommandAsync(IdeCommands.ShowBreakpoints, args, cancellationToken),
                            "ide_show_debug_position" => await actions.ExecuteCommandAsync(IdeCommands.ShowDebugPosition, args, cancellationToken),
                            "ide_show_debug_state" => await actions.ExecuteCommandAsync(IdeCommands.ShowDebugState, args, cancellationToken),
#if DEBUG
                            "ide_add_control" => await actions.ExecuteCommandAsync(IdeCommands.AddControl, args, cancellationToken),
#endif
                            "ide_write_agent_notes" => await actions.WriteAgentNotesAsync(args?.TryGetValue("content", out var c) == true ? c.GetString() ?? "" : "", cancellationToken),
                            "ide_read_agent_notes" => await actions.ReadAgentNotesAsync(cancellationToken),
                            "ide_execute_command" => await CallExecuteCommand(actions, args, cancellationToken),
                            _ => $"Unknown tool: {name}"
                        };
                        // Тулы-действия при ошибке возвращают текст (не "OK") — помечаем как IsError, чтобы агент видел сбой.
                        bool isError;
                        if (name == "ide_execute_command")
                            isError = text.StartsWith("Missing", StringComparison.Ordinal) || text.StartsWith("Unknown command", StringComparison.Ordinal) || text.StartsWith("Error", StringComparison.Ordinal);
                        else
                        {
                        var isActionTool = name is "ide_open_file" or "ide_load_solution" or "ide_select" or "ide_set_breakpoint"
                            or "ide_show_preview" or "ide_show_editor_preview" or "ide_apply_edit" or "ide_go_to_position" or "ide_focus_editor"
                            or "ide_set_ui_theme" or "ide_set_control_layout" or "ide_set_control_text" or "ide_click_control"
                            or "ide_send_keys" or "ide_set_focus" or "ide_highlight_control" or "ide_set_panel_size" or "ide_add_control"
                            or "ide_show_breakpoints" or "ide_show_debug_position" or "ide_show_debug_state" or "ide_write_agent_notes";
                        isError = isActionTool && text != "OK";
                        }
                        return new CallToolResult { Content = [new TextContentBlock { Text = text }], IsError = isError };
                    }
                    catch (Exception ex)
                    {
                        return new CallToolResult { Content = [new TextContentBlock { Text = "Error: " + ex.Message }], IsError = true };
                    }
                }
            }
        };
    }

    private static async Task<string> CallExecuteCommand(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args, CancellationToken cancellationToken)
    {
        var commandId = args is not null && args.TryGetValue("command_id", out var cid) ? cid.GetString() : null;
        if (string.IsNullOrEmpty(commandId))
            return "Missing command_id";
        return await actions.ExecuteCommandAsync(commandId, args, cancellationToken);
    }

}

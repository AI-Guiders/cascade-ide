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
                Description = "Получить состояние редактора: открытый файл, каретка (line/column), выделение (start, length, text). JSON.",
                InputSchema = Schema(new { type = "object", properties = new { }, required = Array.Empty<string>() })
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
                Description = "Информация о решении: solution_path, current_file_path, project_paths. JSON.",
                InputSchema = Schema(new { type = "object", properties = new { }, required = Array.Empty<string>() })
            },
            new()
            {
                Name = "ide_build",
                Description = "Запустить сборку решения (dotnet build). Вернёт вывод.",
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
                            "ide_open_file" => CallOpenFile(actions, args),
                            "ide_load_solution" => CallLoadSolution(actions, args),
                            "ide_select" => CallSelect(actions, args),
                            "ide_set_breakpoint" => CallSetBreakpoint(actions, args),
                            "ide_show_preview" => CallShowPreview(actions, args),
                            "ide_request_confirmation" => await CallRequestConfirmation(actions, args, cancellationToken),
                            "ide_get_editor_state" => await actions.GetEditorStateAsync(),
                            "ide_apply_edit" => CallApplyEdit(actions, args),
                            "ide_go_to_position" => CallGoToPosition(actions, args),
                            "ide_get_solution_info" => actions.GetSolutionInfo(),
                            "ide_build" => await actions.BuildAsync(),
                            "ide_get_build_output" => actions.GetBuildOutput(),
                            "ide_focus_editor" => CallFocusEditor(actions),
                            "ide_get_ui_theme" => actions.GetUiTheme(),
                            "ide_set_ui_theme" => await CallSetUiTheme(actions, args),
                            "ide_get_ui_layout" => await actions.GetUiLayoutAsync(),
                            "ide_get_colors_under_cursor" => await actions.GetColorsUnderCursorAsync(),
                            "ide_get_control_appearance" => await actions.GetControlAppearanceAsync(args is not null && args.TryGetValue("name", out var n) ? n.GetString() : null),
                            "ide_set_control_layout" => await CallSetControlLayout(actions, args),
                            "ide_set_control_text" => await actions.SetControlTextAsync(args is not null && args.TryGetValue("name", out var stn) ? stn.GetString() ?? "" : "", args is not null && args.TryGetValue("text", out var stt) ? stt.GetString() ?? "" : ""),
                            "ide_click_control" => await actions.ClickControlAsync(args is not null && args.TryGetValue("name", out var ccn) ? ccn.GetString() : null),
                            "ide_send_keys" => await actions.SendKeysAsync(args is not null && args.TryGetValue("name", out var skn) ? skn.GetString() : null, args is not null && args.TryGetValue("keys", out var skk) ? skk.GetString() ?? "" : ""),
                            "ide_set_focus" => await actions.SetFocusAsync(args is not null && args.TryGetValue("name", out var sfn) ? sfn.GetString() : null),
                            "ide_highlight_control" => await actions.HighlightControlAsync(args is not null && args.TryGetValue("name", out var hcn) ? hcn.GetString() : null),
                            "ide_set_panel_size" => await actions.SetPanelSizeAsync(
                                args is not null && args.TryGetValue("panel", out var pn) ? pn.GetString() ?? "" : "",
                                args is not null && args.TryGetValue("width", out var pw) && pw.TryGetDouble(out var w) ? w : null,
                                args is not null && args.TryGetValue("height", out var ph) && ph.TryGetDouble(out var h) ? h : null),
#if DEBUG
                            "ide_add_control" => await actions.AddControlAsync(
                                args is not null && args.TryGetValue("parent_name", out var pn) ? pn.GetString() ?? "" : "",
                                args is not null && args.TryGetValue("control_type", out var ct) ? ct.GetString() ?? "" : "",
                                args is not null && args.TryGetValue("content", out var cnt) ? cnt.GetString() : null,
                                args is not null && args.TryGetValue("name", out var nm) ? nm.GetString() : null),
#endif
                            _ => $"Unknown tool: {name}"
                        };
                        // Тулы-действия при ошибке возвращают текст (не "OK") — помечаем как IsError, чтобы агент видел сбой.
                        var isActionTool = name is "ide_open_file" or "ide_load_solution" or "ide_select" or "ide_set_breakpoint"
                            or "ide_show_preview" or "ide_apply_edit" or "ide_go_to_position" or "ide_focus_editor"
                            or "ide_set_ui_theme" or "ide_set_control_layout" or "ide_set_control_text" or "ide_click_control"
                            or "ide_send_keys" or "ide_set_focus" or "ide_highlight_control" or "ide_set_panel_size" or "ide_add_control";
                        var isError = isActionTool && text != "OK";
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

    private static string CallOpenFile(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args)
    {
        var path = args is not null && args.TryGetValue("path", out var p) ? p.GetString() : null;
        if (string.IsNullOrEmpty(path))
            return "Missing path";
        actions.OpenFile(path);
        return "OK";
    }

    private static string CallLoadSolution(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args)
    {
        var path = args is not null && args.TryGetValue("path", out var p) ? p.GetString() : null;
        if (string.IsNullOrEmpty(path))
            return "Missing path";
        actions.LoadSolution(path);
        return "OK";
    }

    private static string CallSelect(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args)
    {
        if (args is null || !args.TryGetValue("file_path", out var fp))
            return "Missing file_path";
        var filePath = fp.GetString();
        if (string.IsNullOrEmpty(filePath))
            return "Missing file_path";
        if (!args.TryGetValue("start_line", out var sl) || !args.TryGetValue("start_column", out var sc)
            || !args.TryGetValue("end_line", out var el) || !args.TryGetValue("end_column", out var ec))
            return "Missing start_line, start_column, end_line or end_column";
        actions.SelectInEditor(filePath, sl.GetInt32(), sc.GetInt32(), el.GetInt32(), ec.GetInt32());
        return "OK";
    }

    private static string CallSetBreakpoint(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args)
    {
        if (args is null || !args.TryGetValue("file_path", out var fp) || !args.TryGetValue("line", out var ln))
            return "Missing arguments";
        var filePath = fp.GetString();
        var line = ln.GetInt32();
        var condition = args.TryGetValue("condition", out var c) ? c.GetString() : null;
        if (string.IsNullOrEmpty(filePath))
            return "Missing file_path";
        actions.SetBreakpoint(filePath, line, condition);
        return "OK";
    }

    private static string CallShowPreview(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args)
    {
        if (args is null)
            return "Missing arguments";
        var title = args.TryGetValue("title", out var t) ? t.GetString() ?? "" : "";
        var content = args.TryGetValue("content", out var c) ? c.GetString() ?? "" : "";
        actions.ShowPreview(title, content);
        return "OK";
    }

    private static async Task<string> CallRequestConfirmation(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args, CancellationToken ct)
    {
        var message = args is not null && args.TryGetValue("message", out var m) ? m.GetString() ?? "" : "";
        return await actions.RequestConfirmationAsync(message, ct);
    }

    private static string CallApplyEdit(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args)
    {
        if (args is null || !args.TryGetValue("file_path", out var fp) || !args.TryGetValue("start_line", out var sl)
            || !args.TryGetValue("start_column", out var sc) || !args.TryGetValue("end_line", out var el)
            || !args.TryGetValue("end_column", out var ec) || !args.TryGetValue("new_text", out var nt))
            return "Missing arguments";
        var filePath = fp.GetString();
        var newText = nt.GetString() ?? "";
        if (string.IsNullOrEmpty(filePath))
            return "Missing file_path";
        actions.ApplyEdit(filePath, sl.GetInt32(), sc.GetInt32(), el.GetInt32(), ec.GetInt32(), newText);
        return "OK";
    }

    private static string CallGoToPosition(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args)
    {
        if (args is null || !args.TryGetValue("file_path", out var fp) || !args.TryGetValue("line", out var ln) || !args.TryGetValue("column", out var col))
            return "Missing file_path, line or column";
        var filePath = fp.GetString();
        var line = ln.GetInt32();
        var column = col.GetInt32();
        int? endLine = args.TryGetValue("end_line", out var el) ? el.GetInt32() : null;
        int? endColumn = args.TryGetValue("end_column", out var ec) ? ec.GetInt32() : null;
        actions.GoToPosition(filePath, line, column, endLine, endColumn);
        return "OK";
    }

    private static string CallFocusEditor(IIdeMcpActions actions)
    {
        actions.FocusEditor();
        return "OK";
    }

    private static async Task<string> CallSetUiTheme(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args)
    {
        if (args is null || !args.TryGetValue("theme", out var themeEl))
            return "Missing argument: theme (JSON string)";
        var themeJson = themeEl.GetString() ?? "";
        return await actions.SetUiThemeAsync(themeJson);
    }

    private static async Task<string> CallSetControlLayout(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args)
    {
        if (args is null || !args.TryGetValue("name", out var nameEl) || !args.TryGetValue("layout", out var layoutEl))
            return "Missing name or layout";
        var controlName = nameEl.GetString() ?? "";
        var layoutJson = layoutEl.GetString() ?? "{}";
        return await actions.SetControlLayoutAsync(controlName, layoutJson);
    }
}

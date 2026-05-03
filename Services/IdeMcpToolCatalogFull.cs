using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace CascadeIDE.Services;

internal static class IdeMcpToolCatalogFull
{
    private static JsonElement Schema(object schema) => JsonSerializer.SerializeToElement(schema);

    /// <summary>Общая схема <c>{ "type": "object", "properties": {}, "required": [] }</c> для инструментов без аргументов.</summary>
    private static readonly JsonElement s_emptyObjectInputSchema = Schema(new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    });

    // CA1861: кэш для повторяющихся массивов имён required в схемах (каталог строится редко, но анализатор требует).
    private static readonly string[] s_reqPath = ["path"];
    private static readonly string[] s_reqFileSelectRange = ["file_path", "start_line", "start_column", "end_line", "end_column"];
    private static readonly string[] s_reqFilePathLine = ["file_path", "line"];
    private static readonly string[] s_reqTitleContent = ["title", "content"];
    private static readonly string[] s_reqMessage = ["message"];
    private static readonly string[] s_reqStartLineEndLine = ["start_line", "end_line"];
    private static readonly string[] s_reqEditChunk = ["file_path", "start_line", "start_column", "end_line", "end_column", "new_text"];
    private static readonly string[] s_reqFilePathLineColumn = ["file_path", "line", "column"];
    private static readonly string[] s_reqRev = ["rev"];
    private static readonly string[] s_reqTheme = ["theme"];
    private static readonly string[] s_reqNameLayout = ["name", "layout"];
    private static readonly string[] s_reqNameText = ["name", "text"];
    private static readonly string[] s_reqKeys = ["keys"];
    private static readonly string[] s_reqPanel = ["panel"];
    private static readonly string[] s_reqContent = ["content"];
    private static readonly string[] s_reqQuery = ["query"];
    private static readonly string[] s_reqUrl = ["url"];
    private static readonly string[] s_reqCommandId = ["command_id"];
    private static readonly string[] s_reqParentNameControlType = ["parent_name", "control_type"];

    private static List<Tool> CreateStandardTools()
    {
        var t = new List<Tool>();
        AddFileEditorAndWorkspaceQueryTools(t);
        AddBuildTestMetricsTools(t);
        AddGitTools(t);
        AddUiAndThemeTools(t);
        AddDebugAndAgentCommandTools(t);
        return t;
    }

    /// <summary>Файл, решение, дерево, диагностика по текущему файлу.</summary>
    private static void AddFileEditorAndWorkspaceQueryTools(List<Tool> t) =>
        t.AddRange(
        [
            new()
            {
                Name = "ide_open_file",
                Description = "Открыть файл в редакторе IDE по пути.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { path = new { type = "string", description = "Полный путь к файлу." } },
                    required = s_reqPath
                })
            },
            new()
            {
                Name = "ide_load_solution",
                Description = "Загрузить workspace: решение (.sln/.slnx/.slnf), один проект (.csproj/.fsproj) или каталог. Дерево в обозревателе обновится.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { path = new { type = "string", description = "Полный путь к .sln/.slnx/.slnf, к .csproj/.fsproj или к каталогу." } },
                    required = s_reqPath
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
                    required = s_reqFileSelectRange
                })
            },
            new()
            {
                Name = "ide_set_breakpoint",
                Description = "Поставить брейкпоинт: при необходимости загрузить решение (.sln/.slnx/.slnf) над файлом, записать точку для dotnet-debug-mcp, открыть файл и перейти к строке (точка видна в редакторе).",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        file_path = new { type = "string" },
                        line = new { type = "integer", description = "Номер строки (1-based)." },
                        condition = new { type = "string", description = "Опциональное условие." }
                    },
                    required = s_reqFilePathLine
                })
            },
            new()
            {
                Name = "ide_remove_breakpoint",
                Description = "Снять брейкпоинт в файле на указанной строке.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        file_path = new { type = "string" },
                        line = new { type = "integer", description = "Номер строки (1-based)." }
                    },
                    required = s_reqFilePathLine
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
                    required = s_reqTitleContent
                })
            },
            new()
            {
                Name = "ide_show_editor_preview",
                Description = "Показать превью текущего файла из редактора в отдельном окне. Контент берётся из IDE (не передаётся по MCP) — удобно для длинных .md с таблицами. Если открыт не .md — окно покажет текущий текст редактора.",
                InputSchema = s_emptyObjectInputSchema
            },
            new()
            {
                Name = "ide_request_confirmation",
                Description = "Запросить подтверждение у пользователя. Возвращает ответ пользователя (ok/cancel или текст).",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { message = new { type = "string" } },
                    required = s_reqMessage
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
                    required = s_reqStartLineEndLine
                })
            },
            new()
            {
                Name = "ide_get_open_document_text",
                Description = "Полный текст открытой вкладки из модели документа (все вкладки из DockDocuments, не только активная). JSON: file_path, length, truncated, is_dirty, text. Без file_path — текущий файл. max_chars — опционально, обрезать text и выставить truncated. Если файл не открыт: error, message.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        file_path = new { type = "string", description = "Полный путь к файлу вкладки. Пусто/нет — текущий открытый файл." },
                        max_chars = new { type = "integer", description = "Максимум символов в text (>0). Без параметра — без обрезки." }
                    },
                    required = Array.Empty<string>()
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
                    required = s_reqEditChunk
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
                    required = s_reqFilePathLineColumn
                })
            },
            new()
            {
                Name = "ide_get_solution_info",
                Description = "Информация о решении: solution_path, current_file_path, project_paths, selected_solution_path (путь узла, выделенного в обозревателе). JSON.",
                InputSchema = s_emptyObjectInputSchema
            },
            new()
            {
                Name = "ide_get_ide_state",
                Description = "Одна сводка состояния IDE: solution/current file/selection/debug/build output/diagnostics и cockpit_surface (CDS, тот же снимок, что BuildCockpitSurfaceSnapshot/Skia). JSON.",
                InputSchema = s_emptyObjectInputSchema
            },
            new()
            {
                Name = "ide_get_ui_modes_diagnostics",
                Description = "Диагностика загрузки UI-режимов: app_base_directory, путь к UiModes, наличие index.toml/Flight.toml, bundle_source (TomlBundle vs BuiltinRegistry), ordered_mode_ids, builtin_registry_fallback_ids, flight_listed_in_menu, hint (если Flight нет в меню).",
                InputSchema = s_emptyObjectInputSchema
            },
            new()
            {
                Name = "ide_get_solution_files",
                Description = "Файлы и дерево решения. file_entries — массив { path, title, relative_path } (relative_path от каталога решения). solution_tree — иерархия (solution → projects → folders → files) с теми же полями. Для поиска .md или узла по пути и открытия через ide_open_file.",
                InputSchema = s_emptyObjectInputSchema
            },
            new()
            {
                Name = "ide_search_web_public_query",
                Description =
                    "Краткая справка из интернета (HTTPS, DuckDuckGo Instant Answer: краткий abstract и связанные темы). Не полнотекстовый поисковик и не истина по умолчанию: дополнять фактами только после чтения JSON. Запрос уходит на duckduckgo.com — учитывай приватность. Без сети вернётся offline_or_error. Для содержимого репозитория — ide_search_workspace_text.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { query = new { type = "string", description = "Поисковая строка или вопрос (по-русски или по-английски)." } },
                    required = s_reqQuery
                })
            },
            new()
            {
                Name = "ide_fetch_web_public_url",
                Description =
                    "Загрузить документ по публичному HTTPS URL и вернуть читаемый текст (аналог Cursor Fetch). HTML упрощается до текста; JSON/XML/обычный текст — как UTF-8. Запрос уходит из машины оператора (приватность, корпоративные ограничения). Только https; локальные и частные IP/localhost блокируются поверхностно (не замена корпоративного egress-фильтра). Ответ ограничен по размеру скачанного тела и по max_chars. Для общих формулировок без конкретного URL — ide_search_web_public_query.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        url = new { type = "string", description = "Абсолютный https URL страницы или сырья (docs, спецификация)." },
                        max_chars = new { type = "integer", description = "Максимум символов в поле text после извлечения (по умолчанию 200000, максимум 1000000)." }
                    },
                    required = s_reqUrl
                })
            },
            new()
            {
                Name = "ide_get_current_file_diagnostics",
                Description = "Диагностики (ошибки и предупреждения) по текущему открытому файлу. Только .cs; для остальных — []. JSON: массив { id, message, severity, line, column } (line/column 1-based). Live-анализ Roslyn по содержимому редактора.",
                InputSchema = s_emptyObjectInputSchema
            }
        ]);

    private static void AddBuildTestMetricsTools(List<Tool> t) =>
        t.AddRange(
        [
            new()
            {
                Name = "ide_build",
                Description = "Запустить сборку решения (dotnet build). Возвращает JSON: success, exit_code, errors[] (file, line, column?, code?, message), warnings[], raw_output (обрезано до 4000 символов). Агент получает структурированные ошибки без парсинга лога.",
                InputSchema = s_emptyObjectInputSchema
            },
            new()
            {
                Name = "ide_get_build_output",
                Description = "Текущее содержимое панели «Вывод сборки»: полный текст вывода и цвета оформления (background, foreground). JSON: text, theme. Чтобы агент видел, что сейчас отображается в панели сборки.",
                InputSchema = s_emptyObjectInputSchema
            },
            new()
            {
                Name = "ide_run_tests",
                Description = "Запустить тесты решения (dotnet test; при необходимости выполняет сборку). Возвращает JSON: success, total, passed, failed, skipped, failed_tests[] (name, message?, duration_ms?). Агент получает структурированный список упавших тестов без парсинга лога.",
                InputSchema = s_emptyObjectInputSchema
            },
            new()
            {
                Name = "ide_run_affected_tests",
                Description = "Запустить затронутые тесты по changed_paths (фильтр FullyQualifiedName~...). Если список пустой или не извлечены тестовые токены — автоматически fallback на полный ide_run_tests. Возвращает JSON: success, total, passed, failed, skipped, failed_tests[], mode, filter.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        changed_paths = new
                        {
                            type = "array",
                            items = new { type = "string" },
                            description = "Опционально: список изменённых путей/файлов для вычисления фильтра затронутых тестов."
                        }
                    },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_run_code_cleanup",
                Description = "Запустить code cleanup через dotnet format для текущего решения. Опционально include_path — точечный файл/путь внутри решения. Возвращает JSON: success, exit_code, raw_output.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        include_path = new { type = "string", description = "Опционально: полный путь к файлу/пути для точечной чистки через --include." }
                    },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_get_code_metrics",
                Description = "Метрики кода (LOC, class_count, method_count, cyclomatic complexity). scope: current_file/file/path/solution; path — опционально для file/path. Возвращает JSON по файлам и агрегаты.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        scope = new { type = "string", description = "current_file|file|path|solution (по умолчанию current_file)." },
                        path = new { type = "string", description = "Опционально: путь к файлу/каталогу (для scope=file/path)." }
                    },
                    required = Array.Empty<string>()
                })
            }
        ]);

    private static void AddGitTools(List<Tool> t) =>
        t.AddRange(
        [
            new()
            {
                Name = "ide_git_status",
                Description = "Git status (branch + short status) в каталоге решения/workspace. JSON: success, exit_code, output.",
                InputSchema = s_emptyObjectInputSchema
            },
            new()
            {
                Name = "ide_git_diff",
                Description = "Git diff в каталоге решения/workspace. Опционально path и staged=true. JSON: success, exit_code, output (обрезано).",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "Опционально: путь к файлу/каталогу для ограничения diff." },
                        staged = new { type = "boolean", description = "true: git diff --staged; false (по умолчанию): рабочие изменения." }
                    },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_git_commit",
                Description = "Сделать git commit в каталоге решения/workspace. message обязателен; paths — опционально (иначе add -A). JSON: success, exit_code, output.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        message = new { type = "string", description = "Сообщение коммита." },
                        paths = new { type = "array", items = new { type = "string" }, description = "Опционально: пути для git add." }
                    },
                    required = s_reqMessage
                })
            },
            new()
            {
                Name = "ide_git_push",
                Description = "Сделать git push в каталоге решения/workspace. Опционально remote и branch (как git push без лишних аргументов). dry_run=true — git push --dry-run (без отправки). JSON: success, exit_code, output.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        remote = new { type = "string", description = "Опционально: remote." },
                        branch = new { type = "string", description = "Опционально: branch." },
                        dry_run = new { type = "boolean", description = "true — только предпросмотр (--dry-run)." }
                    },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_git_log",
                Description = "Git log -n N --oneline в workspace. JSON: success, exit_code, output.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { n = new { type = "integer", description = "Число коммитов (по умолчанию 20, макс. 500)." } },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_git_fetch",
                Description = "Git fetch в workspace. Опционально remote, all, prune. dry_run=true — git fetch --dry-run. JSON: success, exit_code, output.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        remote = new { type = "string", description = "Опционально: remote (не сочетать с all=true)." },
                        all = new { type = "boolean", description = "true — fetch --all." },
                        prune = new { type = "boolean", description = "true — --prune." },
                        dry_run = new { type = "boolean", description = "true — только предпросмотр (--dry-run)." }
                    },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_git_pull",
                Description = "Git pull в workspace. Оба remote+branch или ни одного; ff_only по умолчанию true. dry_run=true — git pull --dry-run (Git 2.27+). JSON: success, exit_code, output.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        remote = new { type = "string", description = "Вместе с branch или оба пустые." },
                        branch = new { type = "string", description = "Вместе с remote." },
                        ff_only = new { type = "boolean", description = "По умолчанию true (--ff-only)." },
                        dry_run = new { type = "boolean", description = "true — только предпросмотр (--dry-run)." }
                    },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_git_branch",
                Description = "Git branch: list (-vv), create, delete. JSON: success, exit_code, output.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        action = new { type = "string", description = "list | create | delete (по умолчанию list)." },
                        name = new { type = "string", description = "Для create/delete." },
                        start_point = new { type = "string", description = "Опционально для create." },
                        force = new { type = "boolean", description = "Для delete: -D." }
                    },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_git_show",
                Description = "Git show rev; опционально path, stat_only. JSON: success, exit_code, output.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        rev = new { type = "string", description = "Ревизия (обязательно)." },
                        path = new { type = "string", description = "Опционально: файл в ревизии." },
                        stat_only = new { type = "boolean", description = "Только --stat." }
                    },
                    required = s_reqRev
                })
            },
            new()
            {
                Name = "ide_git_submodule",
                Description = "Git submodule status или update --init. JSON: success, exit_code, output.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        action = new { type = "string", description = "status | update (по умолчанию status)." },
                        path = new { type = "string", description = "Опционально для update." },
                        recursive = new { type = "boolean", description = "Для update: по умолчанию true." }
                    },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_git_preflight",
                Description = "Git preflight в workspace: классификация изменений на semantic/whitespace-only/eol-only/bom-only + safe fix suggestions. JSON.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        staged = new { type = "boolean", description = "true — анализ staged изменений." },
                        include_untracked = new { type = "boolean", description = "true (по умолчанию) — включать untracked_files." },
                        include_patches = new { type = "boolean", description = "true (по умолчанию) — BOM-only эвристика по патчам." }
                    },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_git_preflight_fix_safe",
                Description = "Git preflight safe-fix: применить git add --renormalize . и вернуть обновлённую классификацию. JSON.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        include_patches = new { type = "boolean", description = "true (по умолчанию) — BOM-only эвристика по патчам после фикса." }
                    },
                    required = Array.Empty<string>()
                })
            }
        ]);

    private static void AddUiAndThemeTools(List<Tool> t) =>
        t.AddRange(
        [
            new()
            {
                Name = "ide_focus_editor",
                Description = "Передать фокус в редактор.",
                InputSchema = s_emptyObjectInputSchema
            },
            new()
            {
                Name = "ide_get_ui_theme",
                Description = "Полный снимок темы и лэйаута. Ресурсы: секции как у ide_set_ui_theme + solution_explorer_tree_power + power_island_frame_brushes. Дополнительно: cascade_theme_resolved — все ключи CascadeTheme.*, разрешённые через TryGetResource под actual_theme_variant (solid и linear(...) для градиентов); window_frame — заголовок, client/bounds, extend_client_area*, transparency, фон окна; layout_regions — по именам (RootWindow, MainGrid, DockIslandInner, DocumentsDock, SolutionIslandInner, ChatIslandInner, ChatPanelRoot, BottomPanelShell, ModeBadge, UiModeBloomOverlay, ChatInputBox, TerminalInputBox): bounds, видимость, effective_*, background_brush/border_brush_display (в т.ч. градиенты), corner_radius/box_shadow для Border; dock_open_documents — все открытые вкладки из VM (tab_index, file_path, dock_title, display_title, is_active, is_dirty, model_content_length, model_text_preview ~240 симв., editor_in_visual_tree); dock_text_editors — только вкладки, у которых TextEditor уже в визуальном дереве (часто одна активная): file_path, dock_title, matches_main_window_current_file, document_length, model_content_length, length_matches_model, line_count, bounds, шрифт, кисти, effective_*, text_preview ~240 симв.; top_levels — все открытые окна процесса (role: main|mfd_host|other, window_type, title, position_x/y, client_width/height, window_state, is_active). ide_set_ui_theme игнорирует новые корневые ключи.",
                InputSchema = s_emptyObjectInputSchema
            },
            new()
            {
                Name = "ide_set_ui_theme",
                Description = "Применить тему UI на лету. Берутся только известные секции (main_window…power_cockpit); игнорируются: _snapshot, solution_explorer_tree_power, power_island_frame_brushes, cascade_theme_resolved, window_frame, layout_regions, dock_open_documents, dock_text_editors, top_levels. Градиенты рамок островов задаются в App.axaml, не через set.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { theme = new { type = "string", description = "JSON темы (…, panel_chrome: title_foreground, accent_brush, header_background, header_separator, menu_glyph_foreground — полоса заголовка панелей)." } },
                    required = s_reqTheme
                })
            },
            new()
            {
                Name = "ide_get_ui_layout",
                Description = "Дерево UI по всем окнам верхнего уровня: JSON с массивом windows — у каждого элемента role (main|mfd_host|other), window_type, title, is_active, root (то же дерево контролов: тип, имя, bounds относительно окна, content, children). Окно-хост зоны Mfd (`MfdHostWindow`) и прочие Window входят в ответ; кнопки/панели главного окна как раньше.",
                InputSchema = s_emptyObjectInputSchema
            },
            new()
            {
                Name = "ide_get_colors_under_cursor",
                Description = "Получить цвета под курсором: прямые (background, foreground) и эффективные (effective_background, effective_foreground — с учётом поддерева и предков, как на экране). JSON: type, name, background, foreground, effective_background, effective_foreground (hex #AARRGGBB).",
                InputSchema = s_emptyObjectInputSchema
            },
            new()
            {
                Name = "ide_get_control_appearance",
                Description = "Универсальный снимок любого контрола: прямой и эффективный цвет фона/текста (effective_*), содержимое, границы, видимость, шрифт, рамка, content_truncated (для TextBlock — из реального TextLayout контрола, HasOverflowed; иначе — оценка по измерению). Для Border: corner_radius, box_shadow; background_brush и border_brush_display — кисть строкой (в т.ч. linear(...)). Без аргументов — под курсором; с name — по имени. JSON: type, name, bounds, visible, content, content_truncated, background, foreground, effective_*, border_*, font_*, background_brush, border_brush_display.",
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
                    required = s_reqNameLayout
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
                    required = s_reqNameText
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
                    required = s_reqKeys
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
                Name = "ide_capture_window",
                Description = "Снимок окон IDE в PNG (base64 в JSON). По умолчанию — главное окно; при scope=all — все top-level окна (в т.ч. окно-хост Mfd), ответ с полем windows[] и role у каждого. workspace_path — корень workspace для относительного output_path; в output_path можно использовать {n} для нумерации файлов (stem-0.png, stem-1.png, …).",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        scope = new { type = "string", description = "Опционально: all — снять все окна; иначе или пусто — только главное окно." },
                        workspace_path = new { type = "string", description = "Корень workspace (для разрешения output_path)." },
                        output_path = new { type = "string", description = "Относительный путь для сохранения PNG; {n} — номер окна при scope=all." }
                    },
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
                    required = s_reqPanel
                })
            },
            new()
            {
                Name = "ide_get_supported_editor_languages",
                Description = "Список языков редактора с подсветкой синтаксиса. JSON: массив объектов { \"extension\": \".cs\", \"language\": \"C#\" }. Чтобы знать, для каких расширений файлов есть подсветка.",
                InputSchema = s_emptyObjectInputSchema
            }
        ]);

    private static void AddDebugAndAgentCommandTools(List<Tool> t) =>
        t.AddRange(
        [
            new()
            {
                Name = "ide_get_debug_snapshot",
                Description = "JSON: канонический снимок DAP (стек, переменные, останов, брейкпоинты из storage в снимке) — тот же источник, что UI; заменяет синтетические ide_show_debug_* (ADR 0002).",
                InputSchema = s_emptyObjectInputSchema
            },
            new()
            {
                Name = "ide_write_agent_notes",
                Description = "Записать заметки агента. Агент сам решает, когда, что и в каком формате сохранять (markdown, json, текст). Хранятся в каталоге решения в .cascade-ide/agent-notes. Без открытого решения — ошибка. Для непрерывности между сессиями и до суммаризации.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { content = new { type = "string", description = "Полное содержимое заметок (перезаписывает файл)." } },
                    required = s_reqContent
                })
            },
            new()
            {
                Name = "ide_read_agent_notes",
                Description = "Прочитать заметки агента из .cascade-ide/agent-notes в каталоге решения. Возвращает содержимое или пустую строку, если файла нет или решение не загружено. Агент восстанавливает контекст в новом чате.",
                InputSchema = s_emptyObjectInputSchema
            },
            new()
            {
                Name = "ide_execute_command",
                Description = "Выполнить команду IDE по коду. command_id — как в IdeCommands (в т.ч. паритет с меню/тулбаром: open_solution_dialog, apply_light_theme, build_solution_ui, send_chat, explain_trace_step+step_index, …). args — плоские поля (path, file_path, visible, mode, culture, message, step_index, …). Полная таблица: docs/MCP-PROTOCOL.md.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        command_id = new { type = "string", description = "Код команды: см. IdeCommands и docs/MCP-PROTOCOL.md (меню «Вид», Файл, тулбар, чат, трасса)." },
                        args = new { type = "object", description = "Аргументы команды (path, file_path, line, start_line, …). Опционально." }
                    },
                    required = s_reqCommandId
                })
            }
        ]);

#if DEBUG
    private static void AddDebugOnlyExperimentalControlTool(List<Tool> tools) =>
        tools.Add(new()
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
                required = s_reqParentNameControlType
            })
        });
#endif

    public static List<Tool> BuildRichTools(bool includeDebugTools)
    {
        var toolsList = CreateStandardTools();
#if DEBUG
        if (includeDebugTools)
            AddDebugOnlyExperimentalControlTool(toolsList);
#endif
        return toolsList;
    }
}

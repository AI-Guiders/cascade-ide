using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace CascadeIDE.Services;

internal static partial class IdeMcpToolCatalogFull
{
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
                Name = "ide_read_workspace_file",
                Description = "Прочитать текст файла workspace с диска (в т.ч. если вкладка не открыта). JSON: file_path, length, text, truncated; offset/limit — строки 1-based.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        file_path = new { type = "string" },
                        offset = new { type = "integer", description = "Первая строка (1-based)." },
                        limit = new { type = "integer", description = "Число строк." },
                        max_chars = new { type = "integer", description = "Обрезка текста." }
                    },
                    required = new[] { "file_path" }
                })
            },
            new()
            {
                Name = "ide_save_document",
                Description = "Сохранить на диск: без content — буфер открытой вкладки; с content — полная замена файла (создаёт при отсутствии). JSON: file_path, bytes.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        file_path = new { type = "string", description = "Путь; пусто — текущий открытый файл (только для save буфера)." },
                        content = new { type = "string", description = "Полное содержимое для записи." }
                    },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_apply_edit",
                Description = "Применить правку: заменить диапазон (1-based) в модели документа; открывает файл при необходимости; любая открытая вкладка.",
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
                Name = "ide_reveal_editor_range",
                Description = "Показать диапазон строк transient-подсветкой без изменения selection (ADR 0130). Строки 1-based или re-resolve member_key/syntax_scope (Roslyn). duration_ms опционально (250–120000).",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        file_path = new { type = "string" },
                        start_line = new { type = "integer" },
                        end_line = new { type = "integer" },
                        member_key = new { type = "string", description = "Documentation comment id или простое имя члена." },
                        syntax_scope = new { type = "object", description = "{ kind, indexInParent, parentMemberKey? }" },
                        duration_ms = new { type = "integer" }
                    },
                    required = new[] { "file_path" }
                })
            },
            new()
            {
                Name = "ide_intercom_connect_team",
                Description = "OAuth Connect к team Intercom transport (ADR 0144).",
                InputSchema = s_emptyObjectInputSchema
            },
            new()
            {
                Name = "ide_intercom_disconnect_team",
                Description = "Disconnect Intercom team transport.",
                InputSchema = s_emptyObjectInputSchema
            },
            new()
            {
                Name = "ide_forge_lens_connect",
                Description = "Device login к Agent Forge для CRS Lens (ADR 0158). base_url из [workspace.forge] если не задан.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { base_url = new { type = "string" } },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_forge_lens_disconnect",
                Description = "Удалить Forge Lens credentials для forge host.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { base_url = new { type = "string" } },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_forge_lens_auth_status",
                Description = "Статус Forge Lens auth (CIDE secrets / ~/.forge).",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { base_url = new { type = "string" } },
                    required = Array.Empty<string>()
                })
            },
            new()
            {
                Name = "ide_forge_lens_create_issue",
                Description = "Создать issue в Agent Forge (write gate). base_url/repo из [workspace.forge] если не заданы.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        title = new { type = "string" },
                        body = new { type = "string" },
                        repo = new { type = "string" },
                        base_url = new { type = "string" },
                        file_path = new { type = "string", description = "Repo-relative path for code anchor." },
                        line_start = new { type = "integer" },
                        line_end = new { type = "integer" },
                        member_key = new { type = "string" }
                    },
                    required = new[] { "title" }
                })
            },
            new()
            {
                Name = "ide_forge_lens_create_merge_request",
                Description = "Создать merge request в Agent Forge.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        title = new { type = "string" },
                        source_branch = new { type = "string" },
                        target_branch = new { type = "string" },
                        repo = new { type = "string" },
                        base_url = new { type = "string" },
                        file_path = new { type = "string" },
                        line_start = new { type = "integer" },
                        line_end = new { type = "integer" }
                    },
                    required = new[] { "title", "source_branch" }
                })
            },
            new()
            {
                Name = "ide_forge_lens_open",
                Description = "Открыть forge artifact по bracket [FRG:…] в браузере; compound code tail → editor (ADR-0159).",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        bracket = new { type = "string", description = "Bracket string, e.g. [FRG:pilot/issues/1]." },
                        base_url = new { type = "string" },
                        select_code = new { type = "boolean", description = "Navigate code tail with selection (default true)." }
                    },
                    required = new[] { "bracket" }
                })
            },
            new()
            {
                Name = "ide_intercom_reveal_attachment",
                Description = "Reveal из Intercom по AttachmentAnchor (ADR 0128 §8): re-resolve member/scope (Roslyn), transient highlight; select=true — выделить.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        anchor_json = new { type = "object", description = "Полный AttachmentAnchor (JSON)." },
                        file = new { type = "string", description = "Плоский режим: workspace-relative file." },
                        line_start = new { type = "integer" },
                        line_end = new { type = "integer" },
                        member_key = new { type = "string" },
                        syntax_scope = new { type = "object" },
                        duration_ms = new { type = "integer" },
                        select = new { type = "boolean", description = "true — SelectInEditor вместо reveal." }
                    },
                    required = Array.Empty<string>()
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

}

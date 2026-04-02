using System.Text.Json;

namespace CascadeIDE.Services;

/// <summary>
/// Callbacks from IDE MCP server into the application (UI/ViewModel).
/// </summary>
public interface IIdeMcpActions
{
    /// <summary>Выполнить команду по коду (IdeCommands). args — аргументы в том же формате, что у соответствующих тулов. Унифицированный вход для MCP и будущих меню/хоткеев.</summary>
    Task<string> ExecuteCommandAsync(string commandId, IReadOnlyDictionary<string, JsonElement>? args, CancellationToken cancellationToken = default);

    void OpenFile(string path);
    /// <summary>Загрузить решение по пути (.sln / .slnx). Дерево проектов обновится.</summary>
    void LoadSolution(string path);
    /// <summary>Выделить диапазон в редакторе (1-based line/column). Если файл не открыт — открыть.</summary>
    void SelectInEditor(string? filePath, int startLine, int startColumn, int endLine, int endColumn);
    /// <summary>Текущее состояние редактора (файл, каретка, выделение, content_length, is_empty, content_preview). JSON. maxPreviewChars: 0 = без превью, null/2000 = первые 2000 символов.</summary>
    Task<string> GetEditorStateAsync(int? maxPreviewChars = null);
    /// <summary>Содержимое редактора по строкам (1-based). JSON: file_path, start_line, end_line, content.</summary>
    Task<string> GetEditorContentRangeAsync(int startLine, int endLine);
    /// <summary>Полный текст открытой вкладки из модели документа. JSON: file_path, length, truncated, is_dirty, text; или error. filePath null — текущий файл; maxChars — опциональная обрезка.</summary>
    Task<string> GetOpenDocumentTextAsync(string? filePath, int? maxChars);
    /// <summary>Применить правку: заменить диапазон в файле (1-based). Файл должен быть открыт.</summary>
    void ApplyEdit(string filePath, int startLine, int startColumn, int endLine, int endColumn, string newText);
    /// <summary>Перейти на позицию (и опционально выделить до end). Если файл не открыт — открыть.</summary>
    void GoToPosition(string? filePath, int line, int column, int? endLine = null, int? endColumn = null);
    /// <summary>Информация о решении и открытом файле. JSON.</summary>
    string GetSolutionInfo();
    /// <summary>Список файлов в дереве решения (обозреватель). JSON: file_entries [{ path, title }]. Выполняется в UI-потоке.</summary>
    Task<string> GetSolutionFilesAsync();
    /// <summary>Диагностики текущего открытого .cs файла (ошибки/предупреждения Roslyn). JSON: массив { id, message, severity, line, column }. Не-C# — [].</summary>
    Task<string> GetCurrentFileDiagnosticsAsync();
    /// <summary>Запустить сборку решения, вернуть вывод.</summary>
    Task<string> BuildAsync();
    /// <summary>Запустить сборку и вернуть структурированный результат: success, exit_code, errors[], warnings[], raw_output (обрезано). JSON.</summary>
    Task<string> BuildStructuredAsync();
    /// <summary>Запустить тесты решения (dotnet test) и вернуть структурированный результат: success, total, passed, failed, skipped, failed_tests[] (name, message?, duration_ms?). JSON.</summary>
    Task<string> RunTestsAsync();
    /// <summary>Запустить затронутые тесты (по переданным путям) или fallback на полный прогон. JSON: success, total, passed, failed, skipped, failed_tests[], mode, filter.</summary>
    Task<string> RunAffectedTestsAsync(IReadOnlyList<string>? changedPaths = null);
    /// <summary>Запустить code cleanup через dotnet format для решения. Опционально includePath — конкретный файл/путь для точечной чистки. JSON: success, exit_code, raw_output.</summary>
    Task<string> RunCodeCleanupAsync(string? includePath = null);
    /// <summary>Посчитать метрики кода (LOC, классы, методы, cyclomatic complexity) для current_file/file/path/solution. JSON.</summary>
    Task<string> GetCodeMetricsAsync(string? scope = null, string? path = null);
    /// <summary>Одна сводка состояния IDE: solution/current file/selection/debug/build output/diagnostics. JSON.</summary>
    Task<string> GetWorkspaceStateAsync();
    /// <summary>Git status в каталоге решения/workspace. JSON с short/branch/output.</summary>
    Task<string> GitStatusAsync();
    /// <summary>Git diff в каталоге решения/workspace. Опционально path, staged. JSON.</summary>
    Task<string> GitDiffAsync(string? path = null, bool staged = false);
    /// <summary>Git commit в каталоге решения/workspace: message обязателен, paths опциональны (иначе add -A). JSON.</summary>
    Task<string> GitCommitAsync(string message, IReadOnlyList<string>? paths = null);
    /// <summary>Git push в каталоге решения/workspace. Опционально remote/branch. JSON.</summary>
    Task<string> GitPushAsync(string? remote = null, string? branch = null);
    /// <summary>Текущий текст панели «Вывод сборки» и цвета её оформления (background, foreground). JSON. Чтобы агент видел содержимое панели.</summary>
    string GetBuildOutput();
    void SetBreakpoint(string filePath, int line, string? condition = null);
    void RemoveBreakpoint(string filePath, int line);
    void ShowPreview(string title, string content);
    /// <summary>Показать превью текущего файла из редактора в отдельном окне. Контент берётся из IDE, не передаётся по MCP — удобно для длинных .md.</summary>
    void ShowEditorPreview();
    Task<string> RequestConfirmationAsync(string message, CancellationToken cancellationToken = default);
    void FocusEditor();
    /// <summary>Параметры темы UI (цвета, фоны, кнопки, шрифты). JSON.</summary>
    string GetUiTheme();
    /// <summary>Применить тему UI из JSON (тот же формат, что get_ui_theme). Выполняется в UI-потоке. Возвращает "OK" или сообщение об ошибке (например, невалидный JSON).</summary>
    Task<string> SetUiThemeAsync(string themeJson);
    /// <summary>Дерево элементов UI: тип, имя, видимость, границы (x,y,w,h), контент, дочерние. JSON. Вызов на UI-потоке.</summary>
    Task<string> GetUiLayoutAsync();
    /// <summary>Цвета фона и текста под текущим курсором мыши. JSON: type, name, background, foreground (hex). Вызов на UI-потоке.</summary>
    Task<string> GetColorsUnderCursorAsync();
    /// <summary>Эффективный вид любого контрола: тип, имя, bounds, visible, content, background, foreground, border, font. Без name — под курсором; с name — поиск по имени в дереве. Вызов на UI-потоке.</summary>
    Task<string> GetControlAppearanceAsync(string? name);
    /// <summary>Применить к контролу параметры layout на лету (margin, grid_row/column, canvas_left/top, dock). JSON. Вызов на UI-потоке.</summary>
    Task<string> SetControlLayoutAsync(string controlName, string layoutJson);
    /// <summary>Добавить контрол в конец Children панели (только Debug-сборка). parent_name — имя Panel, control_type — Button|TextBlock|Border, content — текст, name — опционально.</summary>
    Task<string> AddControlAsync(string parentName, string controlType, string? content, string? name);
    /// <summary>Установить текст в контрол с вводом (TextBox и т.п.) по имени. Вызов на UI-потоке.</summary>
    Task<string> SetControlTextAsync(string controlName, string text);
    /// <summary>Клик по контролу: без name — под курсором (должен быть Button); с name — по имени. Вызов на UI-потоке.</summary>
    Task<string> ClickControlAsync(string? controlName);
    /// <summary>Отправить сочетание клавиш в эффективный контрол (под курсором или по name). keys — текст вида Ctrl+Enter, Alt+F4. Вызов на UI-потоке.</summary>
    Task<string> SendKeysAsync(string? controlName, string keys);
    /// <summary>Передать фокус на эффективный контрол: по имени или на элемент под курсором. Вызов на UI-потоке.</summary>
    Task<string> SetFocusAsync(string? controlName);
    /// <summary>Подсветить эффективный контрол (рамка/оверлей), чтобы пользователь видел, где агент «находится». Без name — под курсором; с name — по имени. Вызов на UI-потоке.</summary>
    Task<string> HighlightControlAsync(string? controlName);
    /// <summary>Изменить размер панели: solution_explorer, chat — width (px); build_output, terminal — height (px). Вызов на UI-потоке.</summary>
    Task<string> SetPanelSizeAsync(string panel, double? width, double? height);
    /// <summary>Список языков/расширений редактора с подсветкой синтаксиса. JSON: массив { "extension", "language" }.</summary>
    string GetSupportedEditorLanguages();

    /// <summary>Показать в IDE брейкпоинты отладчика (из debug_set_breakpoints). breakpoints — массив { file_path, line }.</summary>
    void ShowDebugBreakpoints(IReadOnlyList<(string FilePath, int Line)> breakpoints);
    /// <summary>Показать текущую позицию отладки (файл, строка). Если file_path не null — открыть файл и подсветить строку; null — сбросить подсветку.</summary>
    void ShowDebugPosition(string? filePath, int line);
    /// <summary>Показать в панели отладки стек вызовов и переменные. stacks — массив { name, file, line }; variables — массив { name, value }.</summary>
    void ShowDebugState(IReadOnlyList<(string Name, string? File, int Line)> stackFrames, IReadOnlyList<(string Name, string Value)> variables);

    /// <summary>Записать заметки агента. Формат и структура на усмотрение агента. Хранятся в каталоге решения в .cascade-ide/agent-notes (расширение не задано — агент может писать markdown, json, текст). Без открытого решения — ошибка.</summary>
    Task<string> WriteAgentNotesAsync(string content, CancellationToken cancellationToken = default);
    /// <summary>Прочитать заметки агента. Возвращает содержимое файла или пустую строку, если файла нет или решение не загружено.</summary>
    Task<string> ReadAgentNotesAsync(CancellationToken cancellationToken = default);
    /// <summary>Добавить блок в конец заметок агента без полной перезаписи. Возвращает "OK" или ошибку.</summary>
    Task<string> AppendAgentNotesAsync(string content, CancellationToken cancellationToken = default);
    /// <summary>Список ревизий заметок агента. JSON: массив { file, size_bytes, modified_utc }.</summary>
    Task<string> ListAgentNotesRevisionsAsync(int? limit = null, CancellationToken cancellationToken = default);
    /// <summary>Откатить заметки к ревизии (или к последней, если revisionFile null). Возвращает OK/NO_CHANGES с именем ревизии.</summary>
    Task<string> RollbackAgentNotesAsync(string? revisionFile = null, CancellationToken cancellationToken = default);
    /// <summary>Прочитать только горячий контекст (L0/L1) без архивного хвоста. JSON: active_scope, loaded_sections, content.</summary>
    Task<string> ReadHotContextAsync(string? activeScope = null, CancellationToken cancellationToken = default);
    /// <summary>Router-first контекст пакет по запросу. JSON: assembled_context, loaded_sections, scores.</summary>
    Task<string> RouteContextAsync(string query, string? activeScope = null, int? maxSections = null, int? maxChars = null, CancellationToken cancellationToken = default);
    /// <summary>Health-check памяти по hot-context бюджетам. JSON.</summary>
    Task<string> MemoryHealthAsync(string? activeScope = null, CancellationToken cancellationToken = default);
    /// <summary>Ужать hot-context (preview/apply). JSON.</summary>
    Task<string> CompactHotContextAsync(bool apply = false, CancellationToken cancellationToken = default);
    /// <summary>Поиск по архивной ревизии заметок (или последней), с контекстом строк. JSON.</summary>
    Task<string> ExtractFromArchiveAsync(string query, string? revisionFile = null, int? headLimit = null, int? contextLines = null, CancellationToken cancellationToken = default);

    /// <summary>Вставить/обновить секцию в заметках агента. section_id — стабильный идентификатор, content — новое содержимое секции. Возвращает "OK" или ошибку.</summary>
    Task<string> UpsertAgentNotesSectionAsync(string sectionId, string content, CancellationToken cancellationToken = default);
    /// <summary>Поиск по заметкам агента (case-insensitive). Возвращает JSON: matches [{ line, text }].</summary>
    Task<string> SearchAgentNotesAsync(string query, int? headLimit = null, CancellationToken cancellationToken = default);
    /// <summary>Прочитать knowledge-файл из каталога решения (knowledge/&lt;file_path&gt;). Возвращает текст или "".</summary>
    Task<string> ReadKnowledgeFileAsync(string filePath, CancellationToken cancellationToken = default);
    /// <summary>Список knowledge-файлов в каталоге решения (knowledge/). subdir — относительный подкаталог (например "work"). Возвращает JSON: files [{ path, size_bytes, modified_utc }].</summary>
    Task<string> ListKnowledgeFilesAsync(string? subdir = null, CancellationToken cancellationToken = default);
}

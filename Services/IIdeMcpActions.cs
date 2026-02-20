namespace CascadeIDE.Services;

/// <summary>
/// Callbacks from IDE MCP server into the application (UI/ViewModel).
/// </summary>
public interface IIdeMcpActions
{
    void OpenFile(string path);
    /// <summary>Загрузить решение по пути (.sln / .slnx). Дерево проектов обновится.</summary>
    void LoadSolution(string path);
    /// <summary>Выделить диапазон в редакторе (1-based line/column). Если файл не открыт — открыть.</summary>
    void SelectInEditor(string? filePath, int startLine, int startColumn, int endLine, int endColumn);
    /// <summary>Текущее состояние редактора (файл, каретка, выделение). JSON.</summary>
    Task<string> GetEditorStateAsync();
    /// <summary>Применить правку: заменить диапазон в файле (1-based). Файл должен быть открыт.</summary>
    void ApplyEdit(string filePath, int startLine, int startColumn, int endLine, int endColumn, string newText);
    /// <summary>Перейти на позицию (и опционально выделить до end). Если файл не открыт — открыть.</summary>
    void GoToPosition(string? filePath, int line, int column, int? endLine = null, int? endColumn = null);
    /// <summary>Информация о решении и открытом файле. JSON.</summary>
    string GetSolutionInfo();
    /// <summary>Запустить сборку решения, вернуть вывод.</summary>
    Task<string> BuildAsync();
    /// <summary>Текущий текст панели «Вывод сборки» и цвета её оформления (background, foreground). JSON. Чтобы агент видел содержимое панели.</summary>
    string GetBuildOutput();
    void SetBreakpoint(string filePath, int line, string? condition = null);
    void ShowPreview(string title, string content);
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
}

using System.Threading;

namespace CascadeIDE.ViewModels;

/// <summary>Колбэки и провайдеры, которые View подставляет в главный VM (диалоги, UI automation).</summary>
public partial class MainWindowViewModel
{
    private Func<int?, Services.EditorStateDto?>? _editorStateProvider;
    private Func<int, int, string?>? _editorContentRangeProvider;
    private Action<string, int, int, int, int, string>? _applyEditAction;
    private Action? _focusEditorAction;

    public void SetEditorStateProvider(Func<int?, Services.EditorStateDto?> provider) => _editorStateProvider = provider;
    public void SetEditorContentRangeProvider(Func<int, int, string?> provider) => _editorContentRangeProvider = provider;
    public void SetApplyEdit(Action<string, int, int, int, int, string> action) => _applyEditAction = action;
    public void SetFocusEditor(Action action) => _focusEditorAction = action;

    /// <summary>Вызвать, чтобы показать диалог «Открыть решение» (View подставит реализацию).</summary>
    public Action? RequestOpenSolution { get; set; }
    /// <summary>Вызвать для закрытия окна (View подставит Close).</summary>
    public Action? RequestClose { get; set; }
    /// <summary>Показать «О программе» (View подставит диалог).</summary>
    public Action? RequestShowAbout { get; set; }
    /// <summary>Показать окно настроек (View подставит создание и Show).</summary>
    public Action? RequestOpenSettings { get; set; }
    /// <summary>Показать диалог выбора файла темы (.json). Возвращает путь к файлу или null.</summary>
    public Func<Task<string?>>? RequestOpenThemeFile { get; set; }
    /// <summary>Показать превью Markdown в отдельном окне (контент от агента).</summary>
    public Action<string, string>? RequestShowMarkdownPreviewWindow { get; set; }
    /// <summary>Показать превью текущего редактора в отдельном окне (живое обновление).</summary>
    public Action? RequestShowMarkdownPreviewForEditor { get; set; }
    /// <summary>Показать подтверждение пользователю. Возвращает "ok" или "cancel".</summary>
    public Func<string, CancellationToken, Task<string>>? RequestConfirmation { get; set; }
    /// <summary>Поставщик снимка дерева UI (View подставит вызов UiLayoutSnapshot.BuildJson).</summary>
    public Func<string>? GetUiLayoutProvider { get; set; }
    public Func<string>? GetColorsUnderCursorProvider { get; set; }
    public Func<string?, string>? GetControlAppearanceProvider { get; set; }
    public Func<string, string, string>? SetControlLayoutProvider { get; set; }
    public Func<string, string, string?, string?, string>? AddControlProvider { get; set; }
    public Func<string, string, string>? SetControlTextProvider { get; set; }
    public Func<string?, string>? ClickControlProvider { get; set; }
    public Func<string?, string, string>? SendKeysProvider { get; set; }
    public Func<string?, string>? SetFocusProvider { get; set; }
    public Func<string?, string>? HighlightControlProvider { get; set; }
    public Func<string, double?, double?, string>? SetPanelSizeProvider { get; set; }
}

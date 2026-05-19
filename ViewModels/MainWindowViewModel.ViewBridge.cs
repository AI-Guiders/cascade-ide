using System.Threading;

namespace CascadeIDE.ViewModels;

/// <summary>Колбэки и провайдеры, которые View подставляет в главный VM (диалоги, UI automation).</summary>
public partial class MainWindowViewModel
{
    private Func<int?, Services.EditorStateDto?>? _editorStateProvider;
    private Func<int, int, string?>? _editorContentRangeProvider;
    private Action<string, int, int, int, int, string>? _applyEditAction;
    private Action? _focusEditorAction;
    private Action<string?, int, int>? _revealEditorRangeAction;

    public void SetEditorStateProvider(Func<int?, Services.EditorStateDto?> provider) => _editorStateProvider = provider;
    public void SetEditorContentRangeProvider(Func<int, int, string?> provider) => _editorContentRangeProvider = provider;
    public void SetApplyEdit(Action<string, int, int, int, int, string> action) => _applyEditAction = action;
    public void SetFocusEditor(Action action) => _focusEditorAction = action;
    public void SetRevealEditorRange(Action<string?, int, int> action) => _revealEditorRangeAction = action;

    /// <summary>Вызвать, чтобы показать диалог «Открыть решение» (View подставит реализацию).</summary>
    public Action? RequestOpenSolution { get; set; }
    /// <summary>Вызвать, чтобы создать новое пустое решение (<c>dotnet new sln</c> после диалога «Сохранить как»).</summary>
    public Action? RequestCreateNewSolution { get; set; }
    /// <summary>Вызвать, чтобы показать диалог «Открыть папку» (workspace без .sln).</summary>
    public Action? RequestOpenFolder { get; set; }
    /// <summary>Вызвать, чтобы показать диалог «Открыть файл» (View подставит реализацию).</summary>
    public Action? RequestOpenFile { get; set; }
    /// <summary>Вызвать для закрытия окна (View подставит Close).</summary>
    public Action? RequestClose { get; set; }
    /// <summary>Показать «О программе» (View подставит диалог).</summary>
    public Action? RequestShowAbout { get; set; }
    /// <summary>Полное окно настроек (все секции): при <c>[ai.chat].settings_presentation = window</c>, из кнопки «Все настройки…» на странице AI во вторичном контуре.</summary>
    public Action? RequestOpenSettings { get; set; }
    /// <summary>Открыть или активировать окно-хост зоны Mfd — второй <c>TopLevel</c> (см. ADR 0017).</summary>
    public Action? RequestToggleMfdHostWindow { get; set; }
    /// <summary>Открыть или активировать окно-хост зоны Pfd при тройном пресете (ADR 0017).</summary>
    public Action? RequestTogglePfdHostWindow { get; set; }
    /// <summary>Открыть или активировать окно сплита P+M при пресете <c>(xP+yM)(F)</c> (ADR 0017).</summary>
    public Action? RequestTogglePmSplitHostWindow { get; set; }
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

    /// <summary>Выбор .dll/.exe для запуска под отладчиком. Возвращает полный путь или null.</summary>
    public Func<Task<string?>>? RequestPickDebugTarget { get; set; }

    /// <summary>Ввод PID для attach. Возвращает PID или null.</summary>
    public Func<Task<int?>>? RequestAttachProcessId { get; set; }

    /// <summary>Простой информационный диалог (заголовок, текст).</summary>
    public Func<string, string, Task>? RequestShowInfoAsync { get; set; }

    /// <summary>Сохранить файл Markdown (View показывает SaveFile dialog). Возвращает выбранный путь или null.</summary>
    public Func<string?, Task<string?>>? RequestSaveMarkdownFile { get; set; }

    /// <summary>MCP <c>capture_window</c>: PNG (по умолчанию главное окно; при <c>scope=all</c> — все top-level). Подставляет <see cref="Views.MainWindow"/>.</summary>
    public Func<string?, string?, string?, Task<string>>? CaptureWindowForMcpAsync { get; set; }
}

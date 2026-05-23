using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Editor;

/// <summary>
/// Сессия активного редактора: путь, текст, выделение, флаг загрузки.
/// <see cref="MainWindowViewModel"/> — композитор (прокси-свойства + побочные эффекты HUD/LOC/map).
/// </summary>
public sealed partial class EditorWorkspaceViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _host;

    public EditorWorkspaceViewModel(MainWindowViewModel host) => _host = host;

    [ObservableProperty]
    private string? _currentFilePath;

    [ObservableProperty]
    private bool _isLoadingCurrentFile;

    [ObservableProperty]
    private string _editorText = "";

    /// <summary>Запрос выделения: начальный offset. View применит к редактору и сбросит.</summary>
    [ObservableProperty]
    private int? _editorSelectionStart;

    /// <summary>Запрос выделения: длина. View применит к редактору и сбросит.</summary>
    [ObservableProperty]
    private int? _editorSelectionLength;

    /// <summary>True, если открыт файл .md или .markdown — показываем превью.</summary>
    public bool IsMarkdownFile =>
        !string.IsNullOrEmpty(CurrentFilePath)
        && (CurrentFilePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            || CurrentFilePath.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase));

    /// <summary>Показывать панель превью Markdown только когда контент уже загружен.</summary>
    public bool IsMarkdownPreviewVisible => IsMarkdownFile && !IsLoadingCurrentFile;

    partial void OnCurrentFilePathChanged(string? value) => _host.OnEditorCurrentFilePathChanged();

    partial void OnEditorTextChanged(string value) => _host.OnEditorTextChanged(value);

    partial void OnIsLoadingCurrentFileChanged(bool value) => _host.OnEditorIsLoadingCurrentFileChanged();
}

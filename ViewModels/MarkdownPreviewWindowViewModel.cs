using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>ViewModel для отдельного окна превью Markdown. Поддерживает режим «контент от агента» и «живое» превью из редактора.</summary>
public sealed partial class MarkdownPreviewWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Превью";

    [ObservableProperty]
    private string _markdown = "";

    private MainWindowViewModel? _editorVm;
    private PropertyChangedEventHandler? _editorHandler;

    /// <summary>Подключить к редактору: превью обновляется при изменении EditorText/CurrentFilePath.</summary>
    public void AttachToEditor(MainWindowViewModel vm)
    {
        DetachFromEditor();
        _editorVm = vm;
        _editorHandler = (_, e) =>
        {
            if (e.PropertyName is nameof(MainWindowViewModel.EditorText) or nameof(MainWindowViewModel.CurrentFilePath))
                UpdateFromEditor();
        };
        vm.PropertyChanged += _editorHandler;
        UpdateFromEditor();
    }

    /// <summary>Отвязать от редактора (переход в режим контента от агента).</summary>
    public void DetachFromEditor()
    {
        if (_editorVm is null)
            return;
        if (_editorHandler is not null)
            _editorVm.PropertyChanged -= _editorHandler;
        _editorVm = null;
        _editorHandler = null;
    }

    /// <summary>Установить контент напрямую (режим агента).</summary>
    public void SetContent(string title, string content)
    {
        DetachFromEditor();
        Title = title;
        Markdown = content ?? "";
    }

    private void UpdateFromEditor()
    {
        if (_editorVm is null)
            return;
        Title = string.IsNullOrEmpty(_editorVm.CurrentFilePath) ? "Превью" : _editorVm.CurrentFilePath;
        Markdown = _editorVm.EditorText ?? "";
    }
}

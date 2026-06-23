#nullable enable

namespace CascadeIDE.Views;

public partial class MainWindow
{
    private void EnsurePreviewWindow()
    {
        if (_previewWindow is not null)
            return;
        _previewVm = new ViewModels.MarkdownPreviewWindowViewModel();
        _previewWindow = new MarkdownPreviewWindow { DataContext = _previewVm };
        _previewWindow.Closed += (_, _) =>
        {
            _previewWindow = null;
            _previewVm?.DetachFromEditor();
        };
    }

    private void ShowMarkdownPreviewWindow(string title, string content)
    {
        EnsurePreviewWindow();
        _previewVm!.SetContent(title, content);
        _previewWindow!.Show(this);
        _previewWindow.Activate();
    }

    private void ShowMarkdownPreviewForEditor()
    {
        if (DataContext is not ViewModels.MainWindowViewModel mainVm)
            return;

        EnsurePreviewWindow();
        _previewVm!.AttachToEditor(mainVm);
        _previewWindow!.Show(this);
        _previewWindow.Activate();
    }
}

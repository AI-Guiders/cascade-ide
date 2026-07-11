using CascadeIDE.Features.Editor.Application.Monaco;

namespace CascadeIDE.ViewModels;

/// <summary>Monaco editor navigation service wiring for MainWindow.</summary>
public partial class MainWindowViewModel
{
    public IEditorNavigationService EditorNavigation { get; private set; } = null!;

    private void InitializeEditorNavigation() =>
        EditorNavigation = new EditorNavigationService(this);
}

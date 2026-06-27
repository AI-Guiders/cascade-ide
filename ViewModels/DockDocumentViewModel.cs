using Dock.Model.Mvvm.Controls;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Dock document wrapper around an opened file.
/// Context is an <see cref="OpenDocumentViewModel"/>.
/// Dock chrome title sync — <see cref="Documents.DocumentsWorkspaceViewModel.BindDockChrome"/>.
/// </summary>
public sealed class DockDocumentViewModel(OpenDocumentViewModel doc) : Document
{
    public OpenDocumentViewModel Doc { get; } = doc;
}

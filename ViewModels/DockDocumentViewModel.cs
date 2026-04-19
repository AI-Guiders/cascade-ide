using Dock.Model.Mvvm.Controls;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Dock document wrapper around an opened file.
/// Context is an <see cref="OpenDocumentViewModel"/>.
/// </summary>
public sealed class DockDocumentViewModel(OpenDocumentViewModel doc) : Document
{
    public OpenDocumentViewModel Doc { get; } = doc;
}


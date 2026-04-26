using Avalonia;

namespace CascadeIDE.Features.Editor.Application.Presentation;

/// <summary>
/// Отступы/плотность зоны текста Forward-документа (один источник с <c>DockDocumentView</c> XAML, roadmap §7).
/// </summary>
public static class EditorForwardDocumentChrome
{
    public const double DocumentPaddingDip = 8;

    public static Thickness DocumentPadding { get; } = new(DocumentPaddingDip);
}

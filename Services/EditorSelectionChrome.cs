#nullable enable
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaEdit;

namespace CascadeIDE.Services;

/// <summary>
/// Спокойное выделение в <see cref="TextEditor"/> (AvaloniaEdit): кисти из <c>CascadeTheme.EditorSelection*</c> или мягкий fallback.
/// После <see cref="UiThemeApply.Apply"/> нужно вызывать снова — кисти в ресурсах заменяются новыми экземплярами.
/// </summary>
public static class EditorSelectionChrome
{
    public static void Apply(TextEditor? editor)
    {
        if (editor is null)
            return;

        IResourceHost? host = editor;
        if (Application.Current is { } app)
        {
            if (TopLevel.GetTopLevel(editor) is { } tl)
                host = tl;
            else
                host = app;
        }

        var brush = TryBrush(host, UiThemeApply.Keys.EditorSelectionBrush)
            ?? new SolidColorBrush(Color.Parse("#5528496E"));
        editor.TextArea.SelectionBrush = brush;

        if (TryBrush(host, UiThemeApply.Keys.EditorSelectionForeground) is { } fg)
            editor.TextArea.SelectionForeground = fg;

        editor.TextArea.SelectionCornerRadius = 4;
    }

    private static IBrush? TryBrush(IResourceHost? host, string key)
    {
        if (host is null)
            return null;
        var variant = Application.Current?.ActualThemeVariant ?? ThemeVariant.Default;
        return host.TryGetResource(key, variant, out var raw)
            ? raw as IBrush
            : null;
    }
}

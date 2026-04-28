#nullable enable
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaEdit;

namespace CascadeIDE.Services;

/// <summary>
/// Дефолтные кисти текста/фона/каретки для <see cref="TextEditor"/> (AvaloniaEdit).
/// Нужны как safety-net, когда подсветка/грамматика/тема не выставили видимые цвета.
/// После <see cref="UiThemeApply.Apply"/> нужно вызывать снова — кисти в ресурсах заменяются новыми экземплярами.
/// </summary>
public static class EditorTextChrome
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

        var bg = TryBrush(host, UiThemeApply.Keys.EditorBackground)
                 ?? editor.Background
                 ?? new SolidColorBrush(Color.Parse("#FF1E1E1E"));
        var fg = TryBrush(host, UiThemeApply.Keys.EditorForeground)
                 ?? editor.Foreground
                 ?? new SolidColorBrush(Color.Parse("#FFE6E6E6"));

        editor.Background = bg;
        editor.Foreground = fg;
        editor.TextArea.Caret.CaretBrush = fg;
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


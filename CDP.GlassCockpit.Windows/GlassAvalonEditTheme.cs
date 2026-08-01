#nullable enable
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Stock AvalonEdit C# colours assume a light canvas — on dark glass they become
/// unreadable. Remap to a calm GitHub-Dark / VS Dark+ palette (ADHD-friendly contrast).
/// </summary>
internal static class GlassAvalonEditTheme
{
    static readonly Color Canvas = Color.FromRgb(0x0D, 0x11, 0x17);
    static readonly Color Ink = Color.FromRgb(0xE6, 0xED, 0xF3);
    static readonly Color Gutter = Color.FromRgb(0x6E, 0x76, 0x81);

    static readonly Dictionary<string, Color> Named =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Comment"] = Color.FromRgb(0x8B, 0x94, 0x9E),
            ["XmlComment"] = Color.FromRgb(0x8B, 0x94, 0x9E),
            ["String"] = Color.FromRgb(0xA5, 0xD6, 0xFF),
            ["StringInterpolation"] = Color.FromRgb(0xA5, 0xD6, 0xFF),
            ["Char"] = Color.FromRgb(0xA5, 0xD6, 0xFF),
            ["Preprocessor"] = Color.FromRgb(0x8B, 0x94, 0x9E),
            ["Punctuation"] = Ink,
            ["ValueTypeKeywords"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["ReferenceTypeKeywords"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["MethodCallKeywords"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["ThisOrBaseReference"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["NullOrValueKeywords"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["Keywords"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["GotoKeywords"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["ContextKeywords"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["ExceptionKeywords"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["CheckedKeyword"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["UnsafeKeywords"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["OperatorKeywords"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["ParameterModifiers"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["Modifiers"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["Visibility"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["NamespaceKeywords"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["GetSetAddRemove"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["TrueFalse"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["TypeKeywords"] = Color.FromRgb(0xFF, 0xA6, 0x57),
            ["SemanticKeywords"] = Color.FromRgb(0xFF, 0xA6, 0x57),
            ["NumberLiteral"] = Color.FromRgb(0x79, 0xC0, 0xFF),
            ["MethodName"] = Color.FromRgb(0xD2, 0xA8, 0xFF),
        };

    public static void ApplyDarkReadable(TextEditor editor)
    {
        editor.FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New");
        editor.FontSize = 14.5;
        editor.Background = new SolidColorBrush(Canvas);
        editor.Foreground = new SolidColorBrush(Ink);
        editor.LineNumbersForeground = new SolidColorBrush(Gutter);
        editor.TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(0x88, 0x26, 0x4F, 0x78));
        editor.TextArea.SelectionForeground = null;
        editor.TextArea.SelectionBorder = null;
        editor.TextArea.Caret.CaretBrush = new SolidColorBrush(Color.FromRgb(0x58, 0xA6, 0xFF));
        editor.Options.EnableHyperlinks = false;
        editor.Options.EnableEmailHyperlinks = false;
        editor.Options.AllowScrollBelowDocument = true;
        editor.TextArea.TextView.LinkTextForegroundBrush = new SolidColorBrush(Color.FromRgb(0x79, 0xC0, 0xFF));

        var def = editor.SyntaxHighlighting;
        if (def is null)
            return;

        foreach (var color in def.NamedHighlightingColors)
        {
            color.Background = null;
            color.FontWeight = null;
            color.Underline = null;
            if (Named.TryGetValue(color.Name ?? "", out var fg))
                color.Foreground = new SimpleHighlightingBrush(fg);
            else
                color.Foreground = new SimpleHighlightingBrush(Ink);
        }

        // Force TextView to rebuild colourizers with remapped brushes.
        editor.SyntaxHighlighting = null;
        editor.SyntaxHighlighting = def;
    }
}

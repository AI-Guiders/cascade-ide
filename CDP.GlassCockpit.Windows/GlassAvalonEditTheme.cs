#nullable enable
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Stock AvalonEdit C# colours assume a light canvas — on dark glass they become
/// unreadable (and some spans paint solid backgrounds). Remap to a calm VS Dark+ palette.
/// </summary>
internal static class GlassAvalonEditTheme
{
    static readonly Dictionary<string, Color> Named =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Comment"] = Color.FromRgb(0x6A, 0x99, 0x55),
            ["XmlComment"] = Color.FromRgb(0x6A, 0x99, 0x55),
            ["String"] = Color.FromRgb(0xCE, 0x91, 0x78),
            ["StringInterpolation"] = Color.FromRgb(0xCE, 0x91, 0x78),
            ["Char"] = Color.FromRgb(0xCE, 0x91, 0x78),
            ["Preprocessor"] = Color.FromRgb(0x9B, 0x9B, 0x9B),
            ["Punctuation"] = Color.FromRgb(0xD4, 0xD4, 0xD4),
            ["ValueTypeKeywords"] = Color.FromRgb(0x56, 0x9C, 0xD6),
            ["ReferenceTypeKeywords"] = Color.FromRgb(0x56, 0x9C, 0xD6),
            ["MethodCallKeywords"] = Color.FromRgb(0x56, 0x9C, 0xD6),
            ["ThisOrBaseReference"] = Color.FromRgb(0x56, 0x9C, 0xD6),
            ["NullOrValueKeywords"] = Color.FromRgb(0x56, 0x9C, 0xD6),
            ["Keywords"] = Color.FromRgb(0x56, 0x9C, 0xD6),
            ["GotoKeywords"] = Color.FromRgb(0xC5, 0x86, 0xC0),
            ["ContextKeywords"] = Color.FromRgb(0xC5, 0x86, 0xC0),
            ["ExceptionKeywords"] = Color.FromRgb(0xC5, 0x86, 0xC0),
            ["CheckedKeyword"] = Color.FromRgb(0xC5, 0x86, 0xC0),
            ["UnsafeKeywords"] = Color.FromRgb(0xC5, 0x86, 0xC0),
            ["OperatorKeywords"] = Color.FromRgb(0x56, 0x9C, 0xD6),
            ["ParameterModifiers"] = Color.FromRgb(0x56, 0x9C, 0xD6),
            ["Modifiers"] = Color.FromRgb(0x56, 0x9C, 0xD6),
            ["Visibility"] = Color.FromRgb(0x56, 0x9C, 0xD6),
            ["NamespaceKeywords"] = Color.FromRgb(0x56, 0x9C, 0xD6),
            ["GetSetAddRemove"] = Color.FromRgb(0x56, 0x9C, 0xD6),
            ["TrueFalse"] = Color.FromRgb(0x56, 0x9C, 0xD6),
            ["TypeKeywords"] = Color.FromRgb(0x4E, 0xC9, 0xB0),
            ["SemanticKeywords"] = Color.FromRgb(0x4E, 0xC9, 0xB0),
            ["NumberLiteral"] = Color.FromRgb(0xB5, 0xCE, 0xA8),
            ["MethodName"] = Color.FromRgb(0xDC, 0xDC, 0xAA),
        };

    public static void ApplyDarkReadable(TextEditor editor)
    {
        editor.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
        editor.Foreground = new SolidColorBrush(Color.FromRgb(0xD4, 0xD4, 0xD4));
        editor.LineNumbersForeground = new SolidColorBrush(Color.FromRgb(0x85, 0x85, 0x85));
        editor.TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(0x66, 0x26, 0x4F, 0x78));
        editor.TextArea.SelectionForeground = null;
        editor.TextArea.SelectionBorder = null;
        editor.Options.EnableHyperlinks = false;
        editor.Options.EnableEmailHyperlinks = false;

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
            else if (color.Foreground is not null)
            {
                // Keep unknown named colours but drop any light-canvas backgrounds already cleared.
            }
        }

        // Force TextView to rebuild colourizers with remapped brushes.
        editor.SyntaxHighlighting = null;
        editor.SyntaxHighlighting = def;
    }
}

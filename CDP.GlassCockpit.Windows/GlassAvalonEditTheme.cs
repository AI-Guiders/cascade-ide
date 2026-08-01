#nullable enable
using System.IO;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Native AvalonEdit path (XSHD + HighlightingManager). Dark canvas + one-shot dark remap of stock defs.
/// No TextMate on WPF — that binding exists only for AvaloniaEdit.
/// </summary>
internal static class GlassAvalonEditTheme
{
    static readonly Color Canvas = Color.FromRgb(0x0D, 0x11, 0x17);
    static readonly Color Ink = Color.FromRgb(0xE6, 0xED, 0xF3);
    static readonly Color Gutter = Color.FromRgb(0x6E, 0x76, 0x81);
    static readonly object Gate = new();
    static readonly HashSet<string> Remapped = new(StringComparer.Ordinal);

    static readonly Dictionary<string, Color> Named =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Comment"] = Color.FromRgb(0x8B, 0x94, 0x9E),
            ["XmlComment"] = Color.FromRgb(0x8B, 0x94, 0x9E),
            ["String"] = Color.FromRgb(0xA5, 0xD6, 0xFF),
            ["CharString"] = Color.FromRgb(0xA5, 0xD6, 0xFF),
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
            ["Digits"] = Color.FromRgb(0x79, 0xC0, 0xFF),
            ["MethodName"] = Color.FromRgb(0xD2, 0xA8, 0xFF),
            ["XmlTag"] = Color.FromRgb(0x7E, 0xE7, 0x87),
            ["XmlAttribute"] = Color.FromRgb(0x79, 0xC0, 0xFF),
            ["XmlAttributeQuotes"] = Color.FromRgb(0xA5, 0xD6, 0xFF),
            ["XmlAttributeValue"] = Color.FromRgb(0xA5, 0xD6, 0xFF),
            ["XmlName"] = Color.FromRgb(0x7E, 0xE7, 0x87),
            ["HtmlElement"] = Color.FromRgb(0x7E, 0xE7, 0x87),
            ["HtmlAttributeName"] = Color.FromRgb(0x79, 0xC0, 0xFF),
            ["HtmlAttributeValue"] = Color.FromRgb(0xA5, 0xD6, 0xFF),
            ["ScriptTag"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["JavaScriptKeyword"] = Color.FromRgb(0xFF, 0x7B, 0x72),
            ["JavaScriptString"] = Color.FromRgb(0xA5, 0xD6, 0xFF),
            ["JavaScriptNumber"] = Color.FromRgb(0x79, 0xC0, 0xFF),
            ["JavaScriptComment"] = Color.FromRgb(0x8B, 0x94, 0x9E),
        };

    public static IHighlightingDefinition? ResolveDefinition(string path)
    {
        var ext = Path.GetExtension(path);
        var byExt = HighlightingManager.Instance.GetDefinitionByExtension(ext);
        if (byExt is not null)
            return RemapOnce(byExt);

        var name = ext.ToLowerInvariant() switch
        {
            ".cs" or ".csx" => "C#",
            ".xaml" or ".axaml" or ".csproj" or ".props" or ".targets" or ".config" or ".xml" => "XML",
            ".json" or ".jsonc" => "JavaScript",
            ".md" or ".markdown" => "MarkDown",
            ".js" or ".mjs" or ".cjs" => "JavaScript",
            ".html" or ".htm" => "HTML",
            ".css" => "CSS",
            _ => null
        };

        if (name is null)
            return null;

        var def = HighlightingManager.Instance.GetDefinition(name);
        return def is null ? null : RemapOnce(def);
    }

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

        if (editor.SyntaxHighlighting is { } def)
            editor.SyntaxHighlighting = RemapOnce(def);
    }

    static IHighlightingDefinition RemapOnce(IHighlightingDefinition def)
    {
        var key = def.Name ?? "";
        lock (Gate)
        {
            if (!Remapped.Add(key))
                return def;

            foreach (var color in def.NamedHighlightingColors)
            {
                color.Background = null;
                color.Underline = null;
                if (Named.TryGetValue(color.Name ?? "", out var fg))
                {
                    color.Foreground = new SimpleHighlightingBrush(fg);
                    continue;
                }

                // Leave unknown names alone unless they look like light-theme black ink.
                if (LooksUnreadableOnDark(color.Foreground))
                    color.Foreground = new SimpleHighlightingBrush(Ink);
            }

            return def;
        }
    }

    static bool LooksUnreadableOnDark(HighlightingBrush? brush)
    {
        if (brush is null)
            return true;
        try
        {
            var c = brush.GetColor(null);
            if (c is null)
                return true;
            // Relative luminance — dark strokes vanish on #0D1117.
            var l = (0.2126 * c.Value.R + 0.7152 * c.Value.G + 0.0722 * c.Value.B) / 255.0;
            return l < 0.35;
        }
        catch
        {
            return true;
        }
    }
}

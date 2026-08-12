#nullable enable

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CascadeIDE.SoftInstrument;

namespace CDP.GlassCockpit.Windows;

/// <summary>Unified diff → FlowDocument with Dark Cockpit line tint (+/−/@@ + row background).</summary>
public static class GlassGitDiffFlowDocument
{
    static readonly Brush ContextFg = Fg("#B0B0B0");
    static readonly Brush AddFg = Fg("#A8E0A8");
    static readonly Brush DeleteFg = Fg("#E0A8A8");
    static readonly Brush HunkFg = Fg("#E0C878");
    static readonly Brush MetaFg = Fg("#7A7A7A");

    static readonly Brush AddBg = Fg("#1A2E1A");
    static readonly Brush DeleteBg = Fg("#2E1A1A");
    static readonly Brush HunkBg = Fg("#2A2618");

    public static FlowDocument Build(string? text)
    {
        var doc = new FlowDocument
        {
            Background = Brushes.Transparent,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            PagePadding = new Thickness(0),
            TextAlignment = TextAlignment.Left,
        };

        if (string.IsNullOrWhiteSpace(text))
        {
            doc.Blocks.Add(Line("(no diff)", ContextFg, null));
            return doc;
        }

        foreach (var raw in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var kind = GlassGitDiffLine.Classify(raw);
            var (fg, bg) = kind switch
            {
                GlassGitDiffLineKind.Add => (AddFg, AddBg),
                GlassGitDiffLineKind.Delete => (DeleteFg, DeleteBg),
                GlassGitDiffLineKind.Hunk => (HunkFg, HunkBg),
                GlassGitDiffLineKind.Meta => (MetaFg, (Brush?)null),
                _ => (ContextFg, (Brush?)null),
            };
            doc.Blocks.Add(Line(raw.Length == 0 ? " " : raw, fg, bg));
        }

        return doc;
    }

    static Paragraph Line(string text, Brush fg, Brush? bg)
    {
        var p = new Paragraph(new Run(text) { Foreground = fg })
        {
            Margin = new Thickness(0),
            Padding = new Thickness(2, 0, 2, 0),
            LineHeight = 16,
        };
        if (bg is not null)
            p.Background = bg;
        return p;
    }

    static SolidColorBrush Fg(string hex)
    {
        var b = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        b.Freeze();
        return b;
    }
}

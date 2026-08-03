#nullable enable

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CascadeIDE.Intercom;

namespace CDP.GlassCockpit.Windows;

/// <summary>WPF Intercom feed body — ADR 0129/0170 subset via GlassCore <see cref="IntercomMarkdown"/>.</summary>
public sealed class GlassIntercomMarkdownBody : ContentControl
{
    public static readonly DependencyProperty MarkdownProperty = DependencyProperty.Register(
        nameof(Markdown),
        typeof(string),
        typeof(GlassIntercomMarkdownBody),
        new PropertyMetadata(null, OnMarkdownChanged));

    public string? Markdown
    {
        get => (string?)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((GlassIntercomMarkdownBody)d).Content = GlassIntercomMarkdownRenderer.Build(e.NewValue as string);
    }
}

internal static class GlassIntercomMarkdownRenderer
{
    static readonly Brush BodyFg = brush("#E8E8E8");
    static readonly Brush CodeFg = brush("#D4D4D4");
    static readonly Brush CodeBg = brush("#1E1E1E");
    static readonly Brush CodeBorder = brush("#3A3A3A");
    static readonly Brush LinkFg = brush("#7EB8FF");
    static readonly Brush HrBrush = brush("#555555");
    static readonly FontFamily Mono = new("Consolas, Cascadia Mono, Courier New");

    public static FrameworkElement Build(string? markdown)
    {
        var root = new StackPanel { Orientation = Orientation.Vertical };
        foreach (var seg in IntercomMarkdown.SplitSegments(markdown))
        {
            if (seg.Kind == IntercomMarkdownSegmentKind.Code)
                root.Children.Add(BuildCodeBlock(seg.Text));
            else
                root.Children.Add(BuildProse(seg.Text));
        }

        if (root.Children.Count == 0)
            root.Children.Add(BuildPlainLine(""));

        return root;
    }

    static FrameworkElement BuildCodeBlock(string code)
    {
        var tb = new TextBlock
        {
            Text = code,
            FontFamily = Mono,
            FontSize = 12,
            Foreground = CodeFg,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 16,
        };
        return new Border
        {
            Background = CodeBg,
            BorderBrush = CodeBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 4, 0, 4),
            Child = tb,
        };
    }

    static FrameworkElement BuildProse(string prose)
    {
        if (string.IsNullOrEmpty(prose))
            return BuildPlainLine("");

        if (IntercomMarkdown.ShouldUseDocumentLayout(prose))
            return BuildDocument(prose);

        return BuildInlineProse(prose);
    }

    static FrameworkElement BuildDocument(string prose)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical };
        // Large wrap budget — WPF TextBlock wraps by pixels; avoid char-prewrap crushing layout.
        foreach (var row in IntercomMarkdown.LayoutDocument(prose, maxChars: 10_000))
        {
            switch (row.Kind)
            {
                case IntercomMarkdownBlockKind.Blank:
                    panel.Children.Add(new Border { Height = 8 });
                    break;
                case IntercomMarkdownBlockKind.HorizontalRule:
                    panel.Children.Add(new Border
                    {
                        Height = 1,
                        Background = HrBrush,
                        Margin = new Thickness(0, 8, 0, 8),
                    });
                    break;
                default:
                    panel.Children.Add(BuildRowTextBlock(row));
                    break;
            }
        }

        return panel;
    }

    static TextBlock BuildRowTextBlock(IntercomMarkdownRow row)
    {
        var (fontSize, weight, margin) = row.Kind switch
        {
            IntercomMarkdownBlockKind.Heading1 => (18.0, FontWeights.SemiBold, new Thickness(0, 8, 0, 4)),
            IntercomMarkdownBlockKind.Heading2 => (16.0, FontWeights.SemiBold, new Thickness(0, 6, 0, 3)),
            IntercomMarkdownBlockKind.Heading3 => (14.0, FontWeights.SemiBold, new Thickness(0, 4, 0, 2)),
            IntercomMarkdownBlockKind.Bullet => (13.0, FontWeights.Normal, new Thickness(0, 1, 0, 1)),
            _ => (13.0, FontWeights.Normal, new Thickness(0, 1, 0, 1)),
        };

        var tb = new TextBlock
        {
            FontSize = fontSize,
            FontWeight = weight,
            Foreground = BodyFg,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = fontSize + 5,
            Margin = margin,
        };
        AppendRuns(tb, row.Runs);
        return tb;
    }

    static FrameworkElement BuildInlineProse(string prose)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical };
        var lines = prose.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Length == 0)
            {
                panel.Children.Add(new Border { Height = 8 });
                continue;
            }

            var tb = new TextBlock
            {
                FontSize = 13,
                Foreground = BodyFg,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18,
                Margin = new Thickness(0, i == 0 ? 0 : 1, 0, 0),
            };
            AppendRuns(tb, IntercomMarkdown.ParseInline(line));
            panel.Children.Add(tb);
        }

        return panel;
    }

    static TextBlock BuildPlainLine(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 13,
            Foreground = BodyFg,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 18,
        };
    }

    static void AppendRuns(TextBlock tb, IReadOnlyList<IntercomMarkdownRun> runs)
    {
        if (runs.Count == 0)
        {
            tb.Text = "";
            return;
        }

        foreach (var run in runs)
        {
            if (run.Text.Length == 0)
                continue;

            Inline inline = run.Style switch
            {
                IntercomMarkdownStyle.Bold => new Run(run.Text) { FontWeight = FontWeights.SemiBold },
                IntercomMarkdownStyle.Italic => new Run(run.Text) { FontStyle = FontStyles.Italic },
                IntercomMarkdownStyle.Code => new Run(run.Text)
                {
                    FontFamily = Mono,
                    Foreground = CodeFg,
                    Background = CodeBg,
                },
                IntercomMarkdownStyle.Link => new Run(run.Text)
                {
                    Foreground = LinkFg,
                    TextDecorations = TextDecorations.Underline,
                },
                _ => new Run(run.Text),
            };
            tb.Inlines.Add(inline);
        }

        if (tb.Inlines.Count == 0)
            tb.Text = "";
    }

    static SolidColorBrush brush(string hex)
    {
        var b = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        if (b.CanFreeze)
            b.Freeze();
        return b;
    }
}

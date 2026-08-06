#nullable enable

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using CascadeIDE.Intercom;

namespace CDP.GlassCockpit.Windows;

/// <summary>WPF Intercom feed body — ADR 0129/0170 subset via GlassCore <see cref="IntercomMarkdown"/>.</summary>
/// <remarks>
/// StackPanel (not ContentControl): setting Content during DataTemplate expand double-parents the built tree.
/// One read-only RichTextBox per bubble (single FlowDocument) — select/copy without N× PTS contexts
/// (lived FailFast PtsHost.CreateDocContext → «Unknown Hard Error» when every line was its own RTB).
/// </remarks>
public sealed class GlassIntercomMarkdownBody : StackPanel
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
        var body = (GlassIntercomMarkdownBody)d;
        var md = e.NewValue as string;
        // Escape DataTemplate ApplyTemplatedParentValue reentrancy (XamlParseException / double parent).
        body.Dispatcher.BeginInvoke(
            () => body.Rebuild(md),
            DispatcherPriority.DataBind);
    }

    void Rebuild(string? markdown)
    {
        Children.Clear();
        Children.Add(GlassIntercomMarkdownRenderer.Build(markdown));
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
    static readonly TextDecorationCollection LinkUnderline = CreateLinkUnderline();

    public static FrameworkElement Build(string? markdown)
    {
        var doc = new FlowDocument
        {
            PagePadding = new Thickness(0),
            TextAlignment = TextAlignment.Left,
            Background = Brushes.Transparent,
            Foreground = BodyFg,
        };

        var any = false;
        foreach (var seg in IntercomMarkdown.SplitSegments(markdown))
        {
            if (seg.Kind == IntercomMarkdownSegmentKind.Code)
            {
                AppendCodeBlock(doc, seg.Text);
                any = true;
            }
            else
            {
                any |= AppendProse(doc, seg.Text);
            }
        }

        if (!any)
            doc.Blocks.Add(MakeParagraph([], 13, FontWeights.Normal, new Thickness(0)));

        return new RichTextBox
        {
            Document = doc,
            IsReadOnly = true,
            IsDocumentEnabled = true,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = BodyFg,
            FontSize = 13,
            Margin = new Thickness(0),
            Padding = new Thickness(0),
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CaretBrush = Brushes.Transparent,
            Focusable = true,
        };
    }

    static void AppendCodeBlock(FlowDocument doc, string code)
    {
        var para = new Paragraph(new Run(code)
        {
            FontFamily = Mono,
            Foreground = CodeFg,
            Background = CodeBg,
            FontSize = 12,
        })
        {
            Margin = new Thickness(0, 4, 0, 4),
            Padding = new Thickness(8, 6, 8, 6),
            BorderBrush = CodeBorder,
            BorderThickness = new Thickness(1),
            Background = CodeBg,
            LineHeight = 17,
        };
        doc.Blocks.Add(para);
    }

    static bool AppendProse(FlowDocument doc, string prose)
    {
        if (string.IsNullOrEmpty(prose))
        {
            doc.Blocks.Add(MakeParagraph([], 13, FontWeights.Normal, new Thickness(0)));
            return true;
        }

        if (IntercomMarkdown.ShouldUseDocumentLayout(prose))
        {
            AppendDocument(doc, prose);
            return true;
        }

        AppendInlineProse(doc, prose);
        return true;
    }

    static void AppendDocument(FlowDocument doc, string prose)
    {
        foreach (var row in IntercomMarkdown.LayoutDocument(prose, maxChars: 10_000))
        {
            switch (row.Kind)
            {
                case IntercomMarkdownBlockKind.Blank:
                    doc.Blocks.Add(new Paragraph(new Run(" "))
                    {
                        Margin = new Thickness(0),
                        LineHeight = 8,
                        FontSize = 4,
                    });
                    break;
                case IntercomMarkdownBlockKind.HorizontalRule:
                    doc.Blocks.Add(new Paragraph
                    {
                        Margin = new Thickness(0, 8, 0, 8),
                        BorderBrush = HrBrush,
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Padding = new Thickness(0, 0, 0, 1),
                    });
                    break;
                default:
                    var (fontSize, weight, margin) = row.Kind switch
                    {
                        IntercomMarkdownBlockKind.Heading1 => (18.0, FontWeights.SemiBold, new Thickness(0, 8, 0, 4)),
                        IntercomMarkdownBlockKind.Heading2 => (16.0, FontWeights.SemiBold, new Thickness(0, 6, 0, 3)),
                        IntercomMarkdownBlockKind.Heading3 => (14.0, FontWeights.SemiBold, new Thickness(0, 4, 0, 2)),
                        IntercomMarkdownBlockKind.Bullet => (13.0, FontWeights.Normal, new Thickness(0, 1, 0, 1)),
                        _ => (13.0, FontWeights.Normal, new Thickness(0, 1, 0, 1)),
                    };
                    doc.Blocks.Add(MakeParagraph(row.Runs, fontSize, weight, margin));
                    break;
            }
        }
    }

    static void AppendInlineProse(FlowDocument doc, string prose)
    {
        var lines = prose.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Length == 0)
            {
                doc.Blocks.Add(new Paragraph(new Run(" "))
                {
                    Margin = new Thickness(0),
                    LineHeight = 8,
                    FontSize = 4,
                });
                continue;
            }

            doc.Blocks.Add(MakeParagraph(
                IntercomMarkdown.ParseInline(line),
                13,
                FontWeights.Normal,
                new Thickness(0, i == 0 ? 0 : 1, 0, 0)));
        }
    }

    static Paragraph MakeParagraph(
        IReadOnlyList<IntercomMarkdownRun> runs,
        double fontSize,
        FontWeight weight,
        Thickness margin)
    {
        var para = new Paragraph
        {
            Margin = margin,
            LineHeight = fontSize + 5,
            FontSize = fontSize,
            FontWeight = weight,
            TextAlignment = TextAlignment.Left,
            Foreground = BodyFg,
        };
        AppendDocRuns(para, runs);
        return para;
    }

    static void AppendDocRuns(Paragraph para, IReadOnlyList<IntercomMarkdownRun> runs)
    {
        if (runs.Count == 0)
        {
            para.Inlines.Add(new Run(""));
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
                    TextDecorations = LinkUnderline,
                },
                _ => new Run(run.Text),
            };
            para.Inlines.Add(inline);
        }

        if (para.Inlines.Count == 0)
            para.Inlines.Add(new Run(""));
    }

    static TextDecorationCollection CreateLinkUnderline()
    {
        var c = TextDecorations.Underline.Clone();
        c.Freeze();
        return c;
    }

    static SolidColorBrush brush(string hex)
    {
        var b = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        if (b.CanFreeze)
            b.Freeze();
        return b;
    }
}

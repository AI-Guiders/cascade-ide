#nullable enable

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CDP.GlassCockpit.Windows;

/// <summary>Markdig AST → WPF FlowDocument for Glass MarkdownPreview MFD.</summary>
public static class GlassMarkdownFlowDocumentBuilder
{
    static readonly Brush BodyFg = Freeze(new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)));
    static readonly Brush CodeFg = Freeze(new SolidColorBrush(Color.FromRgb(0xD4, 0xD4, 0xD4)));
    static readonly Brush CodeBg = Freeze(new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)));
    static readonly Brush LinkFg = Freeze(new SolidColorBrush(Color.FromRgb(0x7E, 0xB8, 0xFF)));
    static readonly FontFamily Mono = new("Consolas, Cascadia Mono, Courier New");

    public static FlowDocument Build(string markdown, MarkdownPipeline pipeline)
    {
        var doc = new FlowDocument
        {
            Background = Brushes.Transparent,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 13,
            Foreground = BodyFg,
            PagePadding = new Thickness(4, 0, 4, 8),
        };

        var md = Markdown.Parse(markdown, pipeline);
        foreach (var block in md)
            AppendBlock(doc.Blocks, block);

        if (doc.Blocks.Count == 0)
            doc.Blocks.Add(new Paragraph(new Run("(empty markdown)")));

        return doc;
    }

    static void AppendBlock(BlockCollection target, Markdig.Syntax.Block block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                target.Add(BuildHeading(heading));
                break;
            case ParagraphBlock para when para.Inline is not null:
                target.Add(BuildParagraph(para.Inline, 13, FontWeights.Normal, new Thickness(0, 2, 0, 4)));
                break;
            case FencedCodeBlock code:
                target.Add(BuildCodeBlock(code));
                break;
            case CodeBlock code:
                target.Add(BuildCodeBlock(code));
                break;
            case ListBlock list:
                foreach (var item in list)
                {
                    if (item is not ListItemBlock li || li.Count == 0)
                        continue;
                    foreach (var child in li)
                        AppendBlock(target, child);
                }
                break;
            case QuoteBlock quote:
                foreach (var child in quote)
                    AppendBlock(target, child);
                break;
            default:
                if (block is ContainerBlock container)
                {
                    foreach (var child in container)
                        AppendBlock(target, child);
                }
                break;
        }
    }

    static Paragraph BuildHeading(HeadingBlock heading)
    {
        var (size, weight, margin) = heading.Level switch
        {
            1 => (20.0, FontWeights.SemiBold, new Thickness(0, 10, 0, 6)),
            2 => (17.0, FontWeights.SemiBold, new Thickness(0, 8, 0, 4)),
            3 => (15.0, FontWeights.SemiBold, new Thickness(0, 6, 0, 3)),
            _ => (14.0, FontWeights.SemiBold, new Thickness(0, 4, 0, 2)),
        };
        return heading.Inline is null
            ? new Paragraph { Margin = margin }
            : BuildParagraph(heading.Inline, size, weight, margin);
    }

    static Paragraph BuildParagraph(ContainerInline inline, double fontSize, FontWeight weight, Thickness margin)
    {
        var p = new Paragraph { Margin = margin, FontSize = fontSize, FontWeight = weight };
        AppendInlines(p.Inlines, inline);
        return p;
    }

    static Paragraph BuildCodeBlock(Markdig.Syntax.Block code)
    {
        var text = code is LeafBlock leaf
            ? (leaf.Lines.ToString() ?? "").TrimEnd('\r', '\n')
            : "";
        var p = new Paragraph(new Run(text))
        {
            FontFamily = Mono,
            FontSize = 12,
            Foreground = CodeFg,
            Background = CodeBg,
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 4, 0, 4),
        };
        return p;
    }

    static void AppendInlines(InlineCollection target, ContainerInline? inline)
    {
        if (inline is null)
            return;

        for (var node = inline.FirstChild; node is not null; node = node.NextSibling)
        {
            switch (node)
            {
                case LiteralInline lit:
                    target.Add(new Run(lit.Content.ToString()));
                    break;
                case EmphasisInline emph:
                    var span = new Span();
                    if (emph.DelimiterCount >= 2)
                        span.FontWeight = FontWeights.SemiBold;
                    else
                        span.FontStyle = FontStyles.Italic;
                    AppendInlines(span.Inlines, emph);
                    target.Add(span);
                    break;
                case CodeInline code:
                    target.Add(new Run(code.Content)
                    {
                        FontFamily = Mono,
                        Foreground = CodeFg,
                        Background = CodeBg,
                    });
                    break;
                case LinkInline link:
                    var hl = new Hyperlink(new Run(link.FirstChild is LiteralInline l ? l.Content.ToString() : link.Url ?? ""))
                    {
                        NavigateUri = Uri.TryCreate(link.Url, UriKind.Absolute, out var uri) ? uri : null,
                        Foreground = LinkFg,
                        TextDecorations = TextDecorations.Underline,
                    };
                    target.Add(hl);
                    break;
                case LineBreakInline:
                    target.Add(new LineBreak());
                    break;
                case ContainerInline container:
                    AppendInlines(target, container);
                    break;
            }
        }
    }

    static SolidColorBrush Freeze(SolidColorBrush brush)
    {
        if (brush.CanFreeze)
            brush.Freeze();
        return brush;
    }
}

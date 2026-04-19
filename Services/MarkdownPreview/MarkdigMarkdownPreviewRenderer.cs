using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CascadeIDE.Services.MarkdownPreview;

/// <summary>Базовый native renderer для Markdown preview без зависимости от Markdown.Avalonia.</summary>
public sealed class MarkdigMarkdownPreviewRenderer : IMarkdownPreviewRenderer
{
    public Control Render(MarkdownPreviewPayload payload)
    {
        if (payload.Document is null)
            return BuildFallback(payload);

        var body = new StackPanel
        {
            Spacing = 12,
            Margin = new Avalonia.Thickness(16)
        };

        foreach (var block in payload.Document)
            body.Children.Add(RenderBlock(block));

        if (body.Children.Count == 0)
        {
            body.Children.Add(new TextBlock
            {
                Text = "Markdown document is empty.",
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap
            });
        }

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = body
        };
    }

    private static Control BuildFallback(MarkdownPreviewPayload payload)
    {
        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new StackPanel
            {
                Spacing = 12,
                Margin = new Avalonia.Thickness(16),
                Children =
                {
                    new TextBlock
                    {
                        Text = "Markdown preview rendered in fallback mode.",
                        FontWeight = FontWeight.SemiBold,
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = payload.ErrorMessage ?? "Unknown parser error.",
                        Opacity = 0.75,
                        TextWrapping = TextWrapping.Wrap
                    },
                    CreateCodeBlock(payload.RenderMarkdown)
                }
            }
        };
    }

    private static Control RenderBlock(Block block)
    {
        return block switch
        {
            HeadingBlock heading => RenderHeading(heading),
            ParagraphBlock paragraph => RenderParagraph(paragraph),
            QuoteBlock quote => RenderQuote(quote),
            ListBlock list => RenderList(list),
            FencedCodeBlock fenced => CreateCodeBlock(GetLeafText(fenced)),
            CodeBlock code => CreateCodeBlock(GetLeafText(code)),
            ThematicBreakBlock => new Border
            {
                Height = 1,
                Margin = new Avalonia.Thickness(0, 4),
                Background = new SolidColorBrush(Color.Parse("#40888888"))
            },
            LeafBlock leaf => RenderLeafFallback(leaf),
            ContainerBlock container => RenderContainer(container),
            _ => new TextBlock
            {
                Text = block.ToString() ?? "",
                TextWrapping = TextWrapping.Wrap
            }
        };
    }

    private static Control RenderContainer(ContainerBlock container)
    {
        var panel = new StackPanel { Spacing = 8 };
        foreach (var child in container)
        {
            if (child is Block block)
                panel.Children.Add(RenderBlock(block));
        }

        return panel;
    }

    private static Control RenderHeading(HeadingBlock heading)
    {
        var size = heading.Level switch
        {
            1 => 28d,
            2 => 24d,
            3 => 20d,
            4 => 18d,
            _ => 16d
        };

        return new TextBlock
        {
            Text = ExtractInlineText(heading.Inline),
            FontSize = size,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(0, 4, 0, 0)
        };
    }

    private static Control RenderParagraph(ParagraphBlock paragraph)
    {
        var text = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap
        };
        PopulateInlines(text.Inlines!, paragraph.Inline);
        return text;
    }

    private static Control RenderQuote(QuoteBlock quote)
    {
        var panel = new StackPanel { Spacing = 8 };
        foreach (var child in quote)
        {
            if (child is Block block)
                panel.Children.Add(RenderBlock(block));
        }

        return new Border
        {
            Padding = new Avalonia.Thickness(12, 8),
            BorderThickness = new Avalonia.Thickness(4, 0, 0, 0),
            BorderBrush = new SolidColorBrush(Color.Parse("#808A65FF")),
            Background = new SolidColorBrush(Color.Parse("#14000000")),
            Child = panel
        };
    }

    private static Control RenderList(ListBlock list)
    {
        var panel = new StackPanel { Spacing = 8 };
        var index = int.TryParse(list.OrderedStart, out var orderedStart) ? orderedStart : 1;
        foreach (var item in list)
        {
            if (item is not ListItemBlock listItem)
                continue;

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                ColumnSpacing = 8
            };

            row.Children.Add(new TextBlock
            {
                Text = list.IsOrdered ? $"{index}." : "\u2022",
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Avalonia.Thickness(0, 1, 0, 0)
            });

            var content = new StackPanel { Spacing = 6 };
            foreach (var child in listItem)
            {
                if (child is Block block)
                    content.Children.Add(RenderBlock(block));
            }

            Grid.SetColumn(content, 1);
            row.Children.Add(content);
            panel.Children.Add(row);
            if (list.IsOrdered)
                index++;
        }

        return panel;
    }

    private static Control RenderLeafFallback(LeafBlock leaf)
    {
        if (leaf.Inline is not null)
            return new TextBlock { Text = ExtractInlineText(leaf.Inline), TextWrapping = TextWrapping.Wrap };

        var text = GetLeafText(leaf);
        return string.IsNullOrWhiteSpace(text)
            ? new TextBlock { Text = "", Height = 0 }
            : new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
    }

    private static TextBox CreateCodeBlock(string? text)
    {
        return new TextBox
        {
            Text = text ?? "",
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Consolas,Cascadia Code,monospace"),
            Background = new SolidColorBrush(Color.Parse("#12000000")),
            BorderBrush = new SolidColorBrush(Color.Parse("#40888888")),
            BorderThickness = new Avalonia.Thickness(1),
            Padding = new Avalonia.Thickness(10),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    }

    private static string GetLeafText(LeafBlock block)
    {
        var text = block.Lines.ToString();
        return string.IsNullOrWhiteSpace(text) ? ExtractInlineText(block.Inline) : text;
    }

    private static string ExtractInlineText(ContainerInline? inline)
    {
        if (inline is null)
            return "";

        var sb = new StringBuilder();
        AppendInlineText(sb, inline);
        return sb.ToString().TrimEnd();
    }

    private static void AppendInlineText(StringBuilder sb, Markdig.Syntax.Inlines.Inline? inline)
    {
        for (var current = inline; current is not null; current = current.NextSibling)
        {
            switch (current)
            {
                case LiteralInline literal:
                    sb.Append(literal.Content.ToString());
                    break;
                case LineBreakInline:
                    sb.AppendLine();
                    break;
                case CodeInline code:
                    sb.Append('`').Append(code.Content).Append('`');
                    break;
                case LinkInline link when link.IsImage:
                    sb.Append("[Image");
                    var alt = ExtractInlineText(link);
                    if (!string.IsNullOrWhiteSpace(alt))
                        sb.Append(": ").Append(alt);
                    if (!string.IsNullOrWhiteSpace(link.Url))
                        sb.Append("] (").Append(link.Url).Append(')');
                    else
                        sb.Append(']');
                    break;
                case LinkInline link:
                    var text = ExtractInlineText(link);
                    if (!string.IsNullOrWhiteSpace(text))
                        sb.Append(text);
                    if (!string.IsNullOrWhiteSpace(link.Url))
                        sb.Append(" (").Append(link.Url).Append(')');
                    break;
                case EmphasisInline emphasis:
                    AppendInlineText(sb, emphasis.FirstChild);
                    break;
                case ContainerInline container:
                    AppendInlineText(sb, container.FirstChild);
                    break;
            }
        }
    }

    private static void PopulateInlines(InlineCollection inlines, ContainerInline? inline)
    {
        PopulateInlineRange(inlines, inline?.FirstChild);
    }

    private static void PopulateInlineRange(InlineCollection inlines, Markdig.Syntax.Inlines.Inline? firstChild)
    {
        for (var current = firstChild; current is not null; current = current.NextSibling)
        {
            switch (current)
            {
                case LiteralInline literal:
                    inlines.Add(new Run(literal.Content.ToString()));
                    break;
                case LineBreakInline:
                    inlines.Add(new LineBreak());
                    break;
                case CodeInline code:
                    inlines.Add(new Run($"`{code.Content}`"));
                    break;
                case EmphasisInline emphasis:
                    var span = emphasis.DelimiterChar == '~'
                        ? new Span()
                        : emphasis.DelimiterCount >= 2 ? new Bold() : new Italic();
                    PopulateInlineRange(span.Inlines, emphasis.FirstChild);
                    inlines.Add(span);
                    break;
                case LinkInline link when link.IsImage:
                    inlines.Add(new Run($"[Image: {ExtractInlineText(link)}]"));
                    if (!string.IsNullOrWhiteSpace(link.Url))
                        inlines.Add(new Run($" ({link.Url})"));
                    break;
                case LinkInline link:
                    var anchorText = ExtractInlineText(link);
                    inlines.Add(new Run(string.IsNullOrWhiteSpace(anchorText) ? link.Url ?? "" : anchorText));
                    if (!string.IsNullOrWhiteSpace(link.Url))
                        inlines.Add(new Run($" ({link.Url})"));
                    break;
                case ContainerInline container:
                    PopulateInlineRange(inlines, container.FirstChild);
                    break;
            }
        }
    }
}

#nullable enable

using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using CascadeIDE.Views;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CascadeIDE.Services.MarkdownPreview;

/// <summary>Базовый native renderer для Markdown preview без зависимости от Markdown.Avalonia.</summary>
public sealed class MarkdigMarkdownPreviewRenderer : IMarkdownPreviewRenderer
{
    private static readonly IBrush CodeAnchorLinkBrush = new SolidColorBrush(Color.Parse("#4FC1FF"));
    private static readonly IBrush DocLinkBrush = new SolidColorBrush(Color.Parse("#4FC1FF"));
    private static readonly IBrush ExternalLinkBrush = new SolidColorBrush(Color.Parse("#4FC1FF"));
    private static readonly IBrush TableBorderBrush = new SolidColorBrush(Color.Parse("#40888888"));
    private static readonly IBrush TableHeaderBackground = new SolidColorBrush(Color.Parse("#1AFFFFFF"));

    public Control Render(MarkdownPreviewPayload payload, MarkdownPreviewRenderContext? context = null)
    {
        if (payload.Document is null)
            return BuildFallback(payload);

        var ctx = context ?? new MarkdownPreviewRenderContext(payload.SourcePath, null);
        MarkdownPreviewHeadingSlug.ResetSlugCounts();

        try
        {
            var body = new StackPanel
            {
                Spacing = 12,
                Margin = new Avalonia.Thickness(16)
            };

            foreach (var block in payload.Document)
                body.Children.Add(RenderBlock(block, ctx));

            if (body.Children.Count == 0)
            {
                body.Children.Add(new TextBlock
                {
                    Text = "Markdown document is empty.",
                    Opacity = 0.7,
                    TextWrapping = TextWrapping.Wrap
                });
            }

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = body
            };
            ctx.Anchors.ScrollHost = scroll;
            return scroll;
        }
        catch (Exception ex)
        {
            return BuildFallback(payload with
            {
                ErrorMessage = string.IsNullOrWhiteSpace(payload.ErrorMessage)
                    ? $"Preview render failed: {ex.Message}"
                    : $"{payload.ErrorMessage} | Render: {ex.Message}"
            });
        }
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
                    CreatePlainCodeBox(payload.RenderMarkdown)
                }
            }
        };
    }

    private static Control RenderBlock(Block block, MarkdownPreviewRenderContext ctx)
    {
        Control control = block switch
        {
            Table table => RenderTable(table, ctx),
            HeadingBlock heading => RenderHeading(heading, ctx),
            ParagraphBlock paragraph => RenderParagraph(paragraph, ctx),
            QuoteBlock quote => RenderQuote(quote, ctx),
            ListBlock list => RenderList(list, ctx),
            FencedCodeBlock fenced => MarkdownPreviewFencedCodeHighlighter.Create(fenced),
            CodeBlock code => CreatePlainCodeBox(GetLeafText(code)),
            HtmlBlock html => RenderHtmlBlock(html, ctx),
            ThematicBreakBlock => new Border
            {
                Height = 1,
                Margin = new Avalonia.Thickness(0, 4),
                Background = new SolidColorBrush(Color.Parse("#40888888"))
            },
            LeafBlock leaf => RenderLeafFallback(leaf, ctx),
            ContainerBlock container => RenderContainer(container, ctx),
            _ => new TextBlock
            {
                Text = GetUnknownBlockText(block),
                Opacity = 0.85,
                TextWrapping = TextWrapping.Wrap
            }
        };

        RegisterBlockLineAnchor(block, control, ctx);
        return control;
    }

    private static void RegisterBlockLineAnchor(Block block, Control control, MarkdownPreviewRenderContext ctx)
    {
        if (block is not MarkdownObject md || md.Line <= 0)
            return;

        ctx.Anchors.RegisterLine(md.Line, control);
    }

    private static Control RenderHtmlBlock(HtmlBlock html, MarkdownPreviewRenderContext ctx)
    {
        var text = GetLeafText(html);
        var ids = MarkdownPreviewHeadingSlug.ExtractHtmlAnchorIds(text).ToArray();
        if (ids.Length > 0)
        {
            var panel = new StackPanel { Spacing = 0 };
            foreach (var id in ids)
            {
                var anchor = new Border { Height = 0, Width = 0, IsHitTestVisible = false };
                ctx.Anchors.RegisterFragment(id, anchor);
                panel.Children.Add(anchor);
            }

            return panel;
        }

        if (string.IsNullOrWhiteSpace(text))
            return new Border { Height = 0 };

        return new TextBlock
        {
            Text = text,
            Opacity = 0.8,
            FontFamily = new FontFamily("Consolas,Cascadia Code,monospace"),
            TextWrapping = TextWrapping.Wrap
        };
    }

    private static string GetUnknownBlockText(Block block)
    {
        if (block is LeafBlock leaf)
        {
            var text = GetLeafText(leaf);
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return block.GetType().Name;
    }

    private static Control RenderTable(Table table, MarkdownPreviewRenderContext ctx)
    {
        var columnCount = table.ColumnDefinitions?.Count ?? 0;
        if (columnCount == 0)
        {
            foreach (var row in table.OfType<TableRow>())
                columnCount = Math.Max(columnCount, row.Count);
        }

        if (columnCount == 0)
        {
            return new TextBlock
            {
                Text = "(empty table)",
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap
            };
        }

        var grid = new Grid();
        for (var c = 0; c < columnCount; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        var rowIndex = 0;
        foreach (var rowBlock in table)
        {
            if (rowBlock is not TableRow row)
                continue;

            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var isHeader = row.IsHeader;

            for (var col = 0; col < columnCount; col++)
            {
                Control cellContent;
                if (col < row.Count && row[col] is TableCell cell)
                    cellContent = RenderTableCell(cell, ctx);
                else
                    cellContent = new TextBlock();

                var border = new Border
                {
                    BorderBrush = TableBorderBrush,
                    BorderThickness = new Avalonia.Thickness(1),
                    Padding = new Avalonia.Thickness(8, 6),
                    Background = isHeader ? TableHeaderBackground : null,
                    Child = cellContent
                };

                Grid.SetRow(border, rowIndex);
                Grid.SetColumn(border, col);
                grid.Children.Add(border);
            }

            rowIndex++;
        }

        return new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = grid
        };
    }

    private static Control RenderTableCell(TableCell cell, MarkdownPreviewRenderContext ctx)
    {
        var panel = new StackPanel { Spacing = 4 };
        foreach (var child in cell)
        {
            if (child is Block block)
                panel.Children.Add(RenderBlock(block, ctx));
        }

        return panel;
    }

    private static Control RenderContainer(ContainerBlock container, MarkdownPreviewRenderContext ctx)
    {
        var panel = new StackPanel { Spacing = 8 };
        foreach (var child in container)
        {
            if (child is Block block)
                panel.Children.Add(RenderBlock(block, ctx));
        }

        return panel;
    }

    private static Control RenderHeading(HeadingBlock heading, MarkdownPreviewRenderContext ctx)
    {
        var size = heading.Level switch
        {
            1 => 28d,
            2 => 24d,
            3 => 20d,
            4 => 18d,
            _ => 16d
        };

        var headingText = ExtractInlineText(heading.Inline);
        var slug = MarkdownPreviewHeadingSlug.Create(headingText);

        var text = new TextBlock
        {
            FontSize = size,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(0, 4, 0, 0)
        };

        if (text.Inlines is { } inlines)
            PopulateInlines(inlines, heading.Inline, ctx);
        else
            text.Text = headingText;

        ctx.Anchors.RegisterFragment(slug, text);
        return text;
    }

    private static Control RenderParagraph(ParagraphBlock paragraph, MarkdownPreviewRenderContext ctx)
    {
        if (TryRenderStandaloneImage(paragraph, ctx) is { } imageBlock)
            return imageBlock;

        var text = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap
        };

        if (text.Inlines is { } inlines)
            PopulateInlines(inlines, paragraph.Inline, ctx);
        else
            text.Text = ExtractInlineText(paragraph.Inline);

        return text;
    }

    private static Control? TryRenderStandaloneImage(ParagraphBlock paragraph, MarkdownPreviewRenderContext ctx)
    {
        if (paragraph.Inline?.FirstChild is not LinkInline link || !link.IsImage || link.NextSibling is not null)
            return null;

        return MarkdownPreviewImageFactory.TryCreate(link.Url, ExtractInlineText(link), ctx.SourceFilePath)
               ?? new TextBlock { Text = "[Image]", Opacity = 0.7 };
    }

    private static Control RenderQuote(QuoteBlock quote, MarkdownPreviewRenderContext ctx)
    {
        var panel = new StackPanel { Spacing = 8 };
        foreach (var child in quote)
        {
            if (child is Block block)
                panel.Children.Add(RenderBlock(block, ctx));
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

    private static Control RenderList(ListBlock list, MarkdownPreviewRenderContext ctx)
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

            var bullet = TryGetTaskListMarker(listItem) ?? (list.IsOrdered ? $"{index}." : "\u2022");
            row.Children.Add(new TextBlock
            {
                Text = bullet,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Avalonia.Thickness(0, 1, 0, 0)
            });

            var content = new StackPanel { Spacing = 6 };
            foreach (var child in listItem)
            {
                if (child is Block block)
                    content.Children.Add(RenderBlock(block, ctx));
            }

            Grid.SetColumn(content, 1);
            row.Children.Add(content);
            panel.Children.Add(row);
            if (list.IsOrdered)
                index++;
        }

        return panel;
    }

    private static string? TryGetTaskListMarker(ListItemBlock listItem)
    {
        foreach (var child in listItem)
        {
            if (child is not ParagraphBlock paragraph || paragraph.Inline?.FirstChild is not TaskList task)
                continue;

            return task.Checked ? "\u2611" : "\u2610";
        }

        return null;
    }

    private static Control RenderLeafFallback(LeafBlock leaf, MarkdownPreviewRenderContext ctx)
    {
        if (leaf.Inline is not null)
        {
            var text = new TextBlock { TextWrapping = TextWrapping.Wrap };
            if (text.Inlines is { } inlines)
                PopulateInlines(inlines, leaf.Inline, ctx);
            else
                text.Text = ExtractInlineText(leaf.Inline);
            return text;
        }

        var plain = GetLeafText(leaf);
        return string.IsNullOrWhiteSpace(plain)
            ? new TextBlock { Text = "", Height = 0 }
            : new TextBlock { Text = plain, TextWrapping = TextWrapping.Wrap };
    }

    private static TextBox CreatePlainCodeBox(string? text) =>
        new()
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
        if (inline is LinkInline link)
            AppendInlineText(sb, link.FirstChild);
        else
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
                case TaskList task:
                    sb.Append(task.Checked ? "[x] " : "[ ] ");
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

    private static void PopulateInlines(
        InlineCollection inlines,
        ContainerInline? inline,
        MarkdownPreviewRenderContext ctx)
    {
        PopulateInlineRange(inlines, inline?.FirstChild, ctx);
    }

    private static void PopulateInlineRange(
        InlineCollection inlines,
        Markdig.Syntax.Inlines.Inline? firstChild,
        MarkdownPreviewRenderContext ctx)
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
                case TaskList task:
                    inlines.Add(new Run(task.Checked ? "\u2611 " : "\u2610 "));
                    break;
                case EmphasisInline emphasis:
                    var span = emphasis.DelimiterChar == '~'
                        ? CreateStrikethroughSpan()
                        : emphasis.DelimiterCount >= 2 ? new Bold() : new Italic();
                    PopulateInlineRange(span.Inlines, emphasis.FirstChild, ctx);
                    inlines.Add(span);
                    break;
                case LinkInline link when link.IsImage:
                    AddImageInline(inlines, link, ctx);
                    break;
                case LinkInline link when IsCodeAnchorPreviewLink(link):
                    AddCodeAnchorLink(inlines, link, ctx);
                    break;
                case LinkInline link:
                    AddDocumentLink(inlines, link, ctx);
                    break;
                case ContainerInline container:
                    PopulateInlineRange(inlines, container.FirstChild, ctx);
                    break;
            }
        }
    }

    private static Span CreateStrikethroughSpan() =>
        new() { TextDecorations = TextDecorations.Strikethrough };

    private static void AddImageInline(InlineCollection inlines, LinkInline link, MarkdownPreviewRenderContext ctx)
    {
        var image = MarkdownPreviewImageFactory.TryCreate(link.Url, ExtractInlineText(link), ctx.SourceFilePath);
        if (image is null)
        {
            inlines.Add(new Run($"[Image: {ExtractInlineText(link)}]"));
            return;
        }

        inlines.Add(new InlineUIContainer { Child = image });
    }

    private static void AddDocumentLink(InlineCollection inlines, LinkInline link, MarkdownPreviewRenderContext ctx)
    {
        var label = ExtractInlineText(link);
        if (string.IsNullOrWhiteSpace(label))
            label = link.Url ?? "";

        if (IsExternalLink(link))
        {
            AddClickableLink(inlines, label, ExternalLinkBrush, () => ctx.OpenLink?.Invoke(link.Url!));
            return;
        }

        if (IsFragmentOnlyLink(link))
        {
            AddClickableLink(inlines, label, DocLinkBrush, () => ctx.OpenLink?.Invoke(link.Url!));
            return;
        }

        if (IsNavigableDocumentLink(link, ctx))
        {
            var url = link.Url!;
            AddClickableLink(inlines, label, DocLinkBrush, () =>
            {
                var (path, fragment) = MarkdownPreviewRenderContext.SplitUrl(url);
                if (!string.IsNullOrWhiteSpace(path))
                    ctx.OpenLink?.Invoke(path);

                if (!string.IsNullOrWhiteSpace(fragment))
                    ctx.OpenLink?.Invoke("#" + fragment);
            });
            return;
        }

        inlines.Add(new Run(label)
        {
            Foreground = DocLinkBrush,
            TextDecorations = TextDecorations.Underline,
        });
    }

    private static void AddCodeAnchorLink(InlineCollection inlines, LinkInline link, MarkdownPreviewRenderContext ctx)
    {
        var text = ExtractInlineText(link);
        if (string.IsNullOrWhiteSpace(text))
            text = "code";

        AddClickableLink(inlines, text, CodeAnchorLinkBrush, () => ctx.OpenLink?.Invoke(link.Url!));
    }

    private static void AddClickableLink(
        InlineCollection inlines,
        string label,
        IBrush brush,
        Action onClick)
    {
        var linkText = new TextBlock
        {
            Text = label,
            Foreground = brush,
            TextDecorations = TextDecorations.Underline,
            Cursor = new Cursor(StandardCursorType.Hand),
        };

        linkText.PointerPressed += (_, e) =>
        {
            var point = e.GetCurrentPoint(linkText);
            if (!point.Properties.IsLeftButtonPressed)
                return;

            onClick();
            e.Handled = true;
        };

        inlines.Add(new InlineUIContainer { Child = linkText });
    }

    private static bool IsExternalLink(LinkInline link) =>
        !link.IsImage
        && !string.IsNullOrWhiteSpace(link.Url)
        && (link.Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || link.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

    private static bool IsFragmentOnlyLink(LinkInline link) =>
        !link.IsImage
        && !string.IsNullOrWhiteSpace(link.Url)
        && link.Url.StartsWith('#');

    private static bool IsNavigableDocumentLink(LinkInline link, MarkdownPreviewRenderContext ctx)
    {
        if (link.IsImage || string.IsNullOrWhiteSpace(link.Url) || IsCodeAnchorPreviewLink(link))
            return false;

        var (path, _) = MarkdownPreviewRenderContext.SplitUrl(link.Url);
        return !string.IsNullOrWhiteSpace(path) && ctx.ResolveNavigateTarget(path) is not null;
    }

    private static bool IsCodeAnchorPreviewLink(LinkInline link) =>
        !link.IsImage
        && !string.IsNullOrWhiteSpace(link.Url)
        && link.Url.StartsWith(MarkdownCodeAnchorPreviewExpander.UriScheme, StringComparison.OrdinalIgnoreCase);
}

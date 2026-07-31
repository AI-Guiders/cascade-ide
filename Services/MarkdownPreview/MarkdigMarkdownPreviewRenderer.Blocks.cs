#nullable enable

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using CascadeIDE.Views;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CascadeIDE.Services.MarkdownPreview;

public sealed partial class MarkdigMarkdownPreviewRenderer
{
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
}

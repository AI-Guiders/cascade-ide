#nullable enable

using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using CascadeIDE.Views;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CascadeIDE.Services.MarkdownPreview;

/// <summary>Базовый native renderer для Markdown preview без зависимости от Markdown.Avalonia.</summary>
public sealed partial class MarkdigMarkdownPreviewRenderer : IMarkdownPreviewRenderer
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

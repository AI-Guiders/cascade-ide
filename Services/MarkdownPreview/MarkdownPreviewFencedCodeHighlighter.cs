#nullable enable

using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Markdig.Syntax;

namespace CascadeIDE.Services.MarkdownPreview;

/// <summary>Лёгкая подсветка fenced code (без полноценного highlighter).</summary>
public static partial class MarkdownPreviewFencedCodeHighlighter
{
    private static readonly IBrush KeywordBrush = new SolidColorBrush(Color.Parse("#569CD6"));
    private static readonly IBrush StringBrush = new SolidColorBrush(Color.Parse("#CE9178"));
    private static readonly IBrush CommentBrush = new SolidColorBrush(Color.Parse("#6A9955"));
    private static readonly IBrush NumberBrush = new SolidColorBrush(Color.Parse("#B5CEA8"));
    private static readonly IBrush DefaultBrush = new SolidColorBrush(Color.Parse("#D4D4D4"));

    public static Control Create(FencedCodeBlock block)
    {
        var text = block.Lines.ToString();
        var language = block.Info?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? "";

        var textBlock = new TextBlock
        {
            FontFamily = new FontFamily("Consolas,Cascadia Code,monospace"),
            TextWrapping = TextWrapping.NoWrap,
            Foreground = DefaultBrush,
        };

        if (textBlock.Inlines is null)
            return CreatePlainCodeBox(text);

        if (IsCSharpLike(language))
            PopulateCSharpInlines(textBlock.Inlines, text);
        else if (IsJsonLike(language))
            PopulateJsonInlines(textBlock.Inlines, text);
        else
            textBlock.Inlines.Add(new Run(text));

        var scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = textBlock,
        };

        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#12000000")),
            BorderBrush = new SolidColorBrush(Color.Parse("#40888888")),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Child = scroll,
        };
    }

    private static bool IsCSharpLike(string language) =>
        language.Equals("csharp", StringComparison.OrdinalIgnoreCase)
        || language.Equals("cs", StringComparison.OrdinalIgnoreCase)
        || language.Equals("c#", StringComparison.OrdinalIgnoreCase);

    private static bool IsJsonLike(string language) =>
        language.Equals("json", StringComparison.OrdinalIgnoreCase);

    private static void PopulateCSharpInlines(InlineCollection inlines, string text)
    {
        foreach (Match match in CSharpTokenRegex().Matches(text))
        {
            var value = match.Value;
            var brush = match.Groups["keyword"].Success ? KeywordBrush
                : match.Groups["str"].Success ? StringBrush
                : match.Groups["comment"].Success ? CommentBrush
                : match.Groups["number"].Success ? NumberBrush
                : DefaultBrush;
            inlines.Add(new Run(value) { Foreground = brush });
        }
    }

    private static void PopulateJsonInlines(InlineCollection inlines, string text)
    {
        foreach (Match match in JsonTokenRegex().Matches(text))
        {
            var value = match.Value;
            var brush = match.Groups["key"].Success ? KeywordBrush
                : match.Groups["str"].Success ? StringBrush
                : match.Groups["number"].Success ? NumberBrush
                : DefaultBrush;
            inlines.Add(new Run(value) { Foreground = brush });
        }
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
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
        };

    [GeneratedRegex(
        @"(?<comment>//[^\n]*|/\*[\s\S]*?\*/)|(?<str>@?""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*')|(?<keyword>\b(?:abstract|as|async|await|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|do|double|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|goto|if|implicit|in|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|private|protected|public|readonly|record|ref|return|sealed|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|using|virtual|void|volatile|while|yield)\b)|(?<number>\b\d+(?:\.\d+)?\b)|(?<other>[^\n]+|\n)",
        RegexOptions.CultureInvariant)]
    private static partial Regex CSharpTokenRegex();

    [GeneratedRegex(
        @"(?<key>""(?:\\.|[^""\\])+""\s*:)|(?<str>""(?:\\.|[^""\\])+"")|(?<number>-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b)|(?<other>[^\n]+|\n)",
        RegexOptions.CultureInvariant)]
    private static partial Regex JsonTokenRegex();
}

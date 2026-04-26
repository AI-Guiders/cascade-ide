using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit.Rendering;

namespace CascadeIDE.Services;

/// <summary>
/// Intra-line inlay (тип для <c>var</c>) через публичный API AvaloniaEdit: <see cref="TextView.ElementGenerators" /> +
/// <see cref="VisualLineElementGenerator" /> c <see cref="VisualLineElement" /> с <c>DocumentLength = 0</c>.
/// </summary>
public sealed class VarInlayHintElementGenerator(Func<IReadOnlyList<EditorTrailingInlayPart>> getInlays) : VisualLineElementGenerator
{
    public override int GetFirstInterestedOffset(int startOffset)
    {
        var c = CurrentContext;
        if (c is null)
            return -1;
        IReadOnlyList<EditorTrailingInlayPart> parts;
        try
        {
            parts = getInlays();
        }
        catch
        {
            return -1;
        }
        if (parts.Count == 0)
            return -1;
        var line = c.VisualLine;
        if (c.Document is null)
            return -1;
        int segStart = line.FirstDocumentLine.Offset;
        int segEnd = line.FirstDocumentLine.Offset + line.FirstDocumentLine.Length;
        if (line.LastDocumentLine != line.FirstDocumentLine)
        {
            segEnd = line.LastDocumentLine.Offset + line.LastDocumentLine.Length;
        }
        int best = -1;
        foreach (var p in parts)
        {
            if (p.Label.Length == 0)
                continue;
            if (p.AnchorOffset < startOffset)
                continue;
            if (p.AnchorOffset < segStart || p.AnchorOffset >= segEnd)
                continue;
            if (best < 0 || p.AnchorOffset < best)
                best = p.AnchorOffset;
        }
        return best;
    }

    public override VisualLineElement? ConstructElement(int offset)
    {
        var c = CurrentContext;
        if (c is null)
            return null;
        IReadOnlyList<EditorTrailingInlayPart> parts;
        try
        {
            parts = getInlays();
        }
        catch
        {
            return null;
        }
        string? label = null;
        foreach (var p in parts)
        {
            if (p.Label.Length == 0)
                continue;
            if (p.AnchorOffset == offset)
            {
                label = p.Label;
                break;
            }
        }
        if (label is null)
            return null;
        return VarInlayVisualLineElement.Create(c, label);
    }
}

file static class VarInlayVisualLineElement
{
    public static VisualLineElement Create(ITextRunConstructionContext context, string label)
    {
        var tv = context.TextView;
        var props = new VisualLineElementTextRunProperties(context.GlobalTextRunProperties);
        props.SetForegroundBrush(EditorHudDiagnosticsChroma.InlayLabelBrush);
        var textLine = FormattedTextElement.PrepareText(
            TextFormatter.Current,
            label,
            props);
        int visual = VisualColumnsForWidth(tv, textLine.WidthIncludingTrailingWhitespace);
        return new VarInlayElement(textLine, visual);
    }

    private static int VisualColumnsForWidth(TextView textView, double pixelWidth)
    {
        double w = textView.WideSpaceWidth;
        if (w <= 0)
            w = 7.0;
        return Math.Max(1, (int)Math.Ceiling(pixelWidth / w));
    }

    private sealed class VarInlayElement(TextLine line, int visualLength) : VisualLineElement(visualLength, 0)
    {
        public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context) =>
            new InlayRun(line, TextRunProperties);
    }

    private sealed class InlayRun(TextLine line, TextRunProperties properties) : DrawableTextRun
    {
        public override ReadOnlyMemory<char> Text => ReadOnlyMemory<char>.Empty;
        public override TextRunProperties Properties { get; } = properties;

        public override double Baseline => line.Baseline;

        public override Size Size => new(line.WidthIncludingTrailingWhitespace, line.Height);

        public override void Draw(DrawingContext drawingContext, Point origin) =>
            line.Draw(drawingContext, origin);
    }
}

using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit.Rendering;

namespace CascadeIDE.Services;

/// <summary>
/// Intra-line inlay (тип <c>var</c>, <c>param:</c> у аргументов) через публичный API AvaloniaEdit: <see cref="TextView.ElementGenerators" /> +
/// <see cref="VisualLineElementGenerator" /> c <see cref="VisualLineElement" /> с <c>DocumentLength = 0</c>.
/// </summary>
public sealed class VarInlayHintElementGenerator : VisualLineElementGenerator
{
    /// <summary>
    /// В списке <see cref="TextView.ElementGenerators" /> нельзя иметь N ссылок (даже дубликатов) на inlay-генератор,
    /// иначе в одной <see cref="VisualLine" /> копилось N нуледлинных элементов (CascadeIDE patcheт AvaloniaEdit: один
    /// offset = один 0-width). Дополнительно: CWT+offset — остальные экземпляры с тем же offset возвращают null.
    /// <see cref="StartGeneration" /> сбрасывает CWT, чтобы пул <see cref="VisualLine" /> не тащил старые offset.
    /// </summary>
    private static readonly ConditionalWeakTable<VisualLine, HashSet<int>> ServedInlayOffsetByVisualLine = new();

    private Func<IReadOnlyList<EditorTrailingInlayPart>> _getInlays;
    private IReadOnlyList<EditorTrailingInlayPart> _generationInlays = [];
    private readonly Dictionary<int, string> _labelsByOffset = [];
    private readonly List<int> _sortedOffsets = [];

    public VarInlayHintElementGenerator(Func<IReadOnlyList<EditorTrailingInlayPart>> getInlays) =>
        _getInlays = getInlays;

    /// <summary>Обновить источник inlay: один экземпляр генератора на TextView, делегат меняется при Install.</summary>
    internal void SetInlayProvider(Func<IReadOnlyList<EditorTrailingInlayPart>> getInlays) =>
        _getInlays = getInlays;

    public override void StartGeneration(ITextRunConstructionContext context)
    {
        base.StartGeneration(context);
        ServedInlayOffsetByVisualLine.Remove(context.VisualLine);
        _labelsByOffset.Clear();
        _sortedOffsets.Clear();
        try
        {
            _generationInlays = _getInlays();
            foreach (var p in _generationInlays)
            {
                // Ignore empty/whitespace pseudo-labels to avoid "ghost" zero-length inlays.
                if (string.IsNullOrWhiteSpace(p.Label))
                    continue;
                if (_labelsByOffset.ContainsKey(p.AnchorOffset))
                    continue;
                _labelsByOffset.Add(p.AnchorOffset, p.Label);
                _sortedOffsets.Add(p.AnchorOffset);
            }
            _sortedOffsets.Sort();
            if (InlayHintTrace.IsDebug)
            {
                int lineNo = context.VisualLine.FirstDocumentLine.LineNumber;
                InlayHintTrace.LogDebug(
                    $"VarInlay StartGeneration line={lineNo} anchors={_sortedOffsets.Count} " +
                    $"preview=[{string.Join(",", _sortedOffsets.Take(12))}]");
            }
        }
        catch
        {
            _generationInlays = [];
            _labelsByOffset.Clear();
            _sortedOffsets.Clear();
        }
    }

    public override void FinishGeneration()
    {
        _generationInlays = [];
        _labelsByOffset.Clear();
        _sortedOffsets.Clear();
        base.FinishGeneration();
    }

    public override int GetFirstInterestedOffset(int startOffset)
    {
        var c = CurrentContext;
        if (c is null)
            return -1;
        if (_sortedOffsets.Count == 0)
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
        foreach (var anchorOffset in _sortedOffsets)
        {
            if (anchorOffset < startOffset)
                continue;
            if (anchorOffset < segStart || anchorOffset >= segEnd)
                continue;
            if (best < 0 || anchorOffset < best)
                best = anchorOffset;
        }
        return best;
    }

    public override VisualLineElement? ConstructElement(int offset)
    {
        var c = CurrentContext;
        if (c is null)
            return null;
        if (_labelsByOffset.Count == 0)
            return null;
        if (c.Document is null)
            return null;

        if (!_labelsByOffset.TryGetValue(offset, out var label))
            return null;
        var vline = c.VisualLine;
        if (!ServedInlayOffsetByVisualLine.TryGetValue(vline, out var seen))
        {
            seen = new HashSet<int>();
            ServedInlayOffsetByVisualLine.Add(vline, seen);
        }
        if (!seen.Add(offset))
        {
            if (InlayHintTrace.IsDebug)
            {
                InlayHintTrace.LogDebug(
                    $"VarInlay ConstructElement SKIP duplicate offset vline off={offset} line={vline.FirstDocumentLine.LineNumber}");
            }

            return null;
        }

        if (InlayHintTrace.IsDebug)
        {
            InlayHintTrace.LogDebug(
                $"VarInlay ConstructElement EMIT off={offset} line={vline.FirstDocumentLine.LineNumber} label={label}");
        }
        return VarInlayVisualLineElement.Create(c, offset, label);
    }
}

file static class VarInlayVisualLineElement
{
    public static VisualLineElement Create(ITextRunConstructionContext context, int documentOffset, string label)
    {
        var tv = context.TextView;
        int lineNo = context.VisualLine.FirstDocumentLine.LineNumber;
        var props = new VisualLineElementTextRunProperties(context.GlobalTextRunProperties);
        props.SetForegroundBrush(EditorHudDiagnosticsChroma.InlayLabelBrush);
        var textLine = FormattedTextElement.PrepareText(
            TextFormatter.Current,
            label,
            props);
        int visual = VisualColumnsForWidth(tv, textLine.WidthIncludingTrailingWhitespace);
        return new VarInlayElement(lineNo, documentOffset, label, textLine, visual);
    }

    private static int VisualColumnsForWidth(TextView textView, double pixelWidth)
    {
        double w = textView.WideSpaceWidth;
        if (w <= 0)
            w = 7.0;
        return Math.Max(1, (int)Math.Ceiling(pixelWidth / w));
    }

    private sealed class VarInlayElement(
        int lineNo,
        int documentOffset,
        string label,
        TextLine line,
        int visualLength) : VisualLineElement(visualLength, 0)
    {
        public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
        {
            // Один run на весь inlay: иначе у DrawableTextRun по умолчанию Length=1 и TextFormatter вызывает
            // GetTextRun/Draw на каждую визуальную колонку (дубликаты label на экране).
            int runLength = VisualColumn + VisualLength - startVisualColumn;
            if (runLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(startVisualColumn));
            return new InlayRun(lineNo, documentOffset, label, line, TextRunProperties, runLength);
        }
    }

    private sealed class InlayRun(
        int lineNo,
        int documentOffset,
        string label,
        TextLine line,
        TextRunProperties properties,
        int runLength) : DrawableTextRun
    {
        private static long s_drawSeq;

        public override int Length { get; } = runLength;

        public override ReadOnlyMemory<char> Text => ReadOnlyMemory<char>.Empty;
        public override TextRunProperties Properties { get; } = properties;

        public override double Baseline => line.Baseline;

        public override Size Size => new(line.WidthIncludingTrailingWhitespace, line.Height);

        public override void Draw(DrawingContext drawingContext, Point origin)
        {
            if (InlayHintTrace.IsDebug)
            {
                long n = Interlocked.Increment(ref s_drawSeq);
                InlayHintTrace.LogInlayDraw(
                    $"#{n} line={lineNo} docOff={documentOffset} label={FormatLabelForLog(label)} origin=({origin.X:F1},{origin.Y:F1})");
            }

            line.Draw(drawingContext, origin);
        }

        private static string FormatLabelForLog(string s)
        {
            s = s.ReplaceLineEndings(" ").Trim();
            return s.Length <= 48 ? s : s[..48] + "…";
        }
    }
}

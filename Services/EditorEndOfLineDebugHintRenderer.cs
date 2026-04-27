using System.Globalization;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace CascadeIDE.Services;

/// <summary>EOL debug-hints (при остановке): значения переменных/условий рядом со строкой.</summary>
public sealed class EditorEndOfLineDebugHintRenderer(Func<IReadOnlyList<EditorDebugHintStrip>> getHints)
    : IBackgroundRenderer
{
    private const double AfterTextGap = 10.0;
    private const double FontScale = 0.9;
    private static readonly IBrush s_hintBrush = new SolidColorBrush(Color.FromArgb(255, 255, 215, 120));
    private static readonly IBrush s_hintBackgroundBrush = new SolidColorBrush(Color.FromArgb(190, 36, 42, 54));

    public KnownLayer Layer => KnownLayer.Text;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView.Document is null || !textView.VisualLinesValid)
            return;

        IReadOnlyList<EditorDebugHintStrip> hints;
        try
        {
            hints = getHints();
        }
        catch
        {
            return;
        }

        if (hints.Count == 0)
            return;

        var byLine1 = new Dictionary<int, string>();
        foreach (var h in hints)
        {
            if (h.Line1 <= 0 || string.IsNullOrWhiteSpace(h.Label))
                continue;
            byLine1[h.Line1] = h.Label.Trim();
        }

        if (byLine1.Count == 0)
            return;

        if (InlayHintTrace.IsDebug)
            InlayHintTrace.LogDebug($"DebugHint.Draw hints={byLine1.Count}");

        var vlineByLine1 = new Dictionary<int, VisualLine>();
        foreach (var v in textView.VisualLines)
        {
            var lineNo = v.FirstDocumentLine.LineNumber;
            if (!byLine1.ContainsKey(lineNo))
                continue;
            if (!vlineByLine1.TryGetValue(lineNo, out var old) || v.VisualTop > old.VisualTop)
                vlineByLine1[lineNo] = v;
        }

        var rawSize = TextElement.GetFontSize(textView);
        if (rawSize <= 0 || double.IsNaN(rawSize))
            rawSize = 13.0;
        var font = Math.Max(10.0, rawSize * FontScale);
        var family = TextElement.GetFontFamily(textView) ?? FontFamily.Default;
        var typeface = new Typeface(
            family,
            TextElement.GetFontStyle(textView),
            TextElement.GetFontWeight(textView));

        foreach (var (line1, vline) in vlineByLine1)
        {
            if (!byLine1.TryGetValue(line1, out var label))
                continue;

            var docTextLen = 0;
            foreach (var e in vline.Elements)
                docTextLen += e.DocumentLength;

            var endPos = vline.GetVisualPosition(vline.VisualLength, VisualYPosition.Baseline);

            var ft = new FormattedText(
                label,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                font,
                s_hintBrush);
            var preferredX = endPos.X + (docTextLen > 0 ? AfterTextGap : 4);
            // Keep hints visible inside current viewport even on long lines.
            var maxVisibleX = textView.Bounds.Width - ft.Width - 8;
            var x = Math.Max(4, Math.Min(preferredX, maxVisibleX));
            var y = endPos.Y - ft.Baseline;
            var bgRect = new Rect(x - 4, y - 1, ft.Width + 8, ft.Height + 2);
            drawingContext.FillRectangle(s_hintBackgroundBrush, bgRect, 3);
            drawingContext.DrawText(ft, new Point(x, y));
        }
    }
}

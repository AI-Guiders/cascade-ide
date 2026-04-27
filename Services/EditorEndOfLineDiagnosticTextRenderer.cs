using System.Globalization;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Microsoft.CodeAnalysis;

namespace CascadeIDE.Services;

/// <summary>
/// Inline diagnostics в духе VS (experimental): одна краткая подпись (ошибка/предупреждение) в конце
/// <i>логической</i> строки, по нижнему визуальному сегменту при word wrap. Без <see cref="KnownLayer.Text" />
/// тело сообщения оказалось бы под визуализацией глифов.
/// </summary>
public sealed class EditorEndOfLineDiagnosticTextRenderer(Func<IReadOnlyList<EditorDiagnosticStrip>> getStrips)
    : IBackgroundRenderer
{
    private const int MaxMessageChars = 200;
    private const double AfterTextGap = 10.0;
    private const double FontScale = 0.9;

    public KnownLayer Layer => KnownLayer.Text;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView.Document is null)
            return;
        IReadOnlyList<EditorDiagnosticStrip> strips;
        try
        {
            strips = getStrips();
        }
        catch
        {
            return;
        }
        if (strips.Count == 0)
            return;
        if (!textView.VisualLinesValid)
            return;

        var byLine1 = new Dictionary<int, EditorDiagnosticStrip>();
        foreach (var s in strips)
        {
            if (byLine1.TryGetValue(s.Line1, out var existing) && !PreferOver(s, existing))
                continue;
            byLine1[s.Line1] = s;
        }
        if (byLine1.Count == 0)
            return;

        // Нижний сегмент на каждую логическую строку (для word wrap рисуем в конце последнего визуального куска).
        var vlineByLine1 = new Dictionary<int, VisualLine>();
        foreach (var v in textView.VisualLines)
        {
            int n = v.FirstDocumentLine.LineNumber;
            if (!byLine1.ContainsKey(n))
                continue;
            if (!vlineByLine1.TryGetValue(n, out var o) || v.VisualTop > o.VisualTop)
                vlineByLine1[n] = v;
        }

        IBrush eBrush = new SolidColorBrush(EditorHudDiagnosticsChroma.Error) { Opacity = 0.9 };
        IBrush wBrush = new SolidColorBrush(EditorHudDiagnosticsChroma.Warning) { Opacity = 0.88 };
        IBrush iBrush = new SolidColorBrush(EditorHudDiagnosticsChroma.Info) { Opacity = 0.85 };
        double rawSize = TextElement.GetFontSize(textView);
        if (rawSize <= 0 || double.IsNaN(rawSize))
            rawSize = 13.0;
        var font = rawSize * FontScale;
        if (font < 4)
            font = 10.5;
        var family = TextElement.GetFontFamily(textView) ?? FontFamily.Default;
        var typeface = new Typeface(
            family,
            TextElement.GetFontStyle(textView),
            TextElement.GetFontWeight(textView));

        foreach (var (line1, vline) in vlineByLine1)
        {
            if (!byLine1.TryGetValue(line1, out var strip))
                continue;
            int docTextLen = 0;
            foreach (var e in vline.Elements)
                docTextLen += e.DocumentLength;
            IBrush br = strip.Severity switch
            {
                DiagnosticSeverity.Error => eBrush,
                DiagnosticSeverity.Warning => wBrush,
                _ => iBrush,
            };
            var label = BuildLabel(strip);
            if (label.Length == 0)
                continue;
            if (!TryGetEndAnchorRect(textView, vline, docTextLen, out var endRect, out var baselineY))
                continue;

            // Baseline-выравнивание: опускаем origin по вертикали для FormattedText (origin — верх-лево).
            var ft = new FormattedText(
                label,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                font,
                br);
            double x = endRect.Left + (docTextLen > 0 ? AfterTextGap : 4);
            double y = baselineY - ft.Baseline;
            if (x + ft.Width > 32000)
                continue;
            drawingContext.DrawText(ft, new Point(x, y));
        }
    }

    private static bool PreferOver(EditorDiagnosticStrip a, EditorDiagnosticStrip b)
    {
        if (a.Severity != b.Severity)
            return a.Severity > b.Severity;
        if (a.Start != b.Start)
            return a.Start < b.Start;
        return a.Length < b.Length;
    }

    private static string BuildLabel(EditorDiagnosticStrip strip)
    {
        var msg = strip.Message.ReplaceLineEndings(" ").Trim();
        while (msg.Contains("  ", StringComparison.Ordinal))
            msg = msg.Replace("  ", " ", StringComparison.Ordinal);
        if (msg.Length > MaxMessageChars)
            msg = string.Concat(msg.AsSpan(0, MaxMessageChars - 1), "…");
        if (string.IsNullOrEmpty(strip.Id) || msg.StartsWith(strip.Id, StringComparison.Ordinal))
            return msg;
        return strip.Id + ": " + msg;
    }

    private static bool TryGetEndAnchorRect(
        TextView textView, VisualLine vline, int docTextLen, out Rect endRect, out double baselineY)
    {
        if (vline.Elements.Count == 0)
        {
            endRect = default;
            baselineY = 0;
            return false;
        }
        // Последний символ логического сегмента; для пустой строки — один символ с начала (часто нулевой ширины).
        int o = docTextLen <= 0
            ? vline.StartOffset
            : (vline.StartOffset + docTextLen - 1);
        var seg = new EolTextSegment(o, 1);
        Rect? chosen = null;
        foreach (var r in BackgroundGeometryBuilder.GetRectsForSegment(textView, seg))
        {
            chosen = r;
            break;
        }
        if (chosen is not { } rNonNull)
        {
            endRect = default;
            baselineY = 0;
            return false;
        }
        endRect = rNonNull;
        var p = vline.GetVisualPosition(vline.VisualLength, VisualYPosition.Baseline);
        baselineY = p.Y;
        return true;
    }

    private sealed class EolTextSegment(int offset, int length) : ISegment
    {
        public int Offset { get; } = offset;
        public int Length { get; } = length;
        int ISegment.EndOffset => Offset + Length;
    }
}

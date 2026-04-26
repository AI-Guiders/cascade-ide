using System.Globalization;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;

namespace CascadeIDE.Services;

/// <summary>
/// EOL inlay hints (тип для <c>var</c>) — после текста строки, вторичной типографикой. AvaloniaEdit пока не даёт
/// true intra-line inlay ([discussion #429](https://github.com/AvaloniaUI/AvaloniaEdit/discussions/429)); слой последний в списке, поверх wavy.
/// </summary>
public sealed class EditorInlayHintBackgroundRenderer(Func<IReadOnlyList<EditorTrailingInlayPart>> getInlays) : IBackgroundRenderer
{
    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView.Document is null)
            return;
        IReadOnlyList<EditorTrailingInlayPart> parts;
        try
        {
            parts = getInlays();
        }
        catch
        {
            return;
        }
        if (parts.Count == 0)
            return;
        const double size = 10.5d;
        var typeface = new Typeface("Consolas, Cascadia Code, monospace");
        var culture = CultureInfo.CurrentUICulture;
        foreach (var p in parts)
        {
            if (p.Label.Length == 0)
                continue;
            if (p.Line1 < 1 || p.Line1 > textView.Document.LineCount)
                continue;
            var docLine = textView.Document.GetLineByNumber(p.Line1);
            // После последнего символа тела строки (без учёта \r\n в Length).
            var tvp = new TextViewPosition(p.Line1, docLine.Length + 1);
            try
            {
                var pt = textView.GetVisualPosition(tvp, VisualYPosition.LineMiddle) - textView.ScrollOffset;
                var ft = new FormattedText(
                    p.Label,
                    culture,
                    FlowDirection.LeftToRight,
                    typeface,
                    size,
                    EditorHudDiagnosticsChroma.InlayLabelBrush);
                drawingContext.DrawText(ft, new Point(pt.X + 4, pt.Y - ft.Height * 0.5));
            }
            catch
            {
                // Позиция вне видимой визуальной линии (свернута/обёртка) — тихо пропускаем.
            }
        }
    }
}

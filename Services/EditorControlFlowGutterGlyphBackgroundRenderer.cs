using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using CascadeIDE.Features.WorkspaceNavigation.Application;

namespace CascadeIDE.Services;

/// <summary>Gutter-глифы control-flow, когда virtual spacing выключен (иначе рисуются в полосе lane).</summary>
public sealed class EditorControlFlowGutterGlyphBackgroundRenderer(
    Func<string?> getFilePath,
    Func<string?, IReadOnlyList<ControlFlowLineVisual>?> getLineVisuals,
    Func<string?, bool> isVirtualSpacingActive) : IBackgroundRenderer
{
    private const double R = EditorControlFlowVirtualSpacing.GlyphRadius;
    private const double LeftPad = EditorControlFlowVirtualSpacing.LanePadding;

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        var document = textView.Document;
        if (document is null || document.LineCount == 0 || !textView.VisualLinesValid)
            return;

        var filePath = getFilePath();
        if (isVirtualSpacingActive(filePath))
            return;

        IReadOnlyList<ControlFlowLineVisual>? visuals;
        try
        {
            visuals = getLineVisuals(filePath);
        }
        catch
        {
            return;
        }

        if (visuals is null || visuals.Count == 0)
            return;

        var rawSize = TextElement.GetFontSize(textView);
        if (rawSize <= 0 || double.IsNaN(rawSize))
            rawSize = 13.0;
        var glyphFont = Math.Clamp(rawSize * 0.72, 8.2, 11.5);
        var family = TextElement.GetFontFamily(textView) ?? FontFamily.Default;
        var typeface = new Typeface(
            family,
            TextElement.GetFontStyle(textView),
            FontWeight.SemiBold);

        foreach (var v in visuals)
        {
            if (v.LineOneBased < 1 || v.LineOneBased > document.LineCount)
                continue;
            if (!TryGetLineCenter(textView, v.LineOneBased, out var cx, out var cy))
                continue;

            ControlFlowEditorNodePainter.DrawNode(drawingContext, v, cx, cy, R, typeface, glyphFont);
        }
    }

    private static bool TryGetLineCenter(TextView textView, int lineOneBased, out double cx, out double cy)
    {
        cx = 0;
        cy = 0;
        if (lineOneBased < 1 || lineOneBased > textView.Document!.LineCount)
            return false;

        var line = textView.Document.GetLineByNumber(lineOneBased);
        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, line))
        {
            cy = rect.Top + rect.Height / 2;
            cx = rect.Left + LeftPad + R;
            return true;
        }

        return false;
    }
}

using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit.Rendering;
using CascadeIDE.Features.WorkspaceNavigation.Application;

namespace CascadeIDE.Services;

/// <summary>
/// Virtual Spacing: нулевая длина в документе, ненулевая ширина в визуальной строке — сдвигает текст вправо.
/// В полосе lane рисуются узлы CFG с заливкой как на мини-карте.
/// </summary>
public sealed class ControlFlowVirtualSpacingElementGenerator : VisualLineElementGenerator
{
    private static readonly ConditionalWeakTable<VisualLine, object> ServedLineStartMarker = new();

    private Func<bool> _isActive = static () => false;
    private Func<IReadOnlyList<ControlFlowLineVisual>?> _getLineVisuals = static () => null;

    public void SetActiveCheck(Func<bool> isActive) => _isActive = isActive;

    public void SetLineVisualsProvider(Func<IReadOnlyList<ControlFlowLineVisual>?> getLineVisuals) =>
        _getLineVisuals = getLineVisuals;

    public override void StartGeneration(ITextRunConstructionContext context)
    {
        base.StartGeneration(context);
        ServedLineStartMarker.Remove(context.VisualLine);
    }

    public override int GetFirstInterestedOffset(int startOffset)
    {
        if (!_isActive())
            return -1;

        var c = CurrentContext;
        if (c?.VisualLine is null)
            return -1;

        int lineStart = c.VisualLine.FirstDocumentLine.Offset;
        return lineStart >= startOffset ? lineStart : -1;
    }

    public override VisualLineElement? ConstructElement(int offset)
    {
        if (!_isActive())
            return null;

        var c = CurrentContext;
        if (c?.VisualLine is null || c.TextView is null)
            return null;

        var lineStart = c.VisualLine.FirstDocumentLine.Offset;
        if (offset != lineStart)
            return null;

        var vline = c.VisualLine;
        if (ServedLineStartMarker.TryGetValue(vline, out _))
            return null;

        ServedLineStartMarker.Add(vline, ServedMarker.Instance);

        int visualCols = EditorControlFlowVirtualSpacing.VisualColumnsForWidth(c.TextView);
        var lineOneBased = c.VisualLine.FirstDocumentLine.LineNumber;
        var visual = TryGetVisualForLine(lineOneBased);
        return ControlFlowVirtualSpacingVisualLineElement.Create(c, visualCols, visual);
    }

    private ControlFlowLineVisual? TryGetVisualForLine(int lineOneBased)
    {
        var list = _getLineVisuals();
        if (list is null)
            return null;

        foreach (var v in list)
        {
            if (v.LineOneBased == lineOneBased)
                return v;
        }

        return null;
    }
}

file static class ControlFlowVirtualSpacingVisualLineElement
{
    public static VisualLineElement Create(
        ITextRunConstructionContext context,
        int visualColumns,
        ControlFlowLineVisual? lineVisual) =>
        new SpacingElement(visualColumns, context.GlobalTextRunProperties, lineVisual, context.TextView);

    private sealed class SpacingElement(
        int visualLength,
        TextRunProperties textRunProperties,
        ControlFlowLineVisual? lineVisual,
        TextView textView) : VisualLineElement(visualLength, 0)
    {
        public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
        {
            int runLength = VisualColumn + VisualLength - startVisualColumn;
            if (runLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(startVisualColumn));

            double space = context.TextView.WideSpaceWidth;
            if (space <= 0)
                space = 7.0;

            double height = context.GlobalTextRunProperties.FontRenderingEmSize * 1.2;
            if (height <= 0)
                height = 16;

            return new SpacingRun(textRunProperties, runLength, space, height, lineVisual, textView);
        }
    }

    private sealed class SpacingRun(
        TextRunProperties properties,
        int length,
        double spaceWidth,
        double lineHeight,
        ControlFlowLineVisual? lineVisual,
        TextView textView) : DrawableTextRun
    {
        public override int Length { get; } = length;

        public override ReadOnlyMemory<char> Text => ReadOnlyMemory<char>.Empty;

        public override TextRunProperties Properties { get; } = properties;

        public override double Baseline => lineHeight * 0.8;

        public override Size Size => new(spaceWidth * Length, lineHeight);

        public override void Draw(DrawingContext drawingContext, Point origin)
        {
            if (lineVisual is null)
                return;

            var rawSize = TextElement.GetFontSize(textView);
            if (rawSize <= 0 || double.IsNaN(rawSize))
                rawSize = 13.0;
            var glyphFont = Math.Clamp(rawSize * 0.72, 8.2, 11.5);
            var family = TextElement.GetFontFamily(textView) ?? FontFamily.Default;
            var typeface = new Typeface(family, TextElement.GetFontStyle(textView), FontWeight.SemiBold);

            var cx = origin.X + Size.Width / 2;
            var cy = origin.Y + lineHeight / 2;
            ControlFlowEditorNodePainter.DrawNode(
                drawingContext,
                lineVisual,
                cx,
                cy,
                EditorControlFlowVirtualSpacing.GlyphRadius,
                typeface,
                glyphFont);
        }
    }
}

file static class ServedMarker
{
    public static readonly object Instance = new();
}

using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit.Rendering;

namespace CascadeIDE.Services;

/// <summary>
/// Virtual Spacing: нулевая длина в документе, ненулевая ширина в визуальной строке — сдвигает текст вправо.
/// </summary>
public sealed class ControlFlowVirtualSpacingElementGenerator : VisualLineElementGenerator
{
    private static readonly ConditionalWeakTable<VisualLine, object> ServedLineStartMarker = new();

    private Func<bool> _isActive = static () => false;

    public void SetActiveCheck(Func<bool> isActive) => _isActive = isActive;

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
        return ControlFlowVirtualSpacingVisualLineElement.Create(c, visualCols);
    }
}

file static class ControlFlowVirtualSpacingVisualLineElement
{
    public static VisualLineElement Create(ITextRunConstructionContext context, int visualColumns) =>
        new SpacingElement(visualColumns, context.GlobalTextRunProperties);

    private sealed class SpacingElement(int visualLength, TextRunProperties textRunProperties)
        : VisualLineElement(visualLength, 0)
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

            return new SpacingRun(textRunProperties, runLength, space, height);
        }
    }

    private sealed class SpacingRun(
        TextRunProperties properties,
        int length,
        double spaceWidth,
        double lineHeight) : DrawableTextRun
    {
        public override int Length { get; } = length;

        public override ReadOnlyMemory<char> Text => ReadOnlyMemory<char>.Empty;

        public override TextRunProperties Properties { get; } = properties;

        public override double Baseline => lineHeight * 0.8;

        public override Size Size => new(spaceWidth * Length, lineHeight);

        public override void Draw(DrawingContext drawingContext, Point origin)
        {
        }
    }

}

file static class ServedMarker
{
    public static readonly object Instance = new();
}

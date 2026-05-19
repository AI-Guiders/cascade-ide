using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace CascadeIDE.Services;

/// <summary>
/// Transient подсветка диапазона строк для <c>reveal_editor_range</c> (ADR 0130).
/// Не меняет <see cref="AvaloniaEdit.Editing.Selection"/> редактора.
/// </summary>
public sealed class EditorAgentRangeRevealBackgroundRenderer(Func<(int startLine, int endLine)?> getRange) : IBackgroundRenderer
{
    private static readonly SolidColorBrush s_fill = new(Color.FromArgb(48, 255, 107, 157));
    private static readonly Pen s_border = new(new SolidColorBrush(Color.FromRgb(255, 107, 157)), 1.2);

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        var range = getRange();
        if (range is null)
            return;

        var document = textView.Document;
        if (document is null)
            return;

        var (startLine, endLine) = range.Value;
        if (startLine < 1 || endLine < startLine || startLine > document.LineCount)
            return;

        endLine = Math.Min(endLine, document.LineCount);

        var first = document.GetLineByNumber(startLine);
        var last = document.GetLineByNumber(endLine);
        var segment = new TextSegment(first.Offset, last.EndOffset - first.Offset);
        if (!TryGetUnitedRect(textView, segment, out var block))
            return;

        drawingContext.DrawRectangle(s_fill, null, block);
        drawingContext.DrawRectangle(null, s_border, block);
    }

    private static bool TryGetUnitedRect(TextView textView, ISegment segment, out Rect block)
    {
        block = default;
        var hasRect = false;
        var left = double.MaxValue;
        var top = double.MaxValue;
        var right = double.MinValue;
        var bottom = double.MinValue;

        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
        {
            hasRect = true;
            left = Math.Min(left, rect.Left);
            top = Math.Min(top, rect.Top);
            right = Math.Max(right, rect.Right);
            bottom = Math.Max(bottom, rect.Bottom);
        }

        if (!hasRect)
            return false;

        block = new Rect(left, top, right - left, bottom - top);
        return true;
    }

    private sealed class TextSegment(int offset, int length) : ISegment
    {
        public int Offset { get; } = offset;
        public int Length { get; } = length;
        public int EndOffset => Offset + Length;
    }
}

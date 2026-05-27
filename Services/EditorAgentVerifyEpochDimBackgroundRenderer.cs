using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace CascadeIDE.Services;

/// <summary>
/// Приглушение всего документа, когда verify epoch устарел (ADR 0148 §8.1).
/// </summary>
public sealed class EditorAgentVerifyEpochDimBackgroundRenderer(Func<bool> isDimmed) : IBackgroundRenderer
{
    private static readonly SolidColorBrush s_dim = new(Color.FromArgb(56, 128, 128, 128));

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (!isDimmed())
            return;

        var document = textView.Document;
        if (document is null || document.LineCount == 0)
            return;

        var first = document.GetLineByNumber(1);
        var last = document.GetLineByNumber(document.LineCount);
        var segment = new TextSegment(first.Offset, last.EndOffset - first.Offset);
        if (!TryGetUnitedRect(textView, segment, out var block))
            return;

        drawingContext.DrawRectangle(s_dim, null, block);
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

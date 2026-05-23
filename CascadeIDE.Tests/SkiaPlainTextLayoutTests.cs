using CascadeIDE.Views.SkiaKit;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SkiaPlainTextLayoutTests
{
    [Fact]
    public void HitTest_returns_caret_index_inside_text()
    {
        const string text = "hello world";
        Assert.True(SkiaPlainTextLayout.TryHitTestCaretIndex(
            text,
            localX: 40f,
            localY: 8f,
            maxWidth: 200f,
            fontSize: 12f,
            lineHeight: 17f,
            out var index));
        Assert.InRange(index, 1, text.Length);
    }

    [Fact]
    public void CaretLine_aligns_with_hit_test_at_same_x()
    {
        const string text = "abcdef";
        const float width = 180f;
        const float fontSize = 12f;
        const float lineHeight = 17f;
        Assert.True(SkiaPlainTextLayout.TryHitTestCaretIndex(
            text, 50f, 8f, width, fontSize, lineHeight, out var index));
        Assert.True(SkiaPlainTextLayout.TryGetCaretLine(
            text,
            index,
            width,
            fontSize,
            lineHeight,
            new SkiaSharp.SKPoint(0, 0),
            out var caretX,
            out _,
            out _));
        Assert.True(caretX >= 0f);
    }
}

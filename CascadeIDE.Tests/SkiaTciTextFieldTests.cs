using CascadeIDE.Models;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SkiaTciTextFieldTests
{
    [Fact]
    public void CommandLine_hit_test_returns_caret_inside_mono_buffer()
    {
        const string text = "/intercom anchor peek";
        const float fontSize = 12f;
        var bounds = new SKRect(0, 0, 400, SkiaCommandLineStrip.MeasureHeight(null, fontSize, 10f));
        Assert.True(SkiaCommandLineStrip.TryHitTestCaretAtPoint(
            bounds,
            text,
            pointX: 120f,
            pointY: bounds.Top + SkiaCommandLineStrip.VerticalPadding + 10f,
            fontSize,
            scrollOffsetX: 0f,
            out var index));
        Assert.InRange(index, 1, text.Length);
    }

    [Fact]
    public void CommandLine_caret_rect_follows_index()
    {
        const string text = "/build";
        const float fontSize = 12f;
        var bounds = new SKRect(0, 0, 300, SkiaCommandLineStrip.MeasureHeight(null, fontSize, 10f));
        const int caret = 3;
        Assert.True(SkiaCommandLineStrip.TryGetCaretRect(
            bounds,
            text,
            caret,
            fontSize,
            scrollOffsetX: 0f,
            out var rect));
        Assert.True(rect.Width >= 1f);
        Assert.True(rect.Height > 4f);
    }

    [Fact]
    public void Horizontal_scroll_increases_when_text_wider_than_viewport()
    {
        var longText = new string('x', 120);
        var max = SkiaCommandLineStrip.MaxHorizontalScroll(longText, contentWidth: 80f, fontSize: 12f);
        Assert.True(max > 0f);
    }

    [Fact]
    public void MeasureHeight_scales_with_default_command_line_pt()
    {
        var h12 = SkiaCommandLineStrip.MeasureHeight("err", 12f, 10f);
        var h15 = SkiaCommandLineStrip.MeasureHeight("err", 15f, 12.5f);
        Assert.True(h15 > h12);
        Assert.True(h15 >= 15f + SkiaCommandLineStrip.VerticalPadding * 2);
    }

    [Fact]
    public void Leading_chip_gutter_only_when_icon_on_left()
    {
        const float fontSize = 12f;
        var bounds = new SKRect(0, 0, 400, SkiaCommandLineStrip.MeasureHeight("/x", fontSize, 10f));
        try
        {
            SkiaSlashCommandChip.ConfigureIconPlacement(TciValidationIconModes.Left);
            var plain = SkiaCommandLineStrip.ComputeInputRegion(bounds, fontSize);
            var withChip = SkiaCommandLineStrip.ComputeInputRegion(bounds, fontSize, reserveLeadingChip: true);
            Assert.True(withChip.TextBounds.Left > plain.TextBounds.Left);
        }
        finally
        {
            SkiaSlashCommandChip.ConfigureIconPlacement(TciValidationIconModes.Right);
        }
    }

    [Fact]
    public void Input_region_fits_measured_glyph_height()
    {
        const float fontSize = 15f;
        var bounds = new SKRect(0, 0, 400, SkiaCommandLineStrip.MeasureHeight("Нет такой команды.", fontSize, fontSize * (10f / 12f)));
        var region = SkiaCommandLineStrip.ComputeInputRegion(bounds, fontSize);
        var bodyHeight = SkiaPlainTextLayout.MeasureBodyHeight(
            "/test",
            region.ContentWidth,
            fontSize,
            SkiaCommandLineStrip.InputLineHeightFor(fontSize),
            maxLines: 1,
            SkiaCommandLineStrip.MonoFontFamily);
        Assert.True(bodyHeight + region.TextTopInset * 2 <= region.TextBounds.Height + 1f);
    }
}

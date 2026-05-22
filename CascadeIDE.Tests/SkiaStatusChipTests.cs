using CascadeIDE.Views.SkiaKit;
using SkiaSharp;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SkiaStatusChipTests
{
    [Fact]
    public void ComputeRectAroundTextStart_places_icon_left_of_text()
    {
        var rect = SkiaStatusChip.ComputeRectAroundTextStart(
            textLeft: 100f,
            textTop: 10f,
            lineHeight: 22f,
            labelWidth: 80f);

        Assert.Equal(100f, SkiaStatusChip.ContentLeftInRect(rect), 0.5f);
        Assert.True(rect.Left < 100f);
        Assert.True(rect.Width >= 80f + SkiaStatusChip.IconBox);
    }

    [Fact]
    public void GlyphFor_maps_standard_severities()
    {
        Assert.Equal("\u2713", SkiaStatusChip.GlyphFor(SkiaStatusChipSeverity.Success));
        Assert.Equal("\u26A0", SkiaStatusChip.GlyphFor(SkiaStatusChipSeverity.Warning));
        Assert.Equal("\u2715", SkiaStatusChip.GlyphFor(SkiaStatusChipSeverity.Error));
    }
}

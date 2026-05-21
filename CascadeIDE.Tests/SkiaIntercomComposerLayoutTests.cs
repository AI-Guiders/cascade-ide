using CascadeIDE.Views.Chat;
using CascadeIDE.Views.SkiaKit;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SkiaIntercomComposerLayoutTests
{
    private const float DefaultFontSize = 12f;
    private const float DefaultLineHeight = 17f;

    [Fact]
    public void MeasureBottomChrome_includes_composer_and_slash_popup()
    {
        var width = 420f;
        var composerOnly = SkiaIntercomComposerLayout.MeasureBottomChromeHeight(
            showComposer: true,
            showSlashPopup: false,
            slashRowCount: 0,
            composerText: "hello",
            surfaceWidth: width);
        var minH = SkiaComposerStrip.MinHeightFor(DefaultLineHeight);
        var maxH = SkiaComposerStrip.MaxHeightFor(DefaultLineHeight);
        Assert.InRange(composerOnly, minH, maxH + 2);

        var withPopup = SkiaIntercomComposerLayout.MeasureBottomChromeHeight(
            showComposer: true,
            showSlashPopup: true,
            slashRowCount: 3,
            composerText: "hello",
            surfaceWidth: width);
        Assert.True(withPopup > composerOnly);
        Assert.InRange(withPopup - composerOnly, SkiaPopupList.MeasureHeight(3), SkiaPopupList.MeasureHeight(3) + 8f);
    }

    [Fact]
    public void Composer_grows_with_multiline_text()
    {
        var width = 400f;
        var min = SkiaComposerStrip.MeasureHeight("", null, width, DefaultFontSize, DefaultLineHeight);
        var fourLines = SkiaComposerStrip.MeasureHeight(
            "line one\nline two\nline three\nline four",
            null,
            width,
            DefaultFontSize,
            DefaultLineHeight);
        Assert.Equal(SkiaComposerStrip.MinHeightFor(DefaultLineHeight), min);
        Assert.True(fourLines > min);
    }

    [Fact]
    public void Composer_empty_uses_min_three_lines_height()
    {
        var height = SkiaComposerStrip.MeasureHeight("", null, 400f, DefaultFontSize, DefaultLineHeight);
        Assert.Equal(SkiaComposerStrip.MinHeightFor(DefaultLineHeight), height);
        Assert.True(height >= SkiaComposerStrip.VerticalPadding * 2 + SkiaComposerStrip.MinLines * DefaultLineHeight);
    }
}

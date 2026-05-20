using CascadeIDE.Views.Chat;
using CascadeIDE.Views.SkiaKit;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SkiaIntercomComposerLayoutTests
{
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
        Assert.InRange(composerOnly, SkiaComposerStrip.MinHeight, SkiaComposerStrip.MaxHeight + 2);

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
        var min = SkiaComposerStrip.MeasureHeight("", null, width);
        var fourLines = SkiaComposerStrip.MeasureHeight(
            "line one\nline two\nline three\nline four",
            null,
            width);
        Assert.Equal(SkiaComposerStrip.MinHeight, min);
        Assert.True(fourLines > min);
    }

    [Fact]
    public void Composer_empty_uses_min_three_lines_height()
    {
        var height = SkiaComposerStrip.MeasureHeight("", null, 400f);
        Assert.Equal(SkiaComposerStrip.MinHeight, height);
        Assert.True(height >= SkiaComposerStrip.VerticalPadding * 2 + SkiaComposerStrip.MinLines * SkiaComposerStrip.LineHeight);
    }
}

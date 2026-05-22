using CascadeIDE.Features.Chat;
using CascadeIDE.Views.SkiaKit;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ComposerSlashPreviewTests
{
    [Fact]
    public void TryGetSlashLineAtCaret_returns_line_for_multiline_composer()
    {
        const string text = "hello\n/intercom test";
        Assert.True(ChatSlashAutocomplete.TryGetSlashLineAtCaret(text, text.Length, out var line));
        Assert.Equal("/intercom test", line);
    }

    [Fact]
    public void Composer_preview_matches_ccl_builder_for_same_slash_line()
    {
        const string slash = "/intercom message select";
        var service = new SlashCommandPreviewService();
        var ccl = service.Evaluate(slash);
        var composer = service.Evaluate(slash);
        Assert.Equal(ccl.Kind, composer.Kind);
        Assert.Equal(ccl.Text, composer.Text);
    }

    [Fact]
    public void MeasureHeight_unchanged_when_slash_preview_is_chip_only()
    {
        var h0 = SkiaComposerStrip.MeasureHeight("x", null, 200f, 12f, 17f);
        var h1 = SkiaComposerStrip.MeasureHeight("x", null, 200f, 12f, 17f, "Нет такой команды.", 10f);
        Assert.Equal(h0, h1);
    }
}

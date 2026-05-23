using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SlashCommandPreviewServiceTests
{
    [Fact]
    public void Service_matches_evaluator_for_buffer()
    {
        const string slash = "/intercom message select 5 7";
        var service = new SlashCommandPreviewService();
        var direct = SlashCommandPreviewEvaluator.Evaluate(slash);
        var viaService = service.Evaluate(slash);
        Assert.Equal(direct.Kind, viaService.Kind);
        Assert.Equal(direct.Text, viaService.Text);
    }

    [Fact]
    public void EvaluateComposerAtCaret_uses_slash_line_only()
    {
        const string text = "note\n/intercom test";
        var service = new SlashCommandPreviewService();
        var preview = service.EvaluateComposerAtCaret(text, text.Length);
        Assert.Equal(SlashCommandPreviewKind.Error, preview.Kind);
    }
}

using CascadeIDE.Features.Chat;
using CascadeIDE.Services.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SlashCommandPreviewEvaluatorTests
{
    [Fact]
    public void Unknown_intercom_command_is_error()
    {
        var preview = SlashCommandPreviewEvaluator.Evaluate("/intercom test");
        Assert.Equal(SlashCommandPreviewKind.Error, preview.Kind);
        Assert.Contains("Нет такой команды", preview.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Message_select_without_args_is_incomplete()
    {
        var preview = SlashCommandPreviewEvaluator.Evaluate("/intercom message select");
        Assert.Equal(SlashCommandPreviewKind.Incomplete, preview.Kind);
        Assert.Contains("диапазон", preview.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Message_select_with_range_is_ok()
    {
        var preview = SlashCommandPreviewEvaluator.Evaluate("/intercom message select 5 7");
        Assert.Equal(SlashCommandPreviewKind.Ok, preview.Kind);
        Assert.Contains("Сообщения", preview.Text);
    }

    [Fact]
    public void Typo_in_command_is_error()
    {
        var preview = SlashCommandPreviewEvaluator.Evaluate("/intercom mesage select 5?7");
        Assert.Equal(SlashCommandPreviewKind.Error, preview.Kind);
    }

    [Fact]
    public void Invalid_range_syntax_is_error()
    {
        var preview = SlashCommandPreviewEvaluator.Evaluate("/intercom message select [99;1]");
        Assert.Equal(SlashCommandPreviewKind.Error, preview.Kind);
    }

    [Fact]
    public void AnchorPeek_invalid_id_is_error()
    {
        var preview = SlashCommandPreviewEvaluator.Evaluate("/anchor peek zz");
        Assert.Equal(SlashCommandPreviewKind.Error, preview.Kind);
    }

    [Fact]
    public void AnchorPeek_without_id_is_incomplete()
    {
        var preview = SlashCommandPreviewEvaluator.Evaluate("/anchor peek");
        Assert.Equal(SlashCommandPreviewKind.Incomplete, preview.Kind);
    }

    [Fact]
    public void AnchorPeek_resolved_anchor_is_ok()
    {
        var preview = SlashCommandPreviewEvaluator.Evaluate(
            "/anchor peek abcd1234",
            static (string _, out SlashCommandPreviewResult result) =>
            {
                result = new(
                    "a:abcd1234  Foo  resolved",
                    SlashCommandPreviewKind.Ok);
                return true;
            });
        Assert.Equal(SlashCommandPreviewKind.Ok, preview.Kind);
    }

    [Fact]
    public void MapResolveOutcome_member_not_found_is_incomplete()
    {
        Assert.Equal(
            SlashCommandPreviewKind.Incomplete,
            SlashCommandPreviewEvaluator.MapResolveOutcome(IntercomAttachmentRevealPlan.OutcomeMemberNotFound));
    }
}

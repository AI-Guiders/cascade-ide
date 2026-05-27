using CascadeIDE.Features.Chat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomAnchorSlashTests
{
    [Theory]
    [InlineData("abcd1234", "abcd1234")]
    [InlineData("a:AbCd1234", "abcd1234")]
    public void TryNormalizeAnchorId_AcceptsForms(string raw, string expected)
    {
        Assert.True(IntercomAnchorSlash.TryNormalizeAnchorId(raw, out var id, out var error), error);
        Assert.Equal(expected, id);
    }

    [Fact]
    public void TryNormalizeAnchorId_AcceptsWireMarker()
    {
        var raw = IntercomAttachmentMarkers.FormatMarker("ef001122");
        Assert.True(IntercomAnchorSlash.TryNormalizeAnchorId(raw, out var id, out var error), error);
        Assert.Equal("ef001122", id);
    }

    [Fact]
    public void ResolveInput_IntercomMessageAnchorsList()
    {
        ChatSlashCatalogTestSupport.AssertResolves("/intercom message anchors list", "/intercom message anchors list");
        Assert.True(
            ChatSlashCommandCatalog.TryResolveInput("/intercom message anchors list", out var d, out _));
        Assert.True(IntentSlashCatalog.TryGetRoute(d.SlashPath, out var route));
        Assert.Equal("message_anchors_list", route.IntercomHandlerId);
    }

    [Fact]
    public void ResolveInput_AnchorPeek()
    {
        ChatSlashCatalogTestSupport.AssertResolves("/anchor peek abcd1234", "/anchor peek", "abcd1234");
        Assert.True(
            ChatSlashCommandCatalog.TryResolveInput("/anchor peek abcd1234", out var d, out _));
        Assert.True(IntentSlashCatalog.TryGetRoute(d.SlashPath, out var route));
        Assert.Equal("anchor_peek", route.IntercomHandlerId);
    }

    [Fact]
    public void PreviewBuilder_AnchorPeek()
    {
        Assert.True(SlashCommandPreviewEvaluator.TryEvaluateSummary("/anchor peek abcd1234", out var summary));
        Assert.Contains("Peek", summary);
    }

    [Fact]
    public void ResolveInput_IntercomAnchorPeek()
    {
        ChatSlashCatalogTestSupport.AssertResolves(
            "/intercom anchor peek abcd1234",
            "/intercom anchor peek",
            "abcd1234");
        Assert.Equal(
            "abcd1234",
            IntercomAnchorSlash.ExtractPeekIdTail("/intercom anchor peek", "abcd1234"));
    }

    [Fact]
    public void TryResolvePeekOrdinal_resolves_index_in_selected_message()
    {
        var anchors = new[]
        {
            new AttachmentAnchor { Id = "abcd1234", DisplayLabel = "A" },
            new AttachmentAnchor { Id = "ef005678", DisplayLabel = "B" },
        };

        Assert.True(IntercomAnchorSlash.TryResolvePeekOrdinal("2", anchors, out var anchor, out var ordinal));
        Assert.Equal(2, ordinal);
        Assert.Equal("ef005678", anchor.Id);
    }

    [Fact]
    public void Autocomplete_hex_entry_still_suppressed_for_static_segments()
    {
        var suggestions = ChatSlashAutocomplete.GetSuggestions("/anchor peek abcd");
        Assert.Empty(suggestions);
    }

    [Fact]
    public void AnchorPeek_Ordinal_in_preview()
    {
        var preview = SlashCommandPreviewEvaluator.Evaluate("/anchor peek 1");
        Assert.Equal(SlashCommandPreviewKind.Incomplete, preview.Kind);
    }
}

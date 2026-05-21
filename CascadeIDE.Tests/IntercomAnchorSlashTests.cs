using CascadeIDE.Features.Chat;
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
    public void Parse_IntercomMessageAnchorsList_ResolvesCatalog()
    {
        var parse = ChatSlashCommandParser.TryParse("/intercom message anchors list");
        Assert.True(parse.IsSlashLine);
        Assert.True(ChatSlashCommandCatalog.TryResolve(parse, out var d));
        Assert.Equal("/intercom message anchors list", d.SlashPath);
        Assert.True(IntentSlashCatalog.TryGetRoute(d.SlashPath, out var route));
        Assert.Equal("message_anchors_list", route.IntercomHandlerId);
    }

    [Fact]
    public void Parse_AnchorPeek_ResolvesCatalog()
    {
        var parse = ChatSlashCommandParser.TryParse("/anchor peek abcd1234");
        Assert.True(parse.IsSlashLine);
        Assert.Equal("abcd1234", parse.ArgsTail);
        Assert.True(ChatSlashCommandCatalog.TryResolve(parse, out var d));
        Assert.Equal("/anchor peek", d.SlashPath);
        Assert.True(IntentSlashCatalog.TryGetRoute(d.SlashPath, out var route));
        Assert.Equal("anchor_peek", route.IntercomHandlerId);
    }

    [Fact]
    public void PreviewBuilder_AnchorPeek()
    {
        Assert.True(CockpitCommandLinePreviewBuilder.TryBuild("/anchor peek abcd1234", out var summary));
        Assert.Contains("Peek", summary);
    }
}

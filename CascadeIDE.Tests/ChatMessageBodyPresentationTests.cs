using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatMessageBodyPresentationTests
{
    [Fact]
    public void SplitSegments_multiple_fences_returns_all_blocks()
    {
        var body = "intro\n```cs\nvar a = 1;\n```\nmid\n```txt\nline\n```\ntail";
        var segments = ChatMessageBodyPresentation.SplitSegments(body);

        Assert.Equal(5, segments.Count);
        Assert.Equal(ChatMessageBodySegmentKind.Prose, segments[0].Kind);
        Assert.Contains("intro", segments[0].Text, StringComparison.Ordinal);
        Assert.Equal(ChatMessageBodySegmentKind.Code, segments[1].Kind);
        Assert.Contains("var a = 1", segments[1].Text, StringComparison.Ordinal);
        Assert.Equal(ChatMessageBodySegmentKind.Prose, segments[2].Kind);
        Assert.Contains("mid", segments[2].Text, StringComparison.Ordinal);
        Assert.Equal(ChatMessageBodySegmentKind.Code, segments[3].Kind);
        Assert.Contains("line", segments[3].Text, StringComparison.Ordinal);
        Assert.Equal(ChatMessageBodySegmentKind.Prose, segments[4].Kind);
        Assert.Contains("tail", segments[4].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldUseDocumentLayout_detects_lists_and_headings()
    {
        Assert.True(ChatMessageBodyPresentation.ShouldUseDocumentLayout("## Title\n\n- one"));
        Assert.False(ChatMessageBodyPresentation.ShouldUseDocumentLayout("plain **bold** only"));
    }
}

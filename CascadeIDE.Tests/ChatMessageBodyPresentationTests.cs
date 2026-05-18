using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatMessageBodyPresentationTests
{
    [Fact]
    public void SplitSegments_extracts_fenced_code_block()
    {
        var body = "intro\n```csharp\nvar x = 1;\n```\noutro";
        var segments = ChatMessageBodyPresentation.SplitSegments(body);
        Assert.Equal(3, segments.Count);
        Assert.Equal(ChatMessageBodySegmentKind.Prose, segments[0].Kind);
        Assert.Equal(ChatMessageBodySegmentKind.Code, segments[1].Kind);
        Assert.Contains("var x = 1", segments[1].Text);
        Assert.Equal(ChatMessageBodySegmentKind.Prose, segments[2].Kind);
    }

    [Fact]
    public void IsCollapsedThinking_detects_prefix()
    {
        Assert.True(ChatMessageBodyPresentation.IsCollapsedThinking(
            ChatMessageBodyPresentation.CollapsedThinkingPrefix + "preview"));
        Assert.False(ChatMessageBodyPresentation.IsCollapsedThinking("full text"));
    }
}

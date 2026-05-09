using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatMessageIdTests
{
    [Fact]
    public void TryParse_invalid_returns_false()
    {
        Assert.False(ChatMessageId.TryParse("not-a-guid", out _));
        Assert.False(ChatMessageId.TryParse(null, out _));
    }

    [Fact]
    public void TryParse_valid_round_trips_to_Guid()
    {
        var g = Guid.NewGuid();
        Assert.True(ChatMessageId.TryParse(g.ToString("N"), out var id));
        Assert.Equal(g, id.Value);
        Guid implicitG = id;
        Assert.Equal(g, implicitG);
    }
}

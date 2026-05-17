using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatProductSpineTests
{
    [Fact]
    public void BuildAgentContextPrefix_OnlyWhenEnabled()
    {
        var spine = new ChatProductSpine("CIDE", "Topic cards", ["ADR 0096"], IncludeInAgentContext: false);
        Assert.Null(spine.BuildAgentContextPrefix());

        spine = spine with { IncludeInAgentContext = true };
        var prefix = spine.BuildAgentContextPrefix();
        Assert.NotNull(prefix);
        Assert.Contains("Topic cards", prefix, StringComparison.Ordinal);
        Assert.Contains("ADR 0096", prefix, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseMilestonesText_TrimsAndCaps()
    {
        var lines = ChatProductSpine.ParseMilestonesText("  a \n\nb\n" + string.Join('\n', Enumerable.Range(0, 12).Select(i => i.ToString())));
        Assert.Equal(8, lines.Count);
        Assert.Equal("a", lines[0]);
    }
}

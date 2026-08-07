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

    [Fact]
    public void FormatFaceStrip_HumanizesPreConditionAdoptedJargon()
    {
        var spine = new ChatProductSpine(
            "Glass PreCondition",
            "A6 ADOPTED denser — message select 0136/0138; residual Intercom slash",
            ["A6"],
            IncludeInAgentContext: true);
        var strip = ChatProductSpinePresentation.FormatFaceStrip(spine);
        Assert.Equal("Glass Done · message select ready", strip);
        Assert.DoesNotContain("PreCondition", strip, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ADOPTED", strip, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("0136", strip, StringComparison.Ordinal);
    }
}

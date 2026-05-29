using CascadeIDE.Features.CasaField.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class KbSectionLineResolverTests
{
    [Fact]
    public void FindSectionLine_matches_heading()
    {
        const string md = """
            # Title

            ## Online learning

            Body here.
            """;

        var line = KbSectionLineResolver.FindSectionLineInMarkdown(md, "Online learning");
        Assert.Equal(3, line);
    }

    [Fact]
    public void FindSectionLine_uses_tail_after_slash()
    {
        const string md = """
            ## Metrics / Compute cost

            Latency metrics.
            """;

        var line = KbSectionLineResolver.FindSectionLineInMarkdown(md, "Provenance / Metrics / Compute cost");
        Assert.Equal(1, line);
    }
}

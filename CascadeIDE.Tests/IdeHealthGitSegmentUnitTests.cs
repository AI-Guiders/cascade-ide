using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeHealthGitSegmentUnitTests
{
    [Fact]
    public void Compose_returns_workspace_stratum_and_preserves_texts()
    {
        var segment = IdeHealthGitSegmentUnit.Default.Compose("Git: dirty +2/-1", "main*");

        Assert.Equal(IdeHealthStratum.Workspace, segment.Stratum);
        Assert.Equal(IdeHealthScope.Solution, segment.Scope);
        Assert.Equal("Git: dirty +2/-1", segment.LineText);
        Assert.Equal("main*", segment.CockpitShort);
    }
}

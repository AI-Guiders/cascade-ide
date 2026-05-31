using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeCommandsSafetySemanticIdsTests
{
    [Fact]
    public void SafetyCommands_MatchAgentSafetyLevelConstants()
    {
        Assert.Equal(AgentSafetyLevel.Observe, IdeCommands.SetSafetyObserve);
        Assert.Equal(AgentSafetyLevel.Confirm, IdeCommands.SetSafetyConfirm);
        Assert.Equal(AgentSafetyLevel.Autonomous, IdeCommands.SetSafetyAutonomous);
    }
}

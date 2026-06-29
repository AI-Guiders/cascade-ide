using CascadeIDE.Features.Agent.Harness;
using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatHarnessCoordinatorTests
{
    [Fact]
    public void OnUserMessageCommitted_EmitsCheckpointAtThreshold()
    {
        var settings = new CascadeIdeSettings
        {
            Agent = new AgentSettings
            {
                Harness = new AgentHarnessSettings
                {
                    CheckpointEnabled = true,
                    CheckpointThresholdUserTurns = 3,
                    CheckpointRepeatEveryUserTurns = 3,
                },
            },
        };

        var coord = new ChatHarnessCoordinator(() => settings, executeIdeCommand: null);
        coord.BindSession(Guid.NewGuid());

        Assert.Equal(HarnessUserTurnResult.None, coord.OnUserMessageCommitted());
        Assert.Equal(HarnessUserTurnResult.None, coord.OnUserMessageCommitted());
        var third = coord.OnUserMessageCommitted();
        Assert.True(third.InjectCheckpoint);
        Assert.Contains("harness checkpoint", third.CheckpointUserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefreshHotContext_ParsesJsonContent()
    {
        var settings = new CascadeIdeSettings();
        var coord = new ChatHarnessCoordinator(
            () => settings,
            (_, _, _) => Task.FromResult("""{"content":"L0 stub"}"""));

        await coord.OnSessionInitializedAsync();

        Assert.Contains("L0 stub", coord.HotContextBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void OnThreadMessageCommitted_EmitsPreCompactAtThreshold()
    {
        var settings = new CascadeIdeSettings
        {
            Agent = new AgentSettings
            {
                Harness = new AgentHarnessSettings
                {
                    CheckpointOnContextPressure = true,
                    ContextPressureThreadMessageThreshold = 5,
                    ContextPressureRepeatEveryMessages = 5,
                },
            },
        };

        var coord = new ChatHarnessCoordinator(() => settings, executeIdeCommand: null);
        coord.BindSession(Guid.NewGuid());

        for (var i = 1; i < 4; i++)
            Assert.False(coord.OnThreadMessageCommitted(i).InjectPreCompact);

        var fifth = coord.OnThreadMessageCommitted(5);
        Assert.True(fifth.InjectPreCompact);
        Assert.Contains("preCompact", fifth.PreCompactUserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildTelemetryContextBlock_IncludesStaleFlag()
    {
        var settings = new CascadeIdeSettings();
        var coord = new ChatHarnessCoordinator(() => settings, executeIdeCommand: null);
        var block = coord.BuildTelemetryContextBlock(coord.GetTelemetry(), verifyEpochUiStale: true);
        Assert.Contains("verify_epoch_ui_stale: true", block, StringComparison.Ordinal);
    }
}

using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.Agent.Environment;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class AgentVerifyEpochFormatterTests
{
    [Fact]
    public void FormatCompletedChatTrace_UsesSemanticRungs()
    {
        var evt = new AgentRunCompleted(
            "deadbeef-cafe-babe-0000-000000000001",
            Green: true,
            MaxRungReached: VerifyRung.BuildAffected,
            TimeSlices:
            [
                new(AgentRunPhaseKind.Environment, 0.8, $"{VerifyRung.DiagnoseFiles} ok"),
                new(AgentRunPhaseKind.Environment, 9.1, $"{VerifyRung.BuildAffected} ok"),
                new(AgentRunPhaseKind.Reasoning, 4.2, null),
            ]);

        var text = AgentVerifyEpochFormatter.FormatCompletedChatTrace(evt);

        Assert.Contains("✓ diagnose.files", text);
        Assert.Contains("✓ build.affected", text);
        Assert.Contains("Status: green · max rung: build.affected", text);
    }

    [Fact]
    public void ShouldDisplayGreen_RequiresNotStale()
    {
        Assert.True(AgentVerifyEpochFormatter.ShouldDisplayGreen(true, isStale: false, VerifyRung.BuildAffected));
        Assert.False(AgentVerifyEpochFormatter.ShouldDisplayGreen(true, isStale: true, VerifyRung.BuildAffected));
        Assert.False(AgentVerifyEpochFormatter.ShouldDisplayGreen(false, isStale: false, VerifyRung.BuildAffected));
    }

    [Fact]
    public void Instrument_OnRunCompleted_FreezesRungList()
    {
        var instrument = new AgentVerifyEpochInstrument();
        instrument.OnRunStarted(new AgentRunStarted(
            "run-001",
            "snap-abc",
            "standard",
            "C:\\repo\\App.sln"));

        instrument.OnRunCompleted(new AgentRunCompleted(
            "run-001",
            Green: true,
            MaxRungReached: VerifyRung.BuildAffected,
            TimeSlices:
            [
                new(AgentRunPhaseKind.Environment, 0.5, $"{VerifyRung.DiagnoseFiles} ok"),
                new(AgentRunPhaseKind.Environment, 2.0, $"{VerifyRung.BuildAffected} ok"),
            ]));

        var snap = instrument.Snapshot();
        Assert.Contains("✓ build.affected", snap.ExpandedText);
        Assert.Contains("green (standard)", snap.ExpandedText);
        Assert.True(snap.DisplayGreen);
        Assert.Contains("✓ build.affected · green (standard)", snap.CompactLine);
    }

    [Fact]
    public void Instrument_OnEpochStale_SuppressesGreen()
    {
        var instrument = new AgentVerifyEpochInstrument();
        instrument.OnRunStarted(new AgentRunStarted("run-002", "snap", "standard", null));
        instrument.OnRunCompleted(new AgentRunCompleted(
            "run-002",
            Green: true,
            MaxRungReached: VerifyRung.BuildAffected,
            TimeSlices:
            [
                new(AgentRunPhaseKind.Environment, 1.0, $"{VerifyRung.BuildAffected} ok"),
            ]));
        instrument.OnEpochStale(new AgentVerifyEpochStale("run-002", "snap", "write_in_epoch"));

        var snap = instrument.Snapshot();
        Assert.False(snap.DisplayGreen);
        Assert.Contains("⚠ verify устарел", snap.CompactLine);
        Assert.True(snap.ShowRetry);
    }
}

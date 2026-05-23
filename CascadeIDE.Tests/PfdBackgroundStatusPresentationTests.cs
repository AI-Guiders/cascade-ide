using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.SolutionWarmup.Application;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class PfdBackgroundStatusPresentationTests
{
    private const string Ws = @"D:\repo";
    private const string Sln = @"D:\repo\app.sln";

    private static readonly HybridIndexSettings EnabledAuto = new()
    {
        Enabled = true,
        AutoReindexOnSolutionOpen = true,
    };

    [Fact]
    public void Compute_whenWarmupRunningAndHciPending_showsPreparing()
    {
        var warmup = new SolutionWarmupStateChanged(Ws, Sln, SolutionWarmupLifecycle.Running, null);
        var snap = PfdBackgroundStatusPresentation.Compute(Ws, Sln, warmup, null, hciReindexPending: true, EnabledAuto);

        Assert.True(snap.Show);
        Assert.False(snap.IsCaution);
        Assert.Equal("Preparing workspace…", snap.Text);
    }

    [Fact]
    public void Compute_whenStaleHciFromOtherSolution_stillShowsIndexing()
    {
        var oldHci = new HybridIndexStateChanged(Ws, @"D:\repo\other.sln", "", 10, null, null, null);
        var snap = PfdBackgroundStatusPresentation.Compute(Ws, Sln, null, oldHci, hciReindexPending: true, EnabledAuto);

        Assert.True(snap.Show);
        Assert.Equal("Indexing workspace…", snap.Text);
    }

    [Fact]
    public void Compute_whenHciError_showsCaution()
    {
        var hci = new HybridIndexStateChanged(Ws, Sln, "", 0, null, "disk full", null);
        var snap = PfdBackgroundStatusPresentation.Compute(Ws, Sln, null, hci, hciReindexPending: false, EnabledAuto);

        Assert.True(snap.Show);
        Assert.True(snap.IsCaution);
        Assert.Contains("Index error", snap.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Compute_whenCancelledScopeChange_doesNotShowCaution()
    {
        var warmup = new SolutionWarmupStateChanged(Ws, Sln, SolutionWarmupLifecycle.Cancelled, "scope_changed");
        var snap = PfdBackgroundStatusPresentation.Compute(Ws, Sln, warmup, null, hciReindexPending: true, EnabledAuto);

        Assert.True(snap.Show);
        Assert.False(snap.IsCaution);
        Assert.Equal("Indexing workspace…", snap.Text);
    }

    [Fact]
    public void Compute_whenIdleAndIndexed_hides()
    {
        var warmup = new SolutionWarmupStateChanged(Ws, Sln, SolutionWarmupLifecycle.Ready, null);
        var hci = new HybridIndexStateChanged(Ws, Sln, "", 42, null, null, null);
        var snap = PfdBackgroundStatusPresentation.Compute(Ws, Sln, warmup, hci, hciReindexPending: false, EnabledAuto);

        Assert.False(snap.Show);
        Assert.Null(snap.Text);
    }
}

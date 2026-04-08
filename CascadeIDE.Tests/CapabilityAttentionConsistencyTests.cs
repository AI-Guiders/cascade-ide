using CascadeIDE.Contracts.Experimental;
using CascadeIDE.Contracts.Experimental.Capabilities;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Services.Capabilities;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CapabilityAttentionConsistencyTests : IDisposable
{
    public CapabilityAttentionConsistencyTests() =>
        AttentionZonePanelRuntime.ResetToCodeDefaults();

    public void Dispose() =>
        AttentionZonePanelRuntime.ResetToCodeDefaults();

    [Fact]
    public void Both_set_and_matching_returns_no_issue()
    {
        var d = new UiSurfaceCapabilityDescriptor
        {
            Id = "ui.test.solution",
            OwnerModuleId = "test",
            DisplayName = "Solution",
            PrimaryAttentionZoneId = AttentionZoneCanonicalIds.Pfd,
            HostAttentionPanelId = AttentionPanelCanonicalIds.SolutionExplorer
        };
        Assert.Null(CapabilityAttentionConsistency.TryGetUiSurfaceIssue(d));
    }

    [Fact]
    public void Zone_mismatch_returns_issue()
    {
        var d = new UiSurfaceCapabilityDescriptor
        {
            Id = "ui.bad",
            OwnerModuleId = "test",
            PrimaryAttentionZoneId = AttentionZoneCanonicalIds.Mfd,
            HostAttentionPanelId = AttentionPanelCanonicalIds.SolutionExplorer
        };
        var issue = CapabilityAttentionConsistency.TryGetUiSurfaceIssue(d);
        Assert.NotNull(issue);
        Assert.Contains("does not match", issue, StringComparison.Ordinal);
        Assert.Contains("pfd", issue, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Unknown_panel_returns_issue()
    {
        var d = new UiSurfaceCapabilityDescriptor
        {
            Id = "ui.unknown",
            OwnerModuleId = "test",
            PrimaryAttentionZoneId = AttentionZoneCanonicalIds.Pfd,
            HostAttentionPanelId = "nonexistent_panel"
        };
        var issue = CapabilityAttentionConsistency.TryGetUiSurfaceIssue(d);
        Assert.NotNull(issue);
        Assert.Contains("not in AttentionZonePanelRuntime", issue, StringComparison.Ordinal);
    }

    [Fact]
    public void Only_zone_or_only_panel_skips_cross_check()
    {
        var zoneOnly = new UiSurfaceCapabilityDescriptor
        {
            Id = "a",
            OwnerModuleId = "m",
            PrimaryAttentionZoneId = AttentionZoneCanonicalIds.Mfd
        };
        Assert.Null(CapabilityAttentionConsistency.TryGetUiSurfaceIssue(zoneOnly));

        var panelOnly = new UiSurfaceCapabilityDescriptor
        {
            Id = "b",
            OwnerModuleId = "m",
            HostAttentionPanelId = AttentionPanelCanonicalIds.Editor
        };
        Assert.Null(CapabilityAttentionConsistency.TryGetUiSurfaceIssue(panelOnly));
    }
}

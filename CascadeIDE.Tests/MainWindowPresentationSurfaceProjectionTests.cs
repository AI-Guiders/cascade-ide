using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MainWindowPresentationSurfaceProjectionTests
{
    private sealed class TestInstrumentMountResolver : IInstrumentMountPolicyResolver
    {
        public Func<DisplaySettings, string, string, string, string>? Invoke { get; set; }

        public string Resolve(DisplaySettings displaySettings, string surfaceId, string slotId, string instrumentId) =>
            Invoke?.Invoke(displaySettings, surfaceId, slotId, instrumentId)
               ?? $"{surfaceId}|{slotId}|{instrumentId}";
    }
    [Fact]
    public void ResolveWindowTitle_matches_catalog_then_family_fallback()
    {
        foreach (var mode in new[] { "Flight", "Editor", "Debug", "" })
        {
            var normalized = UiModeCatalog.NormalizeUiMode(string.IsNullOrEmpty(mode) ? null : mode);
            var fromCatalog = UiModeCatalog.GetWindowTitleOverride(normalized);
            string expected = fromCatalog
                ?? UiModeFamilyResolver.FromNormalizedMode(normalized) switch
                {
                    UiModeFamily.Power => "CascadeIDE — Power Mode [Autonomous Agent Cockpit]",
                    UiModeFamily.AgentChat => "CascadeIDE — Agent Chat",
                    UiModeFamily.Debug => "CascadeIDE — Debug",
                    UiModeFamily.Editor => "CascadeIDE — Editor",
                    _ => "CascadeIDE",
                };
            Assert.Equal(expected, MainWindowPresentationSurfaceProjection.ResolveWindowTitle(normalized));
        }
    }

    [Fact]
    public void IsMfdContourContentVisible_requires_any_checked_layer()
    {
        Assert.False(MainWindowPresentationSurfaceProjection.IsMfdContourContentVisible(
            problemsPanelVisible: false,
            isTerminalVisible: false,
            isBuildOutputVisible: false,
            instrumentationTabs: false,
            isGitPanelVisible: false));
        Assert.True(MainWindowPresentationSurfaceProjection.IsMfdContourContentVisible(
            problemsPanelVisible: true,
            isTerminalVisible: false,
            isBuildOutputVisible: false,
            instrumentationTabs: false,
            isGitPanelVisible: false));
    }

    [Fact]
    public void MountPolicySurfaceId_docked_grid_stable()
    {
        Assert.Equal(
            "main_window_docked_grid",
            MainWindowPresentationSurfaceProjection.MountPolicySurfaceId(AttentionLayoutSurfaceKind.MainWindowDockedGrid));
    }

    [Fact]
    public void IsAgentSummary_visible_only_when_differs_from_placeholder()
    {
        var p = MainWindowPresentationSurfaceProjection.DefaultRiskSummaryPlaceholder;
        Assert.False(MainWindowPresentationSurfaceProjection.IsAgentSummaryVisibleComparedToPlaceholder(null, p));
        Assert.False(MainWindowPresentationSurfaceProjection.IsAgentSummaryVisibleComparedToPlaceholder("  ", p));
        Assert.False(MainWindowPresentationSurfaceProjection.IsAgentSummaryVisibleComparedToPlaceholder(p, p));
        Assert.True(MainWindowPresentationSurfaceProjection.IsAgentSummaryVisibleComparedToPlaceholder("risk text", p));
    }

    [Fact]
    public void IsMainGridSplitColumnVisible_positive_width_only()
    {
        Assert.False(MainWindowPresentationSurfaceProjection.IsMainGridSplitColumnVisible(0));
        Assert.True(MainWindowPresentationSurfaceProjection.IsMainGridSplitColumnVisible(1));
    }

    [Fact]
    public void IdeHealth_skia_mount_flags_combine_useSkia_and_column_or_host()
    {
        Assert.False(MainWindowPresentationSurfaceProjection.IsIdeHealthSkiaMountVisibleInDockedColumn(
            useSkiaInstrumentMount: false,
            columnVisible: true));
        Assert.False(MainWindowPresentationSurfaceProjection.IsIdeHealthSkiaMountVisibleInDockedColumn(
            useSkiaInstrumentMount: true,
            columnVisible: false));
        Assert.True(MainWindowPresentationSurfaceProjection.IsIdeHealthSkiaMountVisibleInDockedColumn(
            useSkiaInstrumentMount: true,
            columnVisible: true));

        Assert.False(MainWindowPresentationSurfaceProjection.IsIdeHealthSkiaMountVisibleForHostWindow(
            useSkiaInstrumentMount: true,
            hostShellOpen: false));
        Assert.True(MainWindowPresentationSurfaceProjection.IsIdeHealthSkiaMountVisibleForHostWindow(
            useSkiaInstrumentMount: true,
            hostShellOpen: true));
    }

    [Fact]
    public void ResolveInstrumentMountStyleForSlot_uses_MountPolicySurfaceId()
    {
        var resolver = new TestInstrumentMountResolver();
        DisplaySettings? capturedDisplay = null;
        string? capturedSurface = null;
        string? capturedSlot = null;
        string? capturedInstrument = null;
        resolver.Invoke = (d, surf, slot, instr) =>
        {
            capturedDisplay = d;
            capturedSurface = surf;
            capturedSlot = slot;
            capturedInstrument = instr;
            return "ok";
        };

        var display = new DisplaySettings();
        var style = MainWindowPresentationSurfaceProjection.ResolveInstrumentMountStyleForSlot(
            resolver,
            display,
            AttentionLayoutSurfaceKind.MainWindowPlusMfdHostTopLevel,
            "mfd",
            "inst-1");

        Assert.Equal("ok", style);
        Assert.Same(display, capturedDisplay);
        Assert.Equal(
            MainWindowPresentationSurfaceProjection.MountPolicySurfaceId(
                AttentionLayoutSurfaceKind.MainWindowPlusMfdHostTopLevel),
            capturedSurface);
        Assert.Equal("mfd", capturedSlot);
        Assert.Equal("inst-1", capturedInstrument);
    }

    [Fact]
    public void ResolveExpandedMfdWidthPixels_Flight_uses_agent_chat_width()
    {
        var w = MainWindowPresentationSurfaceProjection.ResolveExpandedMfdWidthPixels(
            "Flight",
            MfdShellPage.Terminal,
            PrimaryWorkSurfaceKind.Editor);
        Assert.Equal(UiWorkspaceLayoutRuntimeMetrics.MfdRegionExpandedAgentChatWidthPixels, w);
    }

    [Fact]
    public void ResolveExpandedMfdWidthPixels_Chat_page_at_least_agent_chat_width()
    {
        var w = MainWindowPresentationSurfaceProjection.ResolveExpandedMfdWidthPixels(
            "Editor",
            MfdShellPage.Chat,
            PrimaryWorkSurfaceKind.Editor);
        Assert.Equal(UiWorkspaceLayoutRuntimeMetrics.MfdRegionExpandedAgentChatWidthPixels, w);
    }

    [Fact]
    public void ResolveExpandedMfdWidthPixels_non_chat_uses_mode_default()
    {
        var w = MainWindowPresentationSurfaceProjection.ResolveExpandedMfdWidthPixels(
            "Editor",
            MfdShellPage.Terminal,
            PrimaryWorkSurfaceKind.Editor);
        Assert.Equal(UiWorkspaceLayoutRuntimeMetrics.MfdRegionExpandedDefaultWidthPixels, w);
    }
}

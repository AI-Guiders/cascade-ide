using CascadeIDE.Features.UiChrome;
using Xunit;

namespace CascadeIDE.Tests;

[Collection("UiModeCatalog")]
public sealed class UiModeCatalogTests : IDisposable
{
    public UiModeCatalogTests() =>
        UiModeCatalog.ResetForTests();

    public void Dispose() =>
        UiModeCatalog.ResetForTests();

    [Fact]
    public void Inherits_copies_debug_family_and_layout()
    {
        var dir = Path.Combine(Path.GetTempPath(), "uimodes_" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        File.WriteAllText(
            Path.Combine(dir, "index.toml"),
            """
            schema_version = 1
            modes = [ "Focus", "Balanced", "Power", "AgentChat", "Debug", "MySuperDebug" ]
            """);
        File.WriteAllText(
            Path.Combine(dir, "MySuperDebug.toml"),
            """
            inherits = "Debug"
            """);

        UiModeCatalog.Initialize(dir);

        Assert.Equal(UiModeFamily.Debug, UiModeCatalog.GetFamily("MySuperDebug"));
        var dbg = UiModeCatalog.GetSpec("Debug");
        var mine = UiModeCatalog.GetSpec("MySuperDebug");
        Assert.Equal(dbg.SolutionExplorerVisible, mine.SolutionExplorerVisible);
        Assert.Equal(dbg.EditorGroupCount, mine.EditorGroupCount);
        Assert.Equal(
            UiModeCatalog.GetChatPanelExpandedWidthPixels("Debug"),
            UiModeCatalog.GetChatPanelExpandedWidthPixels("MySuperDebug"));
        Assert.False(UiModeCatalog.GetShowTaskBar("Debug"));
        Assert.False(UiModeCatalog.GetShowTaskBar("MySuperDebug"));
        Assert.Equal(UiModeCatalog.GetCapabilities("Debug"), UiModeCatalog.GetCapabilities("MySuperDebug"));
    }

    [Fact]
    public void Show_task_cockpit_defaults_false_for_debug_family_without_toml()
    {
        var dir = Path.Combine(Path.GetTempPath(), "uimodes_" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        File.WriteAllText(
            Path.Combine(dir, "index.toml"),
            """
            schema_version = 1
            modes = [ "Debug" ]
            """);

        UiModeCatalog.Initialize(dir);

        Assert.False(UiModeCatalog.GetShowTaskBar("Debug"));
    }

    [Fact]
    public void Show_task_cockpit_toml_can_override_true()
    {
        var dir = Path.Combine(Path.GetTempPath(), "uimodes_" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        File.WriteAllText(
            Path.Combine(dir, "index.toml"),
            """
            schema_version = 1
            modes = [ "Debug" ]
            """);
        File.WriteAllText(
            Path.Combine(dir, "Debug.toml"),
            """
            show_task_cockpit = true
            """);

        UiModeCatalog.Initialize(dir);

        Assert.True(UiModeCatalog.GetShowTaskBar("Debug"));
    }

    [Fact]
    public void Normalize_preserves_canonical_id_from_index()
    {
        var dir = Path.Combine(Path.GetTempPath(), "uimodes_" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        File.WriteAllText(
            Path.Combine(dir, "index.toml"),
            """
            schema_version = 1
            modes = [ "Focus", "MySuperDebug" ]
            """);
        File.WriteAllText(
            Path.Combine(dir, "MySuperDebug.toml"),
            """
            inherits = "Focus"
            """);

        UiModeCatalog.Initialize(dir);

        Assert.Equal("MySuperDebug", UiModeCatalog.NormalizeUiMode("mysuperdebug"));
    }

    [Fact]
    public void Capabilities_defaults_per_family_and_power_column_span()
    {
        var dir = Path.Combine(Path.GetTempPath(), "uimodes_" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        File.WriteAllText(
            Path.Combine(dir, "index.toml"),
            """
            schema_version = 1
            modes = [ "Balanced", "Debug", "Power", "Focus", "AgentChat" ]
            """);

        UiModeCatalog.Initialize(dir);

        var balanced = UiModeCatalog.GetCapabilities("Balanced");
        Assert.True(balanced.ShowQuickActions);
        Assert.True(balanced.ShowAgentOperationsBlock);
        Assert.False(balanced.ShowHypothesesTab);
        Assert.True(balanced.ShowRiskSummaryCard);

        var debug = UiModeCatalog.GetCapabilities("Debug");
        Assert.False(debug.ShowQuickActions);
        Assert.True(debug.ShowHypothesesTab);
        Assert.True(debug.ShowRiskSummaryCard);

        var power = UiModeCatalog.GetCapabilities("Power");
        Assert.True(power.ShowAgentTrace);
        Assert.True(power.ShowPowerTelemetry);
        Assert.Equal(3, power.PowerTelemetryMainGridColumnSpan);

        Assert.False(UiModeCatalog.GetCapabilities("Focus").ShowRiskSummaryCard);
        Assert.False(UiModeCatalog.GetCapabilities("AgentChat").ShowResultSummaryCard);
    }

    [Fact]
    public void Capabilities_toml_overrides_and_window_title()
    {
        var dir = Path.Combine(Path.GetTempPath(), "uimodes_" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        File.WriteAllText(
            Path.Combine(dir, "index.toml"),
            """
            schema_version = 1
            modes = [ "Debug" ]
            """);
        File.WriteAllText(
            Path.Combine(dir, "Debug.toml"),
            """
            show_hypotheses_tab = false
            window_title = "IDE — Debug (custom)"
            power_telemetry_main_grid_column_span = 4
            """);

        UiModeCatalog.Initialize(dir);

        var caps = UiModeCatalog.GetCapabilities("Debug");
        Assert.False(caps.ShowHypothesesTab);
        Assert.Equal(4, caps.PowerTelemetryMainGridColumnSpan);

        Assert.Equal("IDE — Debug (custom)", UiModeCatalog.GetWindowTitleOverride("Debug"));
    }

    [Fact]
    public void Missing_index_falls_back_to_builtin_registry()
    {
        var dir = Path.Combine(Path.GetTempPath(), "uimodes_empty_" + Guid.NewGuid());
        Directory.CreateDirectory(dir);

        UiModeCatalog.Initialize(dir);

        Assert.Equal(UiModeLayoutRegistry.OrderedModeIds, UiModeCatalog.OrderedModeIds);
        Assert.Equal("Balanced", UiModeCatalog.NormalizeUiMode("nope"));
    }
}

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
    public void Inherits_uses_parent_chat_width_when_child_omits_chat_expanded_width_pixels()
    {
        var dir = Path.Combine(Path.GetTempPath(), "uimodes_" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        File.WriteAllText(
            Path.Combine(dir, "index.toml"),
            """
            schema_version = 1
            modes = [ "Balanced", "DerivedBalanced" ]
            """);
        File.WriteAllText(
            Path.Combine(dir, "Balanced.toml"),
            """
            chat_expanded_width_pixels = 555
            """);
        File.WriteAllText(
            Path.Combine(dir, "DerivedBalanced.toml"),
            """
            inherits = "Balanced"
            """);

        UiModeCatalog.Initialize(dir);

        Assert.Equal(555, UiModeCatalog.GetChatPanelExpandedWidthPixels("Balanced"));
        Assert.Equal(555, UiModeCatalog.GetChatPanelExpandedWidthPixels("DerivedBalanced"));
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
            active_task_strip = true
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
        Assert.True(balanced.QuickActions);
        Assert.True(balanced.AgentOperationsPanel);
        Assert.False(balanced.HypothesesTab);
        Assert.True(balanced.RiskSummaryCard);

        var debug = UiModeCatalog.GetCapabilities("Debug");
        Assert.False(debug.QuickActions);
        Assert.True(debug.HypothesesTab);
        Assert.True(debug.RiskSummaryCard);

        var power = UiModeCatalog.GetCapabilities("Power");
        Assert.True(power.AgentTrace);
        Assert.True(power.AutonomousAgentTelemetry);
        Assert.Equal(3, power.WorkspaceHealthMainColumnSpan);

        Assert.False(UiModeCatalog.GetCapabilities("Focus").RiskSummaryCard);
        Assert.False(UiModeCatalog.GetCapabilities("AgentChat").ResultSummaryCard);
    }

    [Fact]
    public void Flight_inherits_balanced_spec_and_uses_flight_family()
    {
        var dir = Path.Combine(Path.GetTempPath(), "uimodes_" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        File.WriteAllText(
            Path.Combine(dir, "index.toml"),
            """
            schema_version = 1
            modes = [ "Balanced", "Flight" ]
            """);
        File.WriteAllText(
            Path.Combine(dir, "Flight.toml"),
            """
            inherits = "Balanced"
            family = "Flight"
            main_window_title = "CascadeIDE — Flight (test)"
            """);

        UiModeCatalog.Initialize(dir);

        Assert.Equal(UiModeFamily.Flight, UiModeCatalog.GetFamily("Flight"));
        Assert.Equal(UiModeCatalog.GetSpec("Balanced"), UiModeCatalog.GetSpec("Flight"));
        Assert.Equal(UiModeCatalog.GetCapabilities("Balanced"), UiModeCatalog.GetCapabilities("Flight"));
        Assert.Equal("CascadeIDE — Flight (test)", UiModeCatalog.GetWindowTitleOverride("Flight"));
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
            hypotheses_tab = false
            main_window_title = "IDE — Debug (custom)"
            workspace_health_main_column_span = 4
            """);

        UiModeCatalog.Initialize(dir);

        var caps = UiModeCatalog.GetCapabilities("Debug");
        Assert.False(caps.HypothesesTab);
        Assert.Equal(4, caps.WorkspaceHealthMainColumnSpan);

        Assert.Equal("IDE — Debug (custom)", UiModeCatalog.GetWindowTitleOverride("Debug"));
    }

    [Fact]
    public void Editor_family_minimal_chrome_and_layout()
    {
        var dir = Path.Combine(Path.GetTempPath(), "uimodes_" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        File.WriteAllText(
            Path.Combine(dir, "index.toml"),
            """
            schema_version = 1
            modes = [ "Editor" ]
            """);
        File.WriteAllText(
            Path.Combine(dir, "Editor.toml"),
            """
            family = "Editor"
            """);

        UiModeCatalog.Initialize(dir);

        Assert.Equal(UiModeFamily.Editor, UiModeCatalog.GetFamily("Editor"));
        Assert.False(UiModeCatalog.GetShowTaskBar("Editor"));

        var spec = UiModeCatalog.GetSpec("Editor");
        Assert.True(spec.SolutionExplorerVisible);
        Assert.True(spec.ChatPanelExpanded);
        Assert.False(spec.InstrumentationDockVisible);

        var caps = UiModeCatalog.GetCapabilities("Editor");
        Assert.False(caps.WorkspaceHealthStripVisible);
        Assert.False(caps.MainToolbarVisible);
        Assert.False(caps.ProblemsPanelVisible);
    }

    [Fact]
    public void Telemetry_surface_toml_sets_dedicated_page()
    {
        var dir = Path.Combine(Path.GetTempPath(), "uimodes_" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        File.WriteAllText(
            Path.Combine(dir, "index.toml"),
            """
            schema_version = 1
            modes = [ "Balanced" ]
            """);
        File.WriteAllText(
            Path.Combine(dir, "Balanced.toml"),
            """
            workspace_health_surface = "dedicated_page"
            """);

        UiModeCatalog.Initialize(dir);

        var caps = UiModeCatalog.GetCapabilities("Balanced");
        Assert.Equal(WorkspaceHealthUiSurface.DedicatedPage, caps.WorkspaceHealthSurface);
    }

    [Fact]
    public void Empty_override_directory_loads_embedded_uimodes_bundle()
    {
        var dir = Path.Combine(Path.GetTempPath(), "uimodes_empty_" + Guid.NewGuid());
        Directory.CreateDirectory(dir);

        UiModeCatalog.Initialize(dir);

        Assert.Equal(UiModesBundleSource.TomlBundle, UiModeCatalog.ActiveBundleSource);
        Assert.Contains("Flight", UiModeCatalog.OrderedModeIds);
    }

    [Fact]
    public void Invalid_index_toml_falls_back_to_builtin_registry()
    {
        var dir = Path.Combine(Path.GetTempPath(), "uimodes_bad_index_" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "index.toml"), "not valid toml {{{");

        UiModeCatalog.Initialize(dir);

        Assert.Equal(UiModesBundleSource.BuiltinRegistry, UiModeCatalog.ActiveBundleSource);
        Assert.Equal(UiModeLayoutRegistry.OrderedModeIds, UiModeCatalog.OrderedModeIds);
        Assert.Equal("Balanced", UiModeCatalog.NormalizeUiMode("nope"));
    }
}

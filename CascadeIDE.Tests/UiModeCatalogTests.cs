using CascadeIDE.Cockpit;
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
            [bundle]
            schema_version = 1
            modes = [ "Focus", "Balanced", "Power", "AgentChat", "Debug", "MySuperDebug" ]
            """);
        File.WriteAllText(
            Path.Combine(dir, "MySuperDebug.toml"),
            """
            [meta]
            inherits = "Debug"
            """);

        UiModeCatalog.Initialize(dir);

        Assert.Equal(UiModeFamily.Debug, UiModeCatalog.GetFamily("MySuperDebug"));
        var dbg = UiModeCatalog.GetSpec("Debug");
        var mine = UiModeCatalog.GetSpec("MySuperDebug");
        Assert.Equal(dbg.PfdRegionExpanded, mine.PfdRegionExpanded);
        Assert.Equal(dbg.EditorGroupCount, mine.EditorGroupCount);
        Assert.Equal(
            UiModeCatalog.GetMfdRegionExpandedWidthPixels("Debug"),
            UiModeCatalog.GetMfdRegionExpandedWidthPixels("MySuperDebug"));
        Assert.False(UiModeCatalog.GetShowTaskBar("Debug"));
        Assert.False(UiModeCatalog.GetShowTaskBar("MySuperDebug"));
        Assert.Equal(UiModeCatalog.GetCapabilities("Debug"), UiModeCatalog.GetCapabilities("MySuperDebug"));
    }

    [Fact]
    public void Inherits_uses_parent_mfd_width_when_child_omits_mfd_region_expanded_width_pixels()
    {
        var dir = Path.Combine(Path.GetTempPath(), "uimodes_" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        File.WriteAllText(
            Path.Combine(dir, "index.toml"),
            """
            [bundle]
            schema_version = 1
            modes = [ "Balanced", "DerivedBalanced" ]
            """);
        File.WriteAllText(
            Path.Combine(dir, "Balanced.toml"),
            """
            [layout]
            mfd_region_expanded_width_pixels = 555
            """);
        File.WriteAllText(
            Path.Combine(dir, "DerivedBalanced.toml"),
            """
            [meta]
            inherits = "Balanced"
            """);

        UiModeCatalog.Initialize(dir);

        Assert.Equal(555, UiModeCatalog.GetMfdRegionExpandedWidthPixels("Balanced"));
        Assert.Equal(555, UiModeCatalog.GetMfdRegionExpandedWidthPixels("DerivedBalanced"));
    }

    [Fact]
    public void Show_task_cockpit_defaults_false_for_debug_family_without_toml()
    {
        var dir = Path.Combine(Path.GetTempPath(), "uimodes_" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        File.WriteAllText(
            Path.Combine(dir, "index.toml"),
            """
            [bundle]
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
            [bundle]
            schema_version = 1
            modes = [ "Debug" ]
            """);
        File.WriteAllText(
            Path.Combine(dir, "Debug.toml"),
            """
            [layout]
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
            [bundle]
            schema_version = 1
            modes = [ "Focus", "MySuperDebug" ]
            """);
        File.WriteAllText(
            Path.Combine(dir, "MySuperDebug.toml"),
            """
            [meta]
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
            [bundle]
            schema_version = 1
            modes = [ "Balanced", "Debug", "Power", "Focus", "AgentChat", "Flight" ]
            """);

        UiModeCatalog.Initialize(dir);

        var balanced = UiModeCatalog.GetCapabilities("Balanced");
        Assert.True(balanced.QuickActions);
        Assert.True(balanced.AgentOperationsPanel);
        Assert.False(balanced.HypothesesTab);
        Assert.True(balanced.RiskSummaryCard);

        var debug = UiModeCatalog.GetCapabilities("Debug");
        Assert.False(debug.QuickActions);
        Assert.False(debug.HypothesesTab);
        Assert.True(debug.RiskSummaryCard);

        var flight = UiModeCatalog.GetCapabilities("Flight");
        Assert.True(flight.HypothesesTab);

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
            [bundle]
            schema_version = 1
            modes = [ "Balanced", "Flight" ]
            """);
        File.WriteAllText(
            Path.Combine(dir, "Flight.toml"),
            """
            [meta]
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
            [bundle]
            schema_version = 1
            modes = [ "Debug" ]
            """);
        File.WriteAllText(
            Path.Combine(dir, "Debug.toml"),
            """
            [capabilities]
            hypotheses_tab = false
            workspace_health_main_column_span = 4

            [meta]
            main_window_title = "IDE — Debug (custom)"
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
            [bundle]
            schema_version = 1
            modes = [ "Editor" ]
            """);
        File.WriteAllText(
            Path.Combine(dir, "Editor.toml"),
            """
            [meta]
            inherits = "Flight"
            family = "Editor"

            [layout]
            instrumentation_dock_visible = false
            active_task_strip = false

            [capabilities]
            quick_actions = false
            agent_operations_panel = false
            agent_trace = false
            autonomous_agent_telemetry = false
            workspace_health_on_terminal_tab = false
            workspace_health_main_column_span = 5
            instrumentation_tabs = false
            hypotheses_tab = false
            risk_summary_card = false
            result_summary_card = false
            workspace_health_strip = false
            workspace_health_surface = "bottom_strip"
            problems_panel = false
            eicas_alerts_bar = false
            """);

        UiModeCatalog.Initialize(dir);

        Assert.Equal(UiModeFamily.Editor, UiModeCatalog.GetFamily("Editor"));
        Assert.False(UiModeCatalog.GetShowTaskBar("Editor"));

        var spec = UiModeCatalog.GetSpec("Editor");
        Assert.True(spec.PfdRegionExpanded);
        Assert.True(spec.MfdRegionExpanded);
        Assert.False(spec.InstrumentationDockVisible);

        var caps = UiModeCatalog.GetCapabilities("Editor");
        Assert.False(caps.WorkspaceHealthStripVisible);
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
            [bundle]
            schema_version = 1
            modes = [ "Balanced" ]
            """);
        File.WriteAllText(
            Path.Combine(dir, "Balanced.toml"),
            """
            [capabilities]
            workspace_health_surface = "dedicated_page"
            """);

        UiModeCatalog.Initialize(dir);

        var caps = UiModeCatalog.GetCapabilities("Balanced");
        Assert.Equal(WorkspaceHealthUiSurface.DedicatedPage, caps.WorkspaceHealthSurface);
        Assert.Equal(ContentRepresentation.Page, caps.WorkspaceHealthContentRepresentation);
    }

    [Fact]
    public void Workspace_health_ui_surface_maps_to_content_representation_axis()
    {
        Assert.Equal(ContentRepresentation.Strip, WorkspaceHealthUiSurface.BottomStrip.ToContentRepresentation());
        Assert.Equal(ContentRepresentation.Page, WorkspaceHealthUiSurface.DedicatedPage.ToContentRepresentation());
    }

    [Fact]
    public void Empty_override_directory_loads_embedded_uimodes_bundle()
    {
        var dir = Path.Combine(Path.GetTempPath(), "uimodes_empty_" + Guid.NewGuid());
        Directory.CreateDirectory(dir);

        UiModeCatalog.Initialize(dir);

        Assert.Equal(UiModesBundleSource.TomlBundle, UiModeCatalog.ActiveBundleSource);
        Assert.Equal(new[] { "Flight" }, UiModeCatalog.OrderedModeIds.ToArray());
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
        Assert.Equal("Flight", UiModeCatalog.NormalizeUiMode("nope"));
    }
}

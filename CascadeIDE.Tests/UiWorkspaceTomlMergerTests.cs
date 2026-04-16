using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class UiWorkspaceTomlMergerTests
{
    [Fact]
    public void Merge_null_null_yields_null()
    {
        Assert.Null(UiWorkspaceTomlMerger.Merge(null, null));
    }

    [Fact]
    public void Merge_lower_only_round_trips_scalars()
    {
        var lower = new UiWorkspaceToml { PfdRegionDefaultWidthPixels = 300, BottomPanelMinRowPixels = 80 };
        var m = UiWorkspaceTomlMerger.Merge(lower, null);
        Assert.NotNull(m);
        Assert.Equal(300, m!.PfdRegionDefaultWidthPixels);
        Assert.Equal(80, m.BottomPanelMinRowPixels);
    }

    [Fact]
    public void Merge_higher_overrides_scalars()
    {
        var lower = new UiWorkspaceToml { PfdRegionDefaultWidthPixels = 300 };
        var higher = new UiWorkspaceToml { PfdRegionDefaultWidthPixels = 400 };
        var m = UiWorkspaceTomlMerger.Merge(lower, higher);
        Assert.Equal(400, m!.PfdRegionDefaultWidthPixels);
    }

    [Fact]
    public void Merge_higher_fills_missing_scalars_from_lower()
    {
        var lower = new UiWorkspaceToml { PfdRegionDefaultWidthPixels = 300, BottomPanelMinRowPixels = 90 };
        var higher = new UiWorkspaceToml { BottomPanelMinRowPixels = 100 };
        var m = UiWorkspaceTomlMerger.Merge(lower, higher);
        Assert.Equal(300, m!.PfdRegionDefaultWidthPixels);
        Assert.Equal(100, m.BottomPanelMinRowPixels);
    }

    [Fact]
    public void Merge_attention_zone_panels_union_higher_wins_key()
    {
        var lower = new UiWorkspaceToml
        {
            AttentionZonePanels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["solution_explorer"] = "pfd",
                ["chat_panel"] = "mfd"
            }
        };
        var higher = new UiWorkspaceToml
        {
            AttentionZonePanels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["solution_explorer"] = "mfd",
                ["git"] = "pfd"
            }
        };
        var m = UiWorkspaceTomlMerger.Merge(lower, higher);
        Assert.NotNull(m!.AttentionZonePanels);
        Assert.Equal("mfd", m.AttentionZonePanels!["solution_explorer"]);
        Assert.Equal("mfd", m.AttentionZonePanels["chat_panel"]);
        Assert.Equal("pfd", m.AttentionZonePanels["git"]);
    }

    [Fact]
    public void Merge_instrument_placement_rules_higher_wins_same_surface_slot()
    {
        var lower = new UiWorkspaceToml
        {
            InstrumentPlacementRules =
            [
                new InstrumentPlacementRuleSettings
                {
                    SurfaceId = "main_window_docked_grid",
                    SlotId = "pfd",
                    InstrumentId = "solution_explorer_tree"
                },
                new InstrumentPlacementRuleSettings
                {
                    SurfaceId = "main_window_docked_grid",
                    SlotId = "mfd",
                    InstrumentId = "workspace_navigation_map"
                }
            ]
        };
        var higher = new UiWorkspaceToml
        {
            InstrumentPlacementRules =
            [
                new InstrumentPlacementRuleSettings
                {
                    SurfaceId = "main_window_docked_grid",
                    SlotId = "pfd",
                    InstrumentId = "workspace_navigation_map"
                }
            ]
        };

        var merged = UiWorkspaceTomlMerger.Merge(lower, higher);
        Assert.NotNull(merged?.InstrumentPlacementRules);
        Assert.Collection(
            merged!.InstrumentPlacementRules!,
            first =>
            {
                Assert.Equal("main_window_docked_grid", first.SurfaceId);
                Assert.Equal("pfd", first.SlotId);
                Assert.Equal("workspace_navigation_map", first.InstrumentId);
            },
            second =>
            {
                Assert.Equal("main_window_docked_grid", second.SurfaceId);
                Assert.Equal("mfd", second.SlotId);
                Assert.Equal("workspace_navigation_map", second.InstrumentId);
            });
    }
}

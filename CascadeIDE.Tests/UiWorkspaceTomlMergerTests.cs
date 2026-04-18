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
        var lower = new UiWorkspaceToml
        {
            Chrome = new UiWorkspaceChromeToml
            {
                PfdRegionDefaultWidthPixels = 300,
                BottomPanelMinRowPixels = 80
            }
        };
        var m = UiWorkspaceTomlMerger.Merge(lower, null);
        Assert.NotNull(m);
        Assert.Equal(300, m!.Chrome!.PfdRegionDefaultWidthPixels);
        Assert.Equal(80, m.Chrome.BottomPanelMinRowPixels);
    }

    [Fact]
    public void Merge_higher_overrides_scalars()
    {
        var lower = new UiWorkspaceToml
        {
            Chrome = new UiWorkspaceChromeToml { PfdRegionDefaultWidthPixels = 300 }
        };
        var higher = new UiWorkspaceToml
        {
            Chrome = new UiWorkspaceChromeToml { PfdRegionDefaultWidthPixels = 400 }
        };
        var m = UiWorkspaceTomlMerger.Merge(lower, higher);
        Assert.Equal(400, m!.Chrome!.PfdRegionDefaultWidthPixels);
    }

    [Fact]
    public void Merge_higher_fills_missing_scalars_from_lower()
    {
        var lower = new UiWorkspaceToml
        {
            Chrome = new UiWorkspaceChromeToml
            {
                PfdRegionDefaultWidthPixels = 300,
                BottomPanelMinRowPixels = 90
            }
        };
        var higher = new UiWorkspaceToml
        {
            Chrome = new UiWorkspaceChromeToml { BottomPanelMinRowPixels = 100 }
        };
        var m = UiWorkspaceTomlMerger.Merge(lower, higher);
        Assert.Equal(300, m!.Chrome!.PfdRegionDefaultWidthPixels);
        Assert.Equal(100, m.Chrome.BottomPanelMinRowPixels);
    }

    [Fact]
    public void Merge_attention_routing_union_higher_wins_key()
    {
        var lower = new UiWorkspaceToml
        {
            Routing = new UiWorkspaceRoutingToml
            {
                Attention = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["solution_explorer"] = "pfd",
                    ["chat"] = "mfd"
                }
            }
        };
        var higher = new UiWorkspaceToml
        {
            Routing = new UiWorkspaceRoutingToml
            {
                Attention = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["solution_explorer"] = "mfd",
                    ["git"] = "pfd"
                }
            }
        };
        var m = UiWorkspaceTomlMerger.Merge(lower, higher);
        Assert.NotNull(m!.Routing?.Attention);
        Assert.Equal("mfd", m.Routing.Attention!["solution_explorer"]);
        Assert.Equal("mfd", m.Routing.Attention["chat"]);
        Assert.Equal("pfd", m.Routing.Attention["git"]);
    }

    [Fact]
    public void Merge_instrument_routing_higher_wins_key_union()
    {
        var lower = new UiWorkspaceToml
        {
            Routing = new UiWorkspaceRoutingToml
            {
                Instruments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [InstrumentRoutingSlotKeys.PfdPrimary] = "solution_explorer",
                    [InstrumentRoutingSlotKeys.MfdPrimary] = "workspace_map"
                }
            }
        };
        var higher = new UiWorkspaceToml
        {
            Routing = new UiWorkspaceRoutingToml
            {
                Instruments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [InstrumentRoutingSlotKeys.PfdPrimary] = "workspace_map"
                }
            }
        };

        var merged = UiWorkspaceTomlMerger.Merge(lower, higher);
        Assert.NotNull(merged?.Routing?.Instruments);
        Assert.Equal("workspace_map", merged!.Routing!.Instruments![InstrumentRoutingSlotKeys.PfdPrimary]);
        Assert.Equal("workspace_map", merged.Routing.Instruments[InstrumentRoutingSlotKeys.MfdPrimary]);
    }
}

using CascadeIDE.Features.UiChrome;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class UiWorkspaceTomlMergerTests
{
    [Fact]
    public void Merge_null_null_yields_null()
    {
        Assert.Null(RepositoryWorkspaceTomlMerger.Merge(null, null));
    }

    [Fact]
    public void Merge_lower_only_round_trips_scalars()
    {
        var lower = new RepositoryWorkspaceToml
        {
            Chrome = new UiWorkspaceChromeToml
            {
                PfdRegionDefaultWidthPixels = 300,
                BottomPanelMinRowPixels = 80
            }
        };
        var m = RepositoryWorkspaceTomlMerger.Merge(lower, null);
        Assert.NotNull(m);
        Assert.Equal(300, m!.Chrome!.PfdRegionDefaultWidthPixels);
        Assert.Equal(80, m.Chrome.BottomPanelMinRowPixels);
    }

    [Fact]
    public void Merge_higher_overrides_scalars()
    {
        var lower = new RepositoryWorkspaceToml
        {
            Chrome = new UiWorkspaceChromeToml { PfdRegionDefaultWidthPixels = 300 }
        };
        var higher = new RepositoryWorkspaceToml
        {
            Chrome = new UiWorkspaceChromeToml { PfdRegionDefaultWidthPixels = 400 }
        };
        var m = RepositoryWorkspaceTomlMerger.Merge(lower, higher);
        Assert.Equal(400, m!.Chrome!.PfdRegionDefaultWidthPixels);
    }

    [Fact]
    public void Merge_higher_fills_missing_scalars_from_lower()
    {
        var lower = new RepositoryWorkspaceToml
        {
            Chrome = new UiWorkspaceChromeToml
            {
                PfdRegionDefaultWidthPixels = 300,
                BottomPanelMinRowPixels = 90
            }
        };
        var higher = new RepositoryWorkspaceToml
        {
            Chrome = new UiWorkspaceChromeToml { BottomPanelMinRowPixels = 100 }
        };
        var m = RepositoryWorkspaceTomlMerger.Merge(lower, higher);
        Assert.Equal(300, m!.Chrome!.PfdRegionDefaultWidthPixels);
        Assert.Equal(100, m.Chrome.BottomPanelMinRowPixels);
    }

    [Fact]
    public void Merge_attention_routing_union_higher_wins_key()
    {
        var lower = new RepositoryWorkspaceToml
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
        var higher = new RepositoryWorkspaceToml
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
        var m = RepositoryWorkspaceTomlMerger.Merge(lower, higher);
        Assert.NotNull(m!.Routing?.Attention);
        Assert.Equal("mfd", m.Routing.Attention!["solution_explorer"]);
        Assert.Equal("mfd", m.Routing.Attention["chat"]);
        Assert.Equal("pfd", m.Routing.Attention["git"]);
    }

    [Fact]
    public void Merge_instrument_routing_higher_wins_key_union()
    {
        var lower = new RepositoryWorkspaceToml
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
        var higher = new RepositoryWorkspaceToml
        {
            Routing = new UiWorkspaceRoutingToml
            {
                Instruments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [InstrumentRoutingSlotKeys.PfdPrimary] = "workspace_map"
                }
            }
        };

        var merged = RepositoryWorkspaceTomlMerger.Merge(lower, higher);
        Assert.NotNull(merged?.Routing?.Instruments);
        Assert.Equal("workspace_map", merged!.Routing!.Instruments![InstrumentRoutingSlotKeys.PfdPrimary]);
        Assert.Equal("workspace_map", merged.Routing.Instruments[InstrumentRoutingSlotKeys.MfdPrimary]);
    }

    [Fact]
    public void Merge_loc_limits_higher_overrides_partial_scalars()
    {
        var lower = new RepositoryWorkspaceToml
        {
            LocLimits = new UiWorkspaceLocLimitsToml { MediumMin = 300, HighMin = 800 }
        };
        var higher = new RepositoryWorkspaceToml
        {
            LocLimits = new UiWorkspaceLocLimitsToml { HighMin = 900 }
        };
        var m = RepositoryWorkspaceTomlMerger.Merge(lower, higher);
        Assert.NotNull(m?.LocLimits);
        Assert.Equal(300, m!.LocLimits!.MediumMin);
        Assert.Equal(900, m.LocLimits.HighMin);
    }
}

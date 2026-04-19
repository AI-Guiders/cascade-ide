using CascadeIDE.Features.UiChrome;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class AttentionZonePanelRuntimeTests : IDisposable
{
    public AttentionZonePanelRuntimeTests() =>
        AttentionZonePanelRuntime.ResetToCodeDefaults();

    public void Dispose() =>
        AttentionZonePanelRuntime.ResetToCodeDefaults();

    [Fact]
    public void Defaults_match_adr_semantics()
    {
        AttentionZonePanelRuntime.ResetToCodeDefaults();
        Assert.True(AttentionZonePanelRuntime.TryGetZone(AttentionPanelIds.SolutionExplorer, out var se));
        Assert.Equal(AttentionZone.Pfd, se);
        Assert.True(AttentionZonePanelRuntime.TryGetZone(AttentionPanelIds.ChatPanel, out var ch));
        Assert.Equal(AttentionZone.Mfd, ch);
        Assert.True(AttentionZonePanelRuntime.TryGetZone(AttentionPanelIds.Terminal, out var term));
        Assert.Equal(AttentionZone.Mfd, term);
        Assert.True(AttentionZonePanelRuntime.TryGetZone(AttentionPanelIds.Editor, out var ed));
        Assert.Equal(AttentionZone.Forward, ed);
        Assert.True(AttentionZonePanelRuntime.TryGetZone(AttentionPanelIds.EditorHud, out var hud));
        Assert.Equal(AttentionZone.Hud, hud);
    }

    [Fact]
    public void ApplyWorkspaceToml_overrides_single_panel()
    {
        AttentionZonePanelRuntime.ApplyWorkspaceToml(new UiWorkspaceToml
        {
            Routing = new UiWorkspaceRoutingToml
            {
                Attention = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [AttentionRoutingIntentIds.SolutionExplorer] = AttentionZoneIds.Mfd,
                }
            }
        });

        Assert.True(AttentionZonePanelRuntime.TryGetZone(AttentionPanelIds.SolutionExplorer, out var z));
        Assert.Equal(AttentionZone.Mfd, z);
        Assert.True(AttentionZonePanelRuntime.TryGetZone(AttentionPanelIds.ChatPanel, out var chat));
        Assert.Equal(AttentionZone.Mfd, chat);
    }

    [Fact]
    public void Toml_deserializes_attention_routing_table()
    {
        const string toml = """
            [chrome]
            pfd_region_default_width_pixels = 220

            [routing]
            attention = { solution_explorer = "pfd", git = "mfd", terminal = "pfd" }
            """;

        var w = CascadeTomlSerializer.Deserialize<UiWorkspaceToml>(toml);
        Assert.NotNull(w);
        Assert.NotNull(w!.Routing?.Attention);
        Assert.Equal("pfd", w.Routing.Attention!["solution_explorer"]);
        AttentionZonePanelRuntime.ApplyWorkspaceToml(w);
        Assert.True(AttentionZonePanelRuntime.TryGetZone(AttentionPanelIds.Git, out var g));
        Assert.Equal(AttentionZone.Mfd, g);
        Assert.True(AttentionZonePanelRuntime.TryGetZone(AttentionPanelIds.Terminal, out var term));
        Assert.Equal(AttentionZone.Pfd, term);
    }

    [Fact]
    public void ApplyWorkspaceToml_rejects_non_spatial_zone_for_terminal_intent()
    {
        AttentionZonePanelRuntime.ApplyWorkspaceToml(new UiWorkspaceToml
        {
            Routing = new UiWorkspaceRoutingToml
            {
                Attention = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [AttentionRoutingIntentIds.Terminal] = AttentionZoneIds.Hud,
                }
            }
        });

        Assert.True(AttentionZonePanelRuntime.TryGetZone(AttentionPanelIds.Terminal, out var term));
        Assert.Equal(AttentionZone.Mfd, term);
    }

    [Fact]
    public void ApplyWorkspaceToml_does_not_allow_editor_hud_override_via_attention_routing()
    {
        AttentionZonePanelRuntime.ApplyWorkspaceToml(new UiWorkspaceToml
        {
            Routing = new UiWorkspaceRoutingToml
            {
                Attention = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["editor_hud"] = AttentionZoneIds.Mfd,
                }
            }
        });

        Assert.True(AttentionZonePanelRuntime.TryGetZone(AttentionPanelIds.EditorHud, out var hud));
        Assert.Equal(AttentionZone.Hud, hud);
    }
}

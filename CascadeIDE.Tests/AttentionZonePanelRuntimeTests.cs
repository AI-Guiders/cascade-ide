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
        Assert.True(AttentionZonePanelRuntime.TryGetZone(AttentionPanelIds.Editor, out var ed));
        Assert.Equal(AttentionZone.Forward, ed);
    }

    [Fact]
    public void ApplyWorkspaceToml_overrides_single_panel()
    {
        AttentionZonePanelRuntime.ApplyWorkspaceToml(new UiWorkspaceToml
        {
            AttentionZonePanels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [AttentionPanelIds.SolutionExplorer] = AttentionZoneIds.Mfd,
            },
        });

        Assert.True(AttentionZonePanelRuntime.TryGetZone(AttentionPanelIds.SolutionExplorer, out var z));
        Assert.Equal(AttentionZone.Mfd, z);
        Assert.True(AttentionZonePanelRuntime.TryGetZone(AttentionPanelIds.ChatPanel, out var chat));
        Assert.Equal(AttentionZone.Mfd, chat);
    }

    [Fact]
    public void Toml_deserializes_attention_zone_panels_table()
    {
        const string toml = """
            solution_explorer_default_width_pixels = 220

            [attention_zone_panels]
            solution_explorer = "pfd"
            git = "mfd"
            """;

        var w = CascadeTomlSerializer.Deserialize<UiWorkspaceToml>(toml);
        Assert.NotNull(w);
        Assert.NotNull(w!.AttentionZonePanels);
        Assert.Equal("pfd", w.AttentionZonePanels!["solution_explorer"]);
        AttentionZonePanelRuntime.ApplyWorkspaceToml(w);
        Assert.True(AttentionZonePanelRuntime.TryGetZone(AttentionPanelIds.Git, out var g));
        Assert.Equal(AttentionZone.Mfd, g);
    }
}

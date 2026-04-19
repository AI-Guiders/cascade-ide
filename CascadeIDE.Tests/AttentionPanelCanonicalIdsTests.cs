using CascadeIDE.Contracts.Experimental;
using CascadeIDE.Features.UiChrome;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class AttentionPanelCanonicalIdsTests
{
    [Fact]
    public void AttentionPanelIds_strings_match_contracts()
    {
        Assert.Equal(AttentionPanelCanonicalIds.SolutionExplorer, AttentionPanelIds.SolutionExplorer);
        Assert.Equal(AttentionPanelCanonicalIds.ChatPanel, AttentionPanelIds.ChatPanel);
        Assert.Equal(AttentionPanelCanonicalIds.Git, AttentionPanelIds.Git);
        Assert.Equal(AttentionPanelCanonicalIds.Terminal, AttentionPanelIds.Terminal);
        Assert.Equal(AttentionPanelCanonicalIds.Editor, AttentionPanelIds.Editor);
        Assert.Equal(AttentionPanelCanonicalIds.EditorHud, AttentionPanelIds.EditorHud);
        Assert.Equal(6, AttentionPanelCanonicalIds.All.Length);
    }

    [Theory]
    [InlineData("solution_explorer", true)]
    [InlineData("editor_hud", true)]
    [InlineData("unknown", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsKnownPanelId(string? id, bool expected) =>
        Assert.Equal(expected, AttentionPanelCanonicalIds.IsKnownPanelId(id));
}

using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SemanticMapSettingsTests
{
    [Theory]
    [InlineData("file", SemanticMapLevelKind.File)]
    [InlineData("FILE", SemanticMapLevelKind.File)]
    [InlineData("controlFlow", SemanticMapLevelKind.ControlFlow)]
    [InlineData("CONTROLFLOW", SemanticMapLevelKind.ControlFlow)]
    [InlineData("unknown", SemanticMapLevelKind.File)]
    public void NormalizeLevel_ReturnsKnownValue(string input, string expected)
    {
        var actual = SemanticMapSettings.NormalizeDepth(input);
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Регресс: обновление Semantic Map по курсору в CF должно опираться на <c>[semantic_map].depth</c>,
    /// как и основной refresh — иначе <c>SemanticMapLevel</c> на VM и Depth расходятся, карта не следует за методом.
    /// </summary>
    [Theory]
    [InlineData("controlFlow", true)]
    [InlineData("CONTROLFLOW", true)]
    [InlineData("file", false)]
    [InlineData("unknown", false)]
    public void IsControlFlowDepth_MatchesNormalizeDepth(string depth, bool expectedControlFlow)
    {
        var map = new SemanticMapSettings { Depth = depth };
        Assert.Equal(expectedControlFlow, map.IsControlFlowDepth);
        Assert.Equal(
            SemanticMapSettings.NormalizeDepth(depth) == SemanticMapLevelKind.ControlFlow,
            map.IsControlFlowDepth);
    }

    /// <summary>
    /// Параметр <c>[semantic_map].view</c> в <c>RunWorkspaceNavigationMapRefreshAsync</c> даёт
    /// <c>wantList</c> / <c>wantGraph</c> — при расхождении с привязкой ComboBox к VM снова получится «тихий» баг UI.
    /// </summary>
    [Theory]
    [InlineData("list", true, false)]
    [InlineData("LIST", true, false)]
    [InlineData("graph", false, true)]
    [InlineData("GRAPH", false, true)]
    [InlineData("both", true, true)]
    [InlineData("BOTH", true, true)]
    [InlineData("nonsense", true, false)]
    [InlineData("", true, false)]
    public void NormalizeView_MatchesWorkspaceRefreshWantListWantGraph(
        string rawView,
        bool expectedWantList,
        bool expectedWantGraph)
    {
        var map = new SemanticMapSettings { View = rawView };
        Assert.Equal(expectedWantList, map.WantsSemanticMapList);
        Assert.Equal(expectedWantGraph, map.WantsSemanticMapGraph);
        var normalized = SemanticMapSettings.NormalizeView(rawView);
        Assert.Equal(expectedWantList, normalized is "list" or "both");
        Assert.Equal(expectedWantGraph, normalized is "graph" or "both");
    }

    [Theory]
    [InlineData("glance", SemanticMapDetailLevel.Glance)]
    [InlineData("GLANCE", SemanticMapDetailLevel.Glance)]
    [InlineData("inspect", SemanticMapDetailLevel.Inspect)]
    [InlineData("normal", SemanticMapDetailLevel.Normal)]
    [InlineData("", SemanticMapDetailLevel.Normal)]
    [InlineData("unknown", SemanticMapDetailLevel.Normal)]
    public void NormalizeDetailLevel_ReturnsKnownValue(string? raw, SemanticMapDetailLevel expected)
    {
        Assert.Equal(expected, SemanticMapSettings.NormalizeDetailLevel(raw));
        var map = new SemanticMapSettings { DetailLevel = raw ?? "" };
        Assert.Equal(expected, map.NormalizedDetailLevel);
    }
}

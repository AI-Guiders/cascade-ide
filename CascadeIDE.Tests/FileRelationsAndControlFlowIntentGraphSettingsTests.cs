using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class FileRelationsAndControlFlowIntentGraphSettingsTests
{
    [Theory]
    [InlineData("file", CodeNavigationMapLevelKind.File)]
    [InlineData("FILE", CodeNavigationMapLevelKind.File)]
    [InlineData("controlFlow", CodeNavigationMapLevelKind.ControlFlow)]
    [InlineData("CONTROLFLOW", CodeNavigationMapLevelKind.ControlFlow)]
    [InlineData("unknown", CodeNavigationMapLevelKind.File)]
    public void NormalizeLevel_ReturnsKnownValue(string input, string expected)
    {
        var actual = FileRelationsAndControlFlowIntentGraphSettings.NormalizeDepth(input);
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Регресс: обновление Semantic Map по курсору в CF должно опираться на
    /// <c>[code_navigation.file_relations_and_control_flow_intent_graph].depth</c>, как и основной refresh.
    /// </summary>
    [Theory]
    [InlineData("controlFlow", true)]
    [InlineData("CONTROLFLOW", true)]
    [InlineData("file", false)]
    [InlineData("unknown", false)]
    public void IsControlFlowDepth_MatchesNormalizeDepth(string depth, bool expectedControlFlow)
    {
        var map = new FileRelationsAndControlFlowIntentGraphSettings { Depth = depth };
        Assert.Equal(expectedControlFlow, map.IsControlFlowDepth);
        Assert.Equal(
            FileRelationsAndControlFlowIntentGraphSettings.NormalizeDepth(depth) == CodeNavigationMapLevelKind.ControlFlow,
            map.IsControlFlowDepth);
    }

    /// <summary>
    /// <c>view</c> в <c>RunWorkspaceNavigationMapRefreshAsync</c> даёт <c>wantList</c> / <c>wantGraph</c>.
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
        var map = new FileRelationsAndControlFlowIntentGraphSettings { View = rawView };
        Assert.Equal(expectedWantList, map.WantsWorkspaceNavigationMapList);
        Assert.Equal(expectedWantGraph, map.WantsWorkspaceNavigationMapGraph);
        var normalized = FileRelationsAndControlFlowIntentGraphSettings.NormalizeView(rawView);
        Assert.Equal(expectedWantList, normalized is "list" or "both");
        Assert.Equal(expectedWantGraph, normalized is "graph" or "both");
    }

    [Theory]
    [InlineData("glance", CodeNavigationMapDetailLevel.Glance)]
    [InlineData("GLANCE", CodeNavigationMapDetailLevel.Glance)]
    [InlineData("inspect", CodeNavigationMapDetailLevel.Inspect)]
    [InlineData("normal", CodeNavigationMapDetailLevel.Normal)]
    [InlineData("", CodeNavigationMapDetailLevel.Normal)]
    [InlineData("unknown", CodeNavigationMapDetailLevel.Normal)]
    public void NormalizeDetailLevel_ReturnsKnownValue(string? raw, CodeNavigationMapDetailLevel expected)
    {
        Assert.Equal(expected, FileRelationsAndControlFlowIntentGraphSettings.NormalizeDetailLevel(raw));
        var map = new FileRelationsAndControlFlowIntentGraphSettings { DetailLevel = raw ?? "" };
        Assert.Equal(expected, map.NormalizedDetailLevel);
    }

    [Theory]
    [InlineData(-5, 0.15)]
    [InlineData(0, 0.15)]
    [InlineData(0.2, 0.2)]
    [InlineData(2, 2)]
    [InlineData(42, 10)]
    [InlineData(double.PositiveInfinity, FileRelationsAndControlFlowIntentGraphSettings.DefaultCaretIdleRefreshSeconds)]
    [InlineData(double.NaN, FileRelationsAndControlFlowIntentGraphSettings.DefaultCaretIdleRefreshSeconds)]
    public void NormalizeCaretIdleRefreshSeconds_ClampsAndFallsBack(double raw, double expected)
    {
        Assert.Equal(expected, FileRelationsAndControlFlowIntentGraphSettings.NormalizeCaretIdleRefreshSeconds(raw), 6);
        var map = new FileRelationsAndControlFlowIntentGraphSettings { CaretIdleRefreshSeconds = raw };
        Assert.Equal(expected, map.NormalizedCaretIdleRefreshSeconds, 6);
    }

    [Fact]
    public void SuspendEditorSideEffectsWhileSelecting_DefaultsToFalse()
    {
        var map = new FileRelationsAndControlFlowIntentGraphSettings();
        Assert.False(map.SuspendEditorSideEffectsWhileSelecting);
    }
}

using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CodeNavigationMapSettingsTests
{
    [Theory]
    [InlineData("file", CodeNavigationMapLevelKind.File)]
    [InlineData("FILE", CodeNavigationMapLevelKind.File)]
    [InlineData("controlFlow", CodeNavigationMapLevelKind.ControlFlow)]
    [InlineData("CONTROLFLOW", CodeNavigationMapLevelKind.ControlFlow)]
    [InlineData("unknown", CodeNavigationMapLevelKind.File)]
    public void NormalizeLevel_ReturnsKnownValue(string input, string expected)
    {
        var actual = CodeNavigationMapSettings.NormalizeDepth(input);
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Регресс: обновление карты намерений по курсору в CF должно опираться на <c>[code_navigation_map].depth</c>,
    /// как и основной refresh — иначе <c>CodeNavigationMapLevel</c> на VM и Depth расходятся, карта не следует за методом.
    /// </summary>
    [Theory]
    [InlineData("controlFlow", true)]
    [InlineData("CONTROLFLOW", true)]
    [InlineData("file", false)]
    [InlineData("unknown", false)]
    public void IsControlFlowDepth_MatchesNormalizeDepth(string depth, bool expectedControlFlow)
    {
        var map = new CodeNavigationMapSettings { Depth = depth };
        Assert.Equal(expectedControlFlow, map.IsControlFlowDepth);
        Assert.Equal(
            CodeNavigationMapSettings.NormalizeDepth(depth) == CodeNavigationMapLevelKind.ControlFlow,
            map.IsControlFlowDepth);
    }

    /// <summary>
    /// Параметр <c>[code_navigation_map].view</c> в <c>RunWorkspaceNavigationMapRefreshAsync</c> даёт
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
        var map = new CodeNavigationMapSettings { View = rawView };
        Assert.Equal(expectedWantList, map.WantsCodeNavigationMapList);
        Assert.Equal(expectedWantGraph, map.WantsCodeNavigationMapGraph);
        var normalized = CodeNavigationMapSettings.NormalizeView(rawView);
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
        Assert.Equal(expected, CodeNavigationMapSettings.NormalizeDetailLevel(raw));
        var map = new CodeNavigationMapSettings { DetailLevel = raw ?? "" };
        Assert.Equal(expected, map.NormalizedDetailLevel);
    }

    [Theory]
    [InlineData("radial", CodeNavigationMapRelatedGraphLayoutKind.Radial)]
    [InlineData("top_down", CodeNavigationMapRelatedGraphLayoutKind.TopDown)]
    [InlineData("bottom_up", CodeNavigationMapRelatedGraphLayoutKind.BottomUp)]
    [InlineData("junk", CodeNavigationMapRelatedGraphLayoutKind.Radial)]
    public void NormalizeRelatedGraphLayout(string raw, string expected)
    {
        Assert.Equal(expected, CodeNavigationMapRelatedGraphLayoutKind.Normalize(raw));
        var map = new CodeNavigationMapSettings { RelatedGraphLayout = raw };
        Assert.Equal(expected, map.NormalizedRelatedGraphLayout);
    }

    [Theory]
    [InlineData("auto", CodeNavigationMapControlFlowMainAxisKind.Auto)]
    [InlineData("", CodeNavigationMapControlFlowMainAxisKind.Auto)]
    [InlineData("junk", CodeNavigationMapControlFlowMainAxisKind.Auto)]
    [InlineData("vertical", CodeNavigationMapControlFlowMainAxisKind.Vertical)]
    [InlineData("HORIZONTAL", CodeNavigationMapControlFlowMainAxisKind.Horizontal)]
    public void NormalizeControlFlowMainAxis(string raw, string expected)
    {
        Assert.Equal(expected, CodeNavigationMapControlFlowMainAxisKind.Normalize(raw));
        var map = new CodeNavigationMapSettings { ControlFlowMainAxis = raw };
        Assert.Equal(expected, map.NormalizedControlFlowMainAxis);
    }

    [Theory]
    [InlineData("", CodeNavigationMapControlFlowGrainKind.Intent)]
    [InlineData("intent", CodeNavigationMapControlFlowGrainKind.Intent)]
    [InlineData("INTENT", CodeNavigationMapControlFlowGrainKind.Intent)]
    [InlineData("micro_cfg", CodeNavigationMapControlFlowGrainKind.Intent)]
    [InlineData("detailed", CodeNavigationMapControlFlowGrainKind.Detailed)]
    [InlineData("DETAILED", CodeNavigationMapControlFlowGrainKind.Detailed)]
    public void NormalizeControlFlowGrain(string raw, string expected)
    {
        Assert.Equal(expected, CodeNavigationMapControlFlowGrainKind.Normalize(raw));
        var map = new CodeNavigationMapSettings { ControlFlowGrain = raw };
        Assert.Equal(expected, map.NormalizedControlFlowGrain);
        Assert.Equal(
            expected == CodeNavigationMapControlFlowGrainKind.Detailed,
            CodeNavigationMapControlFlowGrainKind.IsDetailed(expected));
        Assert.Equal(
            expected == CodeNavigationMapControlFlowGrainKind.Intent,
            CodeNavigationMapControlFlowGrainKind.IsIntent(expected));
    }

    [Theory]
    [InlineData("plus_minus", "+", "-")]
    [InlineData("true_false", "true", "false")]
    [InlineData("one_zero", "1", "0")]
    public void ResolveConditionBranchLabels_FromPreset(string preset, string positive, string negative)
    {
        var map = new CodeNavigationMapSettings { ConditionBranchLabelPreset = preset };
        var pair = CodeNavigationMapConditionBranchLabels.Resolve(map);
        Assert.Equal(positive, pair.Positive);
        Assert.Equal(negative, pair.Negative);
    }

    [Fact]
    public void ResolveConditionBranchLabels_CustomOverridesPreset()
    {
        var map = new CodeNavigationMapSettings
        {
            ConditionBranchLabelPreset = CodeNavigationMapConditionBranchLabels.PresetCustom,
            ConditionBranchPositive = "Y",
            ConditionBranchNegative = "N"
        };
        var pair = map.ResolvedConditionBranchLabels();
        Assert.Equal("Y", pair.Positive);
        Assert.Equal("N", pair.Negative);
    }
}

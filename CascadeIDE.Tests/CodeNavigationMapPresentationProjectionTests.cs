using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CodeNavigationMapPresentationProjectionTests
{
    [Theory]
    [InlineData("list", true, false)]
    [InlineData("graph", false, true)]
    [InlineData("both", true, true)]
    public void Presentation_list_graph_match_settings_normalize_view(string view, bool expectList, bool expectGraph)
    {
        Assert.Equal(expectList, CodeNavigationMapPresentationProjection.ShowCodeNavigationMapList(view));
        Assert.Equal(expectGraph, CodeNavigationMapPresentationProjection.ShowCodeNavigationMapGraph(view, CodeNavigationMapLevelKind.File));
        Assert.Equal(expectList, CodeNavigationMapSettings.ViewWantsList(view));
        Assert.Equal(expectGraph, CodeNavigationMapSettings.ViewWantsGraph(view));
    }

    [Theory]
    [InlineData("list")]
    [InlineData("graph")]
    public void ControlFlow_always_shows_graph_on_pfd(string view)
    {
        Assert.True(CodeNavigationMapPresentationProjection.ShowCodeNavigationMapGraph(view, CodeNavigationMapLevelKind.ControlFlow));
    }

    [Fact]
    public void GraphClickHint_false_for_list_even_if_graph_mode_would_apply()
    {
        var hint = CodeNavigationMapPresentationProjection.ShowCodeNavigationMapGraphClickHint(
            showGraph: true,
            CodeNavigationMapLevelKind.File,
            presentationView: "list");
        Assert.False(hint);
    }

    [Fact]
    public void GraphClickHint_true_when_graph_visibility_file_level_non_list_view()
    {
        var hint = CodeNavigationMapPresentationProjection.ShowCodeNavigationMapGraphClickHint(
            showGraph: true,
            CodeNavigationMapLevelKind.File,
            presentationView: "graph");
        Assert.True(hint);
    }

    [Fact]
    public void GraphClickHint_false_for_control_flow_level()
    {
        var hint = CodeNavigationMapPresentationProjection.ShowCodeNavigationMapGraphClickHint(
            showGraph: true,
            CodeNavigationMapLevelKind.ControlFlow,
            presentationView: "both");
        Assert.False(hint);
    }

    [Theory]
    [InlineData(0, "")]
    [InlineData(1, "1 связь")]
    [InlineData(3, "3 связей")]
    public void RelatedBadge(int count, string expected)
    {
        Assert.Equal(expected, CodeNavigationMapPresentationProjection.WorkspaceNavigationMapRelatedBadge(count));
    }

    [Theory]
    [InlineData(1, null, true)]
    [InlineData(0, 5, true)]
    [InlineData(0, 1, false)]
    [InlineData(0, null, false)]
    public void HasRelated(int related, int? graphNodes, bool expect)
    {
        Assert.Equal(
            expect,
            CodeNavigationMapPresentationProjection.WorkspaceNavigationMapHasRelated(related, graphNodes));
    }

    [Fact]
    public void SettingsSummaryLine_includes_trimmed_detail()
    {
        var s = CodeNavigationMapPresentationProjection.SettingsSummaryLine("both", "file", "  normal  ");
        Assert.Contains("детализация: normal", s, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("list", "graph")]
    [InlineData("graph", "both")]
    [InlineData("both", "list")]
    [InlineData("junk", "graph")]
    public void NextPresentationViewAfter_cycles(string current, string next) =>
        Assert.Equal(next, CodeNavigationMapPresentationProjection.NextPresentationViewAfter(current));

    [Theory]
    [InlineData("file", CodeNavigationMapLevelKind.ControlFlow)]
    [InlineData("FILE", CodeNavigationMapLevelKind.ControlFlow)]
    [InlineData(CodeNavigationMapLevelKind.ControlFlow, CodeNavigationMapLevelKind.File)]
    public void ToggledMapLevel_alternates(string current, string expected) =>
        Assert.Equal(expected, CodeNavigationMapPresentationProjection.ToggledMapLevel(current));

    [Theory]
    [InlineData(CodeNavigationMapDetailLevel.Glance, CodeNavigationMapDetailLevel.Normal, "normal")]
    [InlineData(CodeNavigationMapDetailLevel.Normal, CodeNavigationMapDetailLevel.Inspect, "inspect")]
    [InlineData(CodeNavigationMapDetailLevel.Inspect, CodeNavigationMapDetailLevel.Glance, "glance")]
    public void NextDetailCycle_rotates(
        CodeNavigationMapDetailLevel current,
        CodeNavigationMapDetailLevel expectDetail,
        string expectToml)
    {
        var (d, t) = CodeNavigationMapPresentationProjection.NextDetailCycle(current);
        Assert.Equal(expectDetail, d);
        Assert.Equal(expectToml, t);
    }
}

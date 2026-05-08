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
        Assert.Equal(expectGraph, CodeNavigationMapPresentationProjection.ShowCodeNavigationMapGraph(view));
        Assert.Equal(expectList, CodeNavigationMapSettings.ViewWantsList(view));
        Assert.Equal(expectGraph, CodeNavigationMapSettings.ViewWantsGraph(view));
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
}

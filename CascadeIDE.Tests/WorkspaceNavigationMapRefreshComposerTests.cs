using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Channels.TraceFlow;
using CascadeIDE.Cockpit.Composition.TraceFlow;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceNavigationMapRefreshComposerTests
{
    private static WorkspaceNavigationMapRefreshComposer.Dependencies TestDeps() =>
        new(
            new CodeNavigationMapCompositor(),
            new TraceFlowChannelCoordinator(
            [
                new CodeFlowTraceChannel(),
                new UnitTestTraceChannel()
            ]),
            new TraceFlowCdsRouter(),
            new TraceFlowSurfaceCompositor());

    [Fact]
    public void Malformed_json_returns_friendly_status_without_invoking_cds()
    {
        var r = WorkspaceNavigationMapRefreshComposer.Compose(
            TestDeps(),
            "{bad",
            useSubgraphMode: false,
            wantList: false,
            currentPath: null,
            solutionPath: null,
            CodeNavigationMapLevelKind.File,
            graphWidth: 100,
            graphHeight: 100,
            CodeNavigationMapDetailLevel.Normal,
            new WorkspaceNavigationMapRefreshComposer.TraceSignals(0, null),
            cockpitSurfaceCapturedOnUi: null);

        Assert.Contains("разобрать", r.Status);
        Assert.Null(r.Scene);
        Assert.Empty(r.ListRows);
    }

    [Fact]
    public void Error_document_skips_scene_and_lists_and_uses_message()
    {
        var json = """{"error":"other","message":"short reason"}""";
        var r = WorkspaceNavigationMapRefreshComposer.Compose(
            TestDeps(),
            json,
            useSubgraphMode: false,
            wantList: false,
            currentPath: null,
            solutionPath: null,
            CodeNavigationMapLevelKind.File,
            graphWidth: 100,
            graphHeight: 100,
            CodeNavigationMapDetailLevel.Normal,
            new WorkspaceNavigationMapRefreshComposer.TraceSignals(0, null),
            cockpitSurfaceCapturedOnUi: null);

        Assert.Equal("short reason", r.Status);
        Assert.Null(r.Scene);
    }
}

using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Channels.TraceFlow;
using CascadeIDE.Cockpit.Composition.TraceFlow;
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
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
            CodeNavigationMapRelatedGraphLayoutKind.Radial,
            CodeNavigationMapControlFlowMainAxisKind.Auto,
            new CodeNavigationMapSettings(),
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
            CodeNavigationMapRelatedGraphLayoutKind.Radial,
            CodeNavigationMapControlFlowMainAxisKind.Auto,
            new CodeNavigationMapSettings(),
            new WorkspaceNavigationMapRefreshComposer.TraceSignals(0, null),
            cockpitSurfaceCapturedOnUi: null);

        Assert.Equal("short reason", r.Status);
        Assert.Null(r.Scene);
    }

    [Fact]
    public void File_level_subgraph_from_builder_composes_graph_scene()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "cide-navmap-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            var anchor = Path.Combine(tmp, "A.cs");
            var peer = Path.Combine(tmp, "B.cs");
            File.WriteAllText(anchor, "class A {}");
            File.WriteAllText(peer, "class B {}");
            var paths = new[] { anchor, peer };
            var json = WorkspaceNavigationMapContextJsonBuilder.Build(
                CodeNavigationMapLevelKind.File,
                wantGraph: true,
                currentPath: anchor,
                editorText: null,
                cursorLine: null,
                cursorColumn: null,
                paths,
                solutionPath: null,
                navSettings: null);

            Assert.True(GraphDocumentJson.TryParse(json, out _, out var parseErr), parseErr);

            var r = WorkspaceNavigationMapRefreshComposer.Compose(
                TestDeps(),
                json,
                useSubgraphMode: true,
                wantList: false,
                currentPath: anchor,
                solutionPath: null,
                CodeNavigationMapLevelKind.File,
                graphWidth: 280,
                graphHeight: 120,
                CodeNavigationMapDetailLevel.Normal,
                CodeNavigationMapRelatedGraphLayoutKind.Radial,
                CodeNavigationMapControlFlowMainAxisKind.Auto,
                new CodeNavigationMapSettings(),
                new WorkspaceNavigationMapRefreshComposer.TraceSignals(0, null),
                cockpitSurfaceCapturedOnUi: null);

            Assert.DoesNotContain("разобрать", r.Status);
            Assert.NotNull(r.Scene);
            Assert.True(r.Scene!.Nodes.Count >= 2);
        }
        finally
        {
            try
            {
                Directory.Delete(tmp, recursive: true);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    [Fact]
    public void Control_flow_subgraph_with_file_level_does_not_throw_generic_parse_error()
    {
        const string json = """
            {
              "mode":"subgraph",
              "graph_kind":"code_intent_code_navigation_map",
              "anchor_path":"D:/w/Program.cs",
              "nodes":[
                {"id":"n0","path":"D:/w/Program.cs","kind":"anchor","label":"Program.cs"},
                {"id":"n1","path":"D:/w/Program.cs","kind":"call_step","label":"S1","line_start":1,"line_end":2}
              ],
              "edges":[{"from_id":"n0","to_id":"n1","kind":"Call"}]
            }
            """;

        var r = WorkspaceNavigationMapRefreshComposer.Compose(
            TestDeps(),
            json,
            useSubgraphMode: true,
            wantList: false,
            currentPath: "D:/w/Program.cs",
            solutionPath: null,
            CodeNavigationMapLevelKind.File,
            graphWidth: 280,
            graphHeight: 120,
            CodeNavigationMapDetailLevel.Normal,
            CodeNavigationMapRelatedGraphLayoutKind.TopDown,
            CodeNavigationMapControlFlowMainAxisKind.Auto,
            new CodeNavigationMapSettings(),
            new WorkspaceNavigationMapRefreshComposer.TraceSignals(0, null),
            cockpitSurfaceCapturedOnUi: null);

        Assert.DoesNotContain("разобрать", r.Status);
        Assert.NotNull(r.Scene);
    }

    [Fact]
    public void File_level_zero_viewport_does_not_throw_parse_error()
    {
        const string json = """
            {
              "mode":"subgraph",
              "graph_kind":"related_files",
              "anchor_path":"D:/w/A.cs",
              "nodes":[
                {"id":"n0","path":"D:/w/A.cs","kind":"anchor","label":"A.cs"},
                {"id":"n1","path":"D:/w/B.cs","kind":"project_peer","label":"B.cs"}
              ],
              "edges":[{"from_id":"n0","to_id":"n1","kind":"related_to"}]
            }
            """;

        var r = WorkspaceNavigationMapRefreshComposer.Compose(
            TestDeps(),
            json,
            useSubgraphMode: true,
            wantList: false,
            currentPath: "D:/w/A.cs",
            solutionPath: null,
            CodeNavigationMapLevelKind.File,
            graphWidth: 0,
            graphHeight: 0,
            CodeNavigationMapDetailLevel.Normal,
            CodeNavigationMapRelatedGraphLayoutKind.Radial,
            CodeNavigationMapControlFlowMainAxisKind.Auto,
            new CodeNavigationMapSettings(),
            new WorkspaceNavigationMapRefreshComposer.TraceSignals(0, null),
            cockpitSurfaceCapturedOnUi: null);

        Assert.DoesNotContain("разобрать", r.Status);
    }

    [Fact]
    public void File_level_nan_viewport_height_does_not_throw_parse_error()
    {
        const string json = """
            {
              "mode":"subgraph",
              "graph_kind":"related_files",
              "anchor_path":"D:/w/A.cs",
              "nodes":[{"id":"n0","path":"D:/w/A.cs","kind":"anchor","label":"A.cs"}],
              "edges":[]
            }
            """;

        var r = WorkspaceNavigationMapRefreshComposer.Compose(
            TestDeps(),
            json,
            useSubgraphMode: true,
            wantList: false,
            currentPath: "D:/w/A.cs",
            solutionPath: null,
            CodeNavigationMapLevelKind.File,
            graphWidth: 280,
            graphHeight: double.NaN,
            CodeNavigationMapDetailLevel.Normal,
            CodeNavigationMapRelatedGraphLayoutKind.Radial,
            CodeNavigationMapControlFlowMainAxisKind.Auto,
            new CodeNavigationMapSettings(),
            new WorkspaceNavigationMapRefreshComposer.TraceSignals(0, null),
            cockpitSurfaceCapturedOnUi: null);

        Assert.DoesNotContain("разобрать", r.Status);
    }

    [Fact]
    public void File_level_related_wire_with_subgraph_mode_still_composes_graph()
    {
        const string json = """
            {
              "mode":"related",
              "anchor_path":"D:/w/A.cs",
              "items":[
                {"path":"D:/w/B.cs","kind":"project_peer","rationale":"peer"}
              ]
            }
            """;

        var r = WorkspaceNavigationMapRefreshComposer.Compose(
            TestDeps(),
            json,
            useSubgraphMode: true,
            wantList: false,
            currentPath: "D:/w/A.cs",
            solutionPath: null,
            CodeNavigationMapLevelKind.File,
            graphWidth: 280,
            graphHeight: 120,
            CodeNavigationMapDetailLevel.Normal,
            CodeNavigationMapRelatedGraphLayoutKind.Radial,
            CodeNavigationMapControlFlowMainAxisKind.Auto,
            new CodeNavigationMapSettings(),
            new WorkspaceNavigationMapRefreshComposer.TraceSignals(0, null),
            cockpitSurfaceCapturedOnUi: null);

        Assert.DoesNotContain("разобрать", r.Status);
        Assert.NotNull(r.Scene);
        Assert.Equal(2, r.Scene!.Nodes.Count);
    }

    /// <summary>
    /// Сквозной путь UI refresh: related wire + top_down + много связей (регрессия Math.Clamp в hierarchy layout).
    /// </summary>
    [Fact]
    public void File_level_related_wire_top_down_many_items_composes_without_parse_error()
    {
        var items = new System.Text.StringBuilder();
        for (var i = 1; i <= 37; i++)
        {
            if (i > 1)
                items.Append(',');
            items.Append($$"""{"path":"D:/w/B{{i}}.cs","kind":"project_peer","rationale":"peer"}""");
        }

        var json = $$"""
            {
              "mode":"related",
              "anchor_path":"D:/w/A.cs",
              "items":[{{items}}]
            }
            """;

        var r = WorkspaceNavigationMapRefreshComposer.Compose(
            TestDeps(),
            json,
            useSubgraphMode: true,
            wantList: false,
            currentPath: "D:/w/A.cs",
            solutionPath: null,
            CodeNavigationMapLevelKind.File,
            graphWidth: 280,
            graphHeight: 120,
            CodeNavigationMapDetailLevel.Normal,
            CodeNavigationMapRelatedGraphLayoutKind.TopDown,
            CodeNavigationMapControlFlowMainAxisKind.Auto,
            new CodeNavigationMapSettings(),
            new WorkspaceNavigationMapRefreshComposer.TraceSignals(0, null),
            cockpitSurfaceCapturedOnUi: null);

        Assert.DoesNotContain("разобрать", r.Status);
        Assert.DoesNotContain("cannot be greater than", r.Status, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(r.Scene);
        Assert.Equal(38, r.Scene!.Nodes.Count);
    }
}

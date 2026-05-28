using CascadeIDE.Cockpit.Graph;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GraphDocumentJsonTests
{
    [Fact]
    public void TryParse_UnknownGraphKind_BecomesUnspecified()
    {
        const string json = """{"mode":"subgraph","graph_kind":"reserved_future_kind","anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(GraphDocumentJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(GraphKind.Unspecified, doc!.Kind);
    }

    [Fact]
    public void TryParse_GraphKindAsNonString_BecomesUnspecified()
    {
        const string json = """{"mode":"subgraph","graph_kind":42,"anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(GraphDocumentJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(GraphKind.Unspecified, doc!.Kind);
    }

    [Fact]
    public void TryParse_EmptyGraphKindString_BecomesUnspecified()
    {
        const string json = """{"mode":"subgraph","graph_kind":"","anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(GraphDocumentJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(GraphKind.Unspecified, doc!.Kind);
    }

    [Fact]
    public void TryParse_RelatedFilesGraphKind_Parsed()
    {
        const string json = """{"mode":"subgraph","graph_kind":"related_files","anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(GraphDocumentJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(GraphKind.RelatedFiles, doc!.Kind);
    }

    [Fact]
    public void TryParse_RepositoryModuleTreeGraphKind_Parsed()
    {
        const string json = """{"mode":"subgraph","graph_kind":"repository_module_tree","anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(GraphDocumentJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(GraphKind.RepositoryModuleTree, doc!.Kind);
    }

    [Fact]
    public void TryParse_CodeIntentGraphKind_Canonical_Parsed()
    {
        const string json = """{"mode":"subgraph","graph_kind":"code_intent_code_navigation_map","anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(GraphDocumentJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(GraphKind.CodeIntent, doc!.Kind);
    }

    [Fact]
    public void TryParse_CodeIntentGraphKind_Legacy_Still_Parsed()
    {
        const string json = """{"mode":"subgraph","graph_kind":"code_intent_semantic_map","anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(GraphDocumentJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(GraphKind.CodeIntent, doc!.Kind);
    }

    [Fact]
    public void TryParseRoot_RelatedMode_BuildsStarGraphDocument()
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
        Assert.True(GraphDocumentJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(GraphKind.RelatedFiles, doc!.Kind);
        Assert.Equal(2, doc.Nodes.Count);
        Assert.Single(doc.Edges);
    }
}

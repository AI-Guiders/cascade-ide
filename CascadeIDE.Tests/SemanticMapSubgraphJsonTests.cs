using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SemanticMapSubgraphJsonTests
{
    [Fact]
    public void TryParse_UnknownGraphKind_BecomesUnspecified()
    {
        const string json = """{"mode":"subgraph","graph_kind":"reserved_future_kind","anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(SemanticMapSubgraphJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(SemanticMapGraphKind.Unspecified, doc!.GraphKind);
    }

    [Fact]
    public void TryParse_GraphKindAsNonString_BecomesUnspecified()
    {
        const string json = """{"mode":"subgraph","graph_kind":42,"anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(SemanticMapSubgraphJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(SemanticMapGraphKind.Unspecified, doc!.GraphKind);
    }

    [Fact]
    public void TryParse_EmptyGraphKindString_BecomesUnspecified()
    {
        const string json = """{"mode":"subgraph","graph_kind":"","anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(SemanticMapSubgraphJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(SemanticMapGraphKind.Unspecified, doc!.GraphKind);
    }

    [Fact]
    public void TryParse_RelatedFilesGraphKind_Parsed()
    {
        const string json = """{"mode":"subgraph","graph_kind":"related_files","anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(SemanticMapSubgraphJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(SemanticMapGraphKind.RelatedFiles, doc!.GraphKind);
    }

    [Fact]
    public void TryParse_RepositoryModuleTreeGraphKind_Parsed()
    {
        const string json = """{"mode":"subgraph","graph_kind":"repository_module_tree","anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(SemanticMapSubgraphJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(SemanticMapGraphKind.RepositoryModuleTree, doc!.GraphKind);
    }
}

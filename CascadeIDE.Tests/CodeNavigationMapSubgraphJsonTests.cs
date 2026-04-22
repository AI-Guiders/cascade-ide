using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CodeNavigationMapSubgraphJsonTests
{
    [Fact]
    public void TryParse_UnknownGraphKind_BecomesUnspecified()
    {
        const string json = """{"mode":"subgraph","graph_kind":"reserved_future_kind","anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(CodeNavigationMapSubgraphJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(CodeNavigationMapGraphKind.Unspecified, doc!.GraphKind);
    }

    [Fact]
    public void TryParse_GraphKindAsNonString_BecomesUnspecified()
    {
        const string json = """{"mode":"subgraph","graph_kind":42,"anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(CodeNavigationMapSubgraphJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(CodeNavigationMapGraphKind.Unspecified, doc!.GraphKind);
    }

    [Fact]
    public void TryParse_EmptyGraphKindString_BecomesUnspecified()
    {
        const string json = """{"mode":"subgraph","graph_kind":"","anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(CodeNavigationMapSubgraphJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(CodeNavigationMapGraphKind.Unspecified, doc!.GraphKind);
    }

    [Fact]
    public void TryParse_RelatedFilesGraphKind_Parsed()
    {
        const string json = """{"mode":"subgraph","graph_kind":"related_files","anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(CodeNavigationMapSubgraphJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(CodeNavigationMapGraphKind.RelatedFiles, doc!.GraphKind);
    }

    [Fact]
    public void TryParse_RepositoryModuleTreeGraphKind_Parsed()
    {
        const string json = """{"mode":"subgraph","graph_kind":"repository_module_tree","anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(CodeNavigationMapSubgraphJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(CodeNavigationMapGraphKind.RepositoryModuleTree, doc!.GraphKind);
    }

    [Fact]
    public void TryParse_CodeIntentGraphKind_Canonical_Parsed()
    {
        const string json = """{"mode":"subgraph","graph_kind":"code_intent_code_navigation_map","anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(CodeNavigationMapSubgraphJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(CodeNavigationMapGraphKind.CodeIntent, doc!.GraphKind);
    }

    [Fact]
    public void TryParse_CodeIntentGraphKind_Legacy_Still_Parsed()
    {
        const string json = """{"mode":"subgraph","graph_kind":"code_intent_semantic_map","anchor_path":"D:/a.cs","nodes":[],"edges":[]}""";
        Assert.True(CodeNavigationMapSubgraphJson.TryParse(json, out var doc, out var err), err);
        Assert.NotNull(doc);
        Assert.Equal(CodeNavigationMapGraphKind.CodeIntent, doc!.GraphKind);
    }
}

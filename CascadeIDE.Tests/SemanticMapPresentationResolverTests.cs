using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.Services.Navigation;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SemanticMapPresentationResolverTests
{
    [Fact]
    public void Resolve_Unspecified_UsesLevelControlFlow()
    {
        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\a.cs",
            Nodes = [],
            Edges = [],
            GraphKind = SemanticMapGraphKind.Unspecified
        };
        var p = SemanticMapPresentationResolver.Resolve(doc, SemanticMapLevelKind.ControlFlow);
        Assert.Equal(SemanticMapGraphPresentationKind.CodeControlFlow, p);
    }

    [Fact]
    public void Resolve_Unspecified_UsesLevelFile()
    {
        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\a.cs",
            Nodes = [],
            Edges = [],
            GraphKind = SemanticMapGraphKind.Unspecified
        };
        var p = SemanticMapPresentationResolver.Resolve(doc, SemanticMapLevelKind.File);
        Assert.Equal(SemanticMapGraphPresentationKind.WorkspaceRelatedFiles, p);
    }

    [Fact]
    public void Resolve_ExplicitGraphKind_OverridesLevel()
    {
        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\a.cs",
            Nodes = [],
            Edges = [],
            GraphKind = SemanticMapGraphKind.CodeIntentSemanticMap
        };
        var p = SemanticMapPresentationResolver.Resolve(doc, SemanticMapLevelKind.File);
        Assert.Equal(SemanticMapGraphPresentationKind.CodeControlFlow, p);
    }
}

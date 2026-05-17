using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Models;
using CascadeIDE.Services.Navigation;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CodeNavigationMapPresentationResolverTests
{
    [Fact]
    public void Resolve_Unspecified_UsesLevelControlFlow()
    {
        var doc = new GraphDocument
        {
            AnchorPath = @"D:\a.cs",
            Nodes = [],
            Edges = [],
            Kind = GraphKind.Unspecified
        };
        var p = CodeNavigationMapPresentationResolver.Resolve(doc, CodeNavigationMapLevelKind.ControlFlow);
        Assert.Equal(CodeNavigationMapGraphPresentationKind.CodeControlFlow, p);
    }

    [Fact]
    public void Resolve_Unspecified_UsesLevelFile()
    {
        var doc = new GraphDocument
        {
            AnchorPath = @"D:\a.cs",
            Nodes = [],
            Edges = [],
            Kind = GraphKind.Unspecified
        };
        var p = CodeNavigationMapPresentationResolver.Resolve(doc, CodeNavigationMapLevelKind.File);
        Assert.Equal(CodeNavigationMapGraphPresentationKind.WorkspaceRelatedFiles, p);
    }

    [Fact]
    public void Resolve_ExplicitGraphKind_OverridesLevel()
    {
        var doc = new GraphDocument
        {
            AnchorPath = @"D:\a.cs",
            Nodes = [],
            Edges = [],
            Kind = GraphKind.CodeIntent
        };
        var p = CodeNavigationMapPresentationResolver.Resolve(doc, CodeNavigationMapLevelKind.File);
        Assert.Equal(CodeNavigationMapGraphPresentationKind.CodeControlFlow, p);
    }
}

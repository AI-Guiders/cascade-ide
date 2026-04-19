using System.Collections.Immutable;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CascadeIDE.ArchitectureAnalyzers.Tests;

public sealed class SkiaPipelineArchitectureAnalyzerTests
{
    private static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(params (string Path, string Text)[] files)
    {
        var trees = new List<SyntaxTree>(files.Length);
        foreach (var (path, text) in files)
            trees.Add(CSharpSyntaxTree.ParseText(text, path: path));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            trees,
            Net80.References.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new SkiaPipelineArchitectureAnalyzer();
        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    [Fact]
    public async Task CASCOPE007_AvaloniaUsing_InSkiaInstruments_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Services\SkiaInstruments\Bad.cs",
            """
            using Avalonia.Controls;
            namespace CascadeIDE.Services.SkiaInstruments;
            public static class C { }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(SkiaPipelineArchitectureAnalyzer.SkiaBoundaryId, d.Id);
    }

    [Fact]
    public async Task CASCOPE008_SemanticMapCompositor_MissingDeclutter_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Services\Navigation\SemanticMapCompositor.cs",
            """
            namespace CascadeIDE.Services.Navigation;
            public sealed class SemanticMapCompositor
            {
                private readonly Intent _intentStage = new();
                private readonly Layout _layoutStage = new();
                public void Compose()
                {
                    _intentStage.Resolve();
                    _layoutStage.Layout();
                }
            }

            public sealed class Intent { public void Resolve() { } }
            public sealed class Layout { public void Layout() { } }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(SkiaPipelineArchitectureAnalyzer.SemanticMapStageFlowId, d.Id);
    }

    [Fact]
    public async Task CASCOPE009_LayoutEngineOutsideLayoutStage_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Services\Navigation\Bad.cs",
            """
            namespace CascadeIDE.Services.Navigation;
            public sealed class AnyCompositor
            {
                public object X() => new SemanticMapStarGraphLayoutEngine();
            }
            public sealed class SemanticMapStarGraphLayoutEngine { }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(SkiaPipelineArchitectureAnalyzer.LayoutBypassId, d.Id);
    }

    [Fact]
    public async Task CASCOPE010_ViewSkia_UsesPipelineState_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Views\SkiaHostRendererPipeline.cs",
            """
            using CascadeIDE.Services.Navigation;
            namespace CascadeIDE.Views;
            public sealed class SkiaHostRendererPipeline
            {
                public void Render(SemanticMapPipelineState state) { }
            }

            namespace CascadeIDE.Services.Navigation
            {
                public struct SemanticMapPipelineState { }
            }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(SkiaPipelineArchitectureAnalyzer.SkiaViewDomainLeakId, d.Id);
    }
}

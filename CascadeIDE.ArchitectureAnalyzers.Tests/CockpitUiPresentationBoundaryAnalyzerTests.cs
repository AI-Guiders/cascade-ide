using System.Collections.Immutable;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CascadeIDE.ArchitectureAnalyzers.Tests;

public sealed class CockpitUiPresentationBoundaryAnalyzerTests
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

        var analyzer = new CockpitUiPresentationBoundaryAnalyzer();
        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    [Fact]
    public async Task CASCOPE011_UiChrome_UsingPrimitivesKit_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Features\UiChrome\ModalOverlay.axaml.cs",
            """
            using CascadeIDE.Cockpit.PrimitivesKit;

            namespace CascadeIDE.Features.UiChrome;

            public partial class ModalOverlay { }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(CockpitUiPresentationBoundaryAnalyzer.UiChromeMustNotUsePrimitivesKitId, d.Id);
    }

    [Fact]
    public async Task CASCOPE011_UiChrome_WithoutPrimitivesKit_Clean()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Features\UiChrome\ModalOverlay.axaml.cs",
            """
            using Avalonia.Controls;

            namespace CascadeIDE.Features.UiChrome;

            public partial class ModalOverlay { }
            """));

        Assert.Empty(diags);
    }

    [Fact]
    public async Task CASCOPE012_PrimitivesKit_UsingUiChrome_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Cockpit\PrimitivesKit\Fake.cs",
            """
            using CascadeIDE.Features.UiChrome;

            namespace CascadeIDE.Cockpit.PrimitivesKit;

            public static class Fake { }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(CockpitUiPresentationBoundaryAnalyzer.PrimitivesKitMustNotUseUiChromeId, d.Id);
    }

    [Fact]
    public async Task CASCOPE012_PrimitivesKit_WithoutUiChrome_Clean()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Cockpit\PrimitivesKit\Fake.cs",
            """
            using Avalonia.Media;

            namespace CascadeIDE.Cockpit.PrimitivesKit;

            public static class Fake { }
            """));

        Assert.Empty(diags);
    }
}

using System.Collections.Immutable;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CascadeIDE.ArchitectureAnalyzers.Tests;

public sealed class CockpitIdeDisplayBoundaryAnalyzerTests
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

        var analyzer = new CockpitIdeDisplayBoundaryAnalyzer();
        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    [Fact]
    public async Task CASCOPE016_Cockpit_UsingIdeDisplay_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Cockpit\Channels\Bad.cs",
            """
            using CascadeIDE.IdeDisplay;

            namespace CascadeIDE.Cockpit.Channels.Example;

            public static class C { }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(CockpitIdeDisplayBoundaryAnalyzer.DiagnosticId, d.Id);
    }

    [Fact]
    public async Task Cockpit_WithoutIdeDisplay_Clean()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Cockpit\Channels\Ok.cs",
            """
            namespace CascadeIDE.Cockpit.Channels.Example;

            public static class C { }
            """));

        Assert.Empty(diags);
    }
}

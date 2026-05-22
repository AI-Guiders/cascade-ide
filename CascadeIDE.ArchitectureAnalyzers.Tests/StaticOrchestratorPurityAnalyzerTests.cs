using System.Collections.Immutable;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CascadeIDE.ArchitectureAnalyzers.Tests;

public sealed class StaticOrchestratorPurityAnalyzerTests
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

        var analyzer = new StaticOrchestratorPurityAnalyzer();
        return await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync();
    }

    [Fact]
    public async Task CASCOPE030_StaticField_InApplicationOrchestrator_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Features\IdeMcp\Application\BadOrchestrator.cs",
            """
            namespace CascadeIDE.Features.IdeMcp.Application;

            public static class BadOrchestrator
            {
                private static int _counter;
                public static int Next() => ++_counter;
            }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(StaticOrchestratorPurityAnalyzer.StatefulStaticOrchestratorId, d.Id);
    }

    [Fact]
    public async Task CASCOPE031_FileAccess_InApplicationOrchestrator_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Features\IdeMcp\Application\BadOrchestrator.cs",
            """
            namespace CascadeIDE.Features.IdeMcp.Application;

            public static class BadOrchestrator
            {
                public static string Read(string path) => File.ReadAllText(path);
            }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(StaticOrchestratorPurityAnalyzer.ExternalIoInStaticOrchestratorId, d.Id);
    }

    [Fact]
    public async Task CASCOPE031_QualifiedFileAccess_InOrchestrator_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Features\IdeMcp\Application\BadOrchestrator.cs",
            """
            namespace CascadeIDE.Features.IdeMcp.Application;

            public static class BadOrchestrator
            {
                public static bool Exists(string path) => System.IO.File.Exists(path);
            }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(StaticOrchestratorPurityAnalyzer.ExternalIoInStaticOrchestratorId, d.Id);
    }

    [Fact]
    public async Task PureStaticOrchestrator_DoesNotReport()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Features\WorkspaceNavigation\Application\GoodOrchestrator.cs",
            """
            namespace CascadeIDE.Features.WorkspaceNavigation.Application;

            public static class GoodOrchestrator
            {
                public static string Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
            }
            """));

        Assert.Empty(diags);
    }

    [Fact]
    public async Task StaticClassOutsideApplicationLayer_DoesNotReport()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Services\BadOrchestrator.cs",
            """
            namespace CascadeIDE.Services;

            public static class BadOrchestrator
            {
                private static int _counter;
            }
            """));

        Assert.Empty(diags);
    }
}

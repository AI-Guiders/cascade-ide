using System.Collections.Immutable;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CascadeIDE.ArchitectureAnalyzers.Tests;

public sealed class CockpitComputeUnitBoundaryAnalyzerTests
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

        var analyzer = new CockpitComputeUnitBoundaryAnalyzer();
        return await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync();
    }

    [Fact]
    public async Task CASCOPE020_FileRead_InComputingUnit_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Cockpit\ComputingUnits\Launch\Bad.cs",
            """
            namespace CascadeIDE.Cockpit.ComputingUnits.Launch;

            public static class Bad
            {
                public static string Read(string path) => File.ReadAllText(path);
            }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(CockpitComputeUnitBoundaryAnalyzer.ForbiddenExternalAccessId, d.Id);
    }

    [Fact]
    public async Task CASCOPE020_HttpClientCreation_InComputingUnit_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Cockpit\ComputingUnits\Launch\Bad.cs",
            """
            namespace CascadeIDE.Cockpit.ComputingUnits.Launch;

            public sealed class Bad
            {
                public object Build() => new HttpClient();
            }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(CockpitComputeUnitBoundaryAnalyzer.ForbiddenExternalAccessId, d.Id);
    }

    [Fact]
    public async Task CASCOPE021_UsingViewModels_InComputingUnit_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Cockpit\ComputingUnits\Launch\Bad.cs",
            """
            using CascadeIDE.ViewModels;
            namespace CascadeIDE.Cockpit.ComputingUnits.Launch;

            public static class Bad { }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(CockpitComputeUnitBoundaryAnalyzer.ForbiddenLayerDependencyId, d.Id);
    }

    [Fact]
    public async Task NonComputingUnit_FileAccess_DoesNotReport()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Services\Ok.cs",
            """
            namespace CascadeIDE.Services;

            public static class Ok
            {
                public static string Read(string path) => File.ReadAllText(path);
            }
            """));

        Assert.Empty(diags);
    }
}

using System.Collections.Immutable;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CascadeIDE.ArchitectureAnalyzers.Tests;

public sealed class EnvironmentReadinessPipelineAnalyzerTests
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

        var analyzer = new EnvironmentReadinessPipelineAnalyzer();
        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    [Fact]
    public async Task CASCOPE006_SnapshotBuilder_InMainWindowViewModel_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\ViewModels\MainWindowViewModel.EnvironmentReadiness.cs",
            """
            namespace CascadeIDE.ViewModels;

            public sealed class MainWindowViewModel
            {
                public object X() => EnvironmentReadinessSnapshotBuilder.BuildAllRowsAsync();
            }

            public static class EnvironmentReadinessSnapshotBuilder
            {
                public static object BuildAllRowsAsync() => new object();
            }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(EnvironmentReadinessPipelineAnalyzer.LegacySnapshotBuilderId, d.Id);
    }

    [Fact]
    public async Task ChannelAndSurfacePath_InMainWindowViewModel_DoesNotReport()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\ViewModels\MainWindowViewModel.EnvironmentReadiness.cs",
            """
            namespace CascadeIDE.ViewModels;

            public sealed class MainWindowViewModel
            {
                private readonly ReadinessChannel _channel = new();
                private readonly ReadinessSurface _surface = new();

                public void Refresh()
                {
                    var rows = _channel.Build(0);
                    _surface.Compose(rows);
                }
            }

            public sealed class ReadinessChannel
            {
                public int Build(int context) => context;
            }

            public sealed class ReadinessSurface
            {
                public void Compose(int payload) { }
            }
            """));

        Assert.Empty(diags);
    }
}

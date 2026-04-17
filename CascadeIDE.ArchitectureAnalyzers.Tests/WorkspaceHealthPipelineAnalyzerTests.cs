using System.Collections.Immutable;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CascadeIDE.ArchitectureAnalyzers.Tests;

public sealed class WorkspaceHealthPipelineAnalyzerTests
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

        var analyzer = new WorkspaceHealthPipelineAnalyzer();
        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    [Fact]
    public async Task CASCOPE004_GetSnapshot_InMainWindowViewModel_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\ViewModels\MainWindowViewModel.WorkspaceHealth.cs",
            """
            namespace CascadeIDE.ViewModels;

            public sealed class MainWindowViewModel
            {
                private readonly LegacyHealth _workspaceHealth = new();
                private void Rebuild() => _workspaceHealth.GetSnapshot();
            }

            public sealed class LegacyHealth
            {
                public int GetSnapshot() => 1;
            }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(WorkspaceHealthPipelineAnalyzer.LegacyGetSnapshotId, d.Id);
    }

    [Fact]
    public async Task Build_InMainWindowViewModel_DoesNotReport()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\ViewModels\MainWindowViewModel.WorkspaceHealth.cs",
            """
            namespace CascadeIDE.ViewModels;

            public sealed class MainWindowViewModel
            {
                private readonly HealthChannel _workspaceHealth = new();
                private void Rebuild() => _workspaceHealth.Build(0);
            }

            public sealed class HealthChannel
            {
                public int Build(int context) => context;
            }
            """));

        Assert.Empty(diags);
    }

    [Fact]
    public async Task CASCOPE005_WorkspaceHealthSegmentBuilder_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Any\SomeFile.cs",
            """
            namespace CascadeIDE.Any;

            public static class WorkspaceHealthSegmentBuilder
            {
                public static void Rebuild() { }
            }

            public sealed class Use
            {
                public void Go() => WorkspaceHealthSegmentBuilder.Rebuild();
            }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(WorkspaceHealthPipelineAnalyzer.LegacySegmentBuilderId, d.Id);
    }
}

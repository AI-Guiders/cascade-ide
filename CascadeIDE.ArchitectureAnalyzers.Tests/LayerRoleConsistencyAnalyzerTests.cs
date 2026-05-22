#nullable enable

using System.Collections.Immutable;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CascadeIDE.ArchitectureAnalyzers.Tests;

public sealed class LayerRoleConsistencyAnalyzerTests
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

        var analyzer = new LayerRoleConsistencyAnalyzer();
        return await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync();
    }

    [Fact]
    public async Task CASCOPE032_OrchestratorWithoutAttribute_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Features\Chat\Application\FooOrchestrator.cs",
            """
            namespace CascadeIDE.Features.Chat.Application;
            public static class FooOrchestrator { }
            """));

        Assert.Contains(
            diags,
            d => d.Id == LayerRoleConsistencyAnalyzer.MissingApplicationOrchestratorAttributeId);
    }

    [Fact]
    public async Task CASCOPE033_CcuWithoutAttribute_Reports()
    {
        var diags = await RunAnalyzerAsync(
            (
                @"D:\repo\Cockpit\ComputingUnits\Foo\BarUnit.cs",
                """
                namespace CascadeIDE.Cockpit.ComputingUnits.Foo;
                public sealed class BarUnit : ICockpitComputeUnit { }
                """),
            (
                @"D:\repo\Cockpit\ComputingUnits\ICockpitComputeUnit.cs",
                """
                namespace CascadeIDE.Cockpit.ComputingUnits;
                public interface ICockpitComputeUnit { }
                """));

        Assert.Contains(
            diags,
            d => d.Id == LayerRoleConsistencyAnalyzer.MissingComputingUnitAttributeId);
    }

    [Fact]
    public async Task CASCOPE036_ComputingUnitOnOrchestratorName_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Features\X\Application\BadOrchestrator.cs",
            """
            namespace CascadeIDE.Contracts;
            [System.AttributeUsage(System.AttributeTargets.Class)]
            sealed class ComputingUnitAttribute : System.Attribute { }

            namespace CascadeIDE.Features.X.Application;
            [ComputingUnit]
            public static class BadOrchestrator { }
            """));

        Assert.Contains(
            diags,
            d => d.Id == LayerRoleConsistencyAnalyzer.ComputingUnitOnOrchestratorNameId);
    }

    [Fact]
    public async Task CASCOPE040_PresentationProjection_FileIo_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Features\Shell\Application\BadProjection.cs",
            """
            namespace CascadeIDE.Contracts;
            [System.AttributeUsage(System.AttributeTargets.Class)]
            sealed class PresentationProjectionAttribute : System.Attribute { }

            namespace CascadeIDE.Features.Shell.Application;
            [PresentationProjection]
            public static class BadProjection
            {
                public static bool Exists(string path) => System.IO.File.Exists(path);
            }
            """));

        Assert.Contains(
            diags,
            d => d.Id == LayerRoleConsistencyAnalyzer.PresentationProjectionForbiddenIoId);
    }

    [Fact]
    public async Task MarkedOrchestrator_NoCasope032()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Features\X\Application\GoodOrchestrator.cs",
            """
            namespace CascadeIDE.Contracts;
            [System.AttributeUsage(System.AttributeTargets.Class)]
            sealed class ApplicationOrchestratorAttribute : System.Attribute { }

            namespace CascadeIDE.Features.X.Application;
            [ApplicationOrchestrator]
            public static class GoodOrchestrator
            {
                public static System.Threading.Tasks.Task RunAsync() => System.Threading.Tasks.Task.CompletedTask;
            }
            """));

        Assert.DoesNotContain(
            diags,
            d => d.Id == LayerRoleConsistencyAnalyzer.MissingApplicationOrchestratorAttributeId);
    }

    [Fact]
    public async Task CASCOPE039_McpServiceFacade_WithApplicationOrchestrator_NoReport()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Features\IdeMcp\Application\NotesOrchestrator.cs",
            """
            namespace CascadeIDE.Contracts;
            [System.AttributeUsage(System.AttributeTargets.Class)]
            sealed class ApplicationOrchestratorAttribute : System.Attribute { }

            namespace CascadeIDE.Features.IdeMcp.Application;
            sealed class McpAgentNotesService
            {
                public string Write(string path, string content) => content;
            }

            [ApplicationOrchestrator]
            public static class NotesOrchestrator
            {
                public static string Write(McpAgentNotesService svc, string content) =>
                    svc.Write("x", content);
            }
            """));

        Assert.DoesNotContain(
            diags,
            d => d.Id == LayerRoleConsistencyAnalyzer.OrchestratorLooksLikeProjectionId);
    }

    [Fact]
    public async Task CASCOPE039_PureNormalizeOrchestrator_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Features\Shell\Application\NormalizeOrchestrator.cs",
            """
            namespace CascadeIDE.Contracts;
            [System.AttributeUsage(System.AttributeTargets.Class)]
            sealed class ApplicationOrchestratorAttribute : System.Attribute { }

            namespace CascadeIDE.Features.Shell.Application;
            [ApplicationOrchestrator]
            public static class NormalizeOrchestrator
            {
                public static string Normalize(string? value) => value ?? "";
            }
            """));

        Assert.Contains(
            diags,
            d => d.Id == LayerRoleConsistencyAnalyzer.OrchestratorLooksLikeProjectionId);
    }

    [Fact]
    public async Task CASCOPE038_MarkedComputingUnit_StaticHelper_NoReport()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Features\Shell\Application\MarkedHelper.cs",
            """
            namespace CascadeIDE.Contracts;
            [System.AttributeUsage(System.AttributeTargets.Class)]
            sealed class ComputingUnitAttribute : System.Attribute { }

            namespace CascadeIDE.Features.Shell.Application;
            [ComputingUnit]
            public static class IntercomFeedProjector
            {
                public static string Format(string s) => s;
            }
            """));

        Assert.DoesNotContain(
            diags,
            d => d.Id == LayerRoleConsistencyAnalyzer.StaticHelperSuggestProjectionOrCuId);
    }
}

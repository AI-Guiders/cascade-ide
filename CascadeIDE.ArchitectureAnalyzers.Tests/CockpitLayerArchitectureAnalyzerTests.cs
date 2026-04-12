using System.Collections.Immutable;
using System.Globalization;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CascadeIDE.ArchitectureAnalyzers.Tests;

public sealed class CockpitLayerArchitectureAnalyzerTests
{
    private static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(params (string Path, string Text)[] files)
    {
        var trees = new List<SyntaxTree>(files.Length);
        foreach (var (path, text) in files)
            trees.Add(CSharpSyntaxTree.ParseText(text, path: path));

        var references = Net80.References.All;
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new CockpitLayerArchitectureAnalyzer();
        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    [Fact]
    public async Task CASCOPE001_UsingAvalonia_InChannelsPath_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Cockpit\Channels\Bad.cs",
            """
            using Avalonia;
            namespace CascadeIDE.Cockpit.Channels.Example;

            public static class C { }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(CockpitLayerArchitectureAnalyzer.AvaloniaNamespaceId, d.Id);
        Assert.Contains("Channels", d.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CASCOPE002_UsingUiChrome_InCdsPath_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Cockpit\Cds\Bad.cs",
            """
            using CascadeIDE.Features.UiChrome;
            namespace CascadeIDE.Cockpit.Cds.Example;

            public static class C { }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(CockpitLayerArchitectureAnalyzer.FeaturesUiChromeId, d.Id);
    }

    [Fact]
    public async Task UsingAvalonia_InSurfacePath_DoesNotReport()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Cockpit\Surface\Ok.cs",
            """
            using Avalonia;
            namespace CascadeIDE.Cockpit.Surface.Example;

            public static class C { }
            """));

        Assert.Empty(diags);
    }

    [Fact]
    public async Task CASCOPE001_AvaloniaTypeInMember_InCompositionNamespace_Reports()
    {
        var diags = await RunAnalyzerAsync(
            (@"D:\repo\Support\AvaloniaStub.cs", """
namespace Avalonia.Controls;

public sealed class Control { }
"""),
            (@"D:\repo\Cockpit\Composition\Bad.cs", """
namespace CascadeIDE.Cockpit.Composition.Example;

public sealed class Host
{
    public Avalonia.Controls.Control? Widget { get; set; }
}
"""));

        var d = Assert.Single(diags);
        Assert.Equal(CockpitLayerArchitectureAnalyzer.AvaloniaNamespaceId, d.Id);
        Assert.Contains("Avalonia.Controls.Control", d.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ChannelsNamespace_WithoutAvalonia_DoesNotReport()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\Cockpit\Channels\Ok.cs",
            """
            namespace CascadeIDE.Cockpit.Channels.Example;

            public static class C
            {
                public static string Id => "ok";
            }
            """));

        Assert.Empty(diags);
    }
}

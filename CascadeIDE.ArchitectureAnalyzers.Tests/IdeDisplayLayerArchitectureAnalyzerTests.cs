using System.Collections.Immutable;
using System.Globalization;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CascadeIDE.ArchitectureAnalyzers.Tests;

public sealed class IdeDisplayLayerArchitectureAnalyzerTests
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

        var analyzer = new IdeDisplayLayerArchitectureAnalyzer();
        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    [Fact]
    public async Task CASCOPE013_UsingCockpit_InIdeDisplay_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\IdeDisplay\Bad.cs",
            """
            using CascadeIDE.Cockpit.Channels;

            namespace CascadeIDE.IdeDisplay.Example;

            public static class C { }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(IdeDisplayLayerArchitectureAnalyzer.CockpitNamespaceId, d.Id);
    }

    [Fact]
    public async Task CASCOPE014_UsingAvalonia_InIdeDisplay_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\IdeDisplay\Bad.cs",
            """
            using Avalonia.Media;

            namespace CascadeIDE.IdeDisplay.Example;

            public static class C { }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(IdeDisplayLayerArchitectureAnalyzer.AvaloniaNamespaceId, d.Id);
    }

    [Fact]
    public async Task CASCOPE015_UsingUiChrome_InIdeDisplay_Reports()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\IdeDisplay\Bad.cs",
            """
            using CascadeIDE.Features.UiChrome;

            namespace CascadeIDE.IdeDisplay.Example;

            public static class C { }
            """));

        var d = Assert.Single(diags);
        Assert.Equal(IdeDisplayLayerArchitectureAnalyzer.FeaturesUiChromeId, d.Id);
    }

    [Fact]
    public async Task IdeDisplay_WithoutRestrictedUsings_Clean()
    {
        var diags = await RunAnalyzerAsync((
            @"D:\repo\IdeDisplay\Ok.cs",
            """
            namespace CascadeIDE.IdeDisplay.Example;

            public static class C { }
            """));

        Assert.Empty(diags);
    }

    [Fact]
    public async Task CASCOPE014_AvaloniaTypeInMember_InIdeDisplayNamespace_Reports()
    {
        var diags = await RunAnalyzerAsync(
            (@"D:\repo\Support\AvaloniaStub.cs", """
namespace Avalonia.Controls;

public sealed class Control { }
"""),
            (@"D:\repo\IdeDisplay\Bad.cs", """
namespace CascadeIDE.IdeDisplay.Example;

public sealed class Host
{
    public Avalonia.Controls.Control? Widget { get; set; }
}
"""));

        var d = Assert.Single(diags);
        Assert.Equal(IdeDisplayLayerArchitectureAnalyzer.AvaloniaNamespaceId, d.Id);
        Assert.Contains("Avalonia", d.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CASCOPE013_CockpitTypeInMember_InIdeDisplayNamespace_Reports()
    {
        var diags = await RunAnalyzerAsync(
            (@"D:\repo\Support\CockpitStub.cs", """
namespace CascadeIDE.Cockpit.Channels;

public sealed class ChannelState { }
"""),
            (@"D:\repo\IdeDisplay\Bad.cs", """
namespace CascadeIDE.IdeDisplay.Example;

public sealed class Host
{
    public CascadeIDE.Cockpit.Channels.ChannelState? State { get; set; }
}
"""));

        var d = Assert.Single(diags);
        Assert.Equal(IdeDisplayLayerArchitectureAnalyzer.CockpitNamespaceId, d.Id);
    }
}

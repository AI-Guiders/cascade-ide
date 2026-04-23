using System.Collections.Immutable;
using System.Text;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace CascadeIDE.ArchitectureAnalyzers.Tests;

public sealed class MfdShellViewXamlLeanAnalyzerTests
{
    private sealed class StringAdditionalText : AdditionalText
    {
        private readonly string _text;

        public StringAdditionalText(string path, string text)
        {
            Path = path;
            _text = text;
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(_text, Encoding.UTF8, SourceHashAlgorithm.Sha1);
    }

    private static async Task<ImmutableArray<Diagnostic>> RunWithAxamlAsync(
        string xaml,
        string fileName = "MfdShellView.axaml")
    {
        var tree = CSharpSyntaxTree.ParseText("namespace T { }", path: "dummy.cs");
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            Net80.References.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var additional = new StringAdditionalText(
            $@"D:\repo\cascade-ide\Views\{fileName}",
            xaml);

        var options = new AnalyzerOptions(ImmutableArray.Create<AdditionalText>(additional));
        var withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new MfdShellViewXamlLeanAnalyzer()),
            options);

        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    [Fact]
    public async Task LeanShell_BelowLimit_NoDiagnostics()
    {
        var xaml = """
            <UserControl>
              <Grid>
                <Border><views:BuildMfdPageView/></Border>
              </Grid>
            </UserControl>
            """;
        var diags = await RunWithAxamlAsync(xaml);
        Assert.Empty(diags);
    }

    [Fact]
    public async Task MfdShellView_TooManyLines_Reports017()
    {
        var lines = new StringBuilder();
        lines.AppendLine("<UserControl><Grid></Grid></UserControl>");
        for (var i = 0; i < MfdShellViewXamlLeanAnalyzer.MaxLineCountMfdShellView; i++)
            lines.AppendLine("<!--x-->");

        var diags = await RunWithAxamlAsync(lines.ToString());
        var d = Assert.Single(diags);
        Assert.Equal(MfdShellViewXamlLeanAnalyzer.TooLongId, d.Id);
    }

    [Fact]
    public async Task MfdShellPageStack_TooManyLines_Reports017()
    {
        var lines = new StringBuilder();
        lines.AppendLine("<UserControl><Grid></Grid></UserControl>");
        for (var i = 0; i < MfdShellViewXamlLeanAnalyzer.MaxLineCountMfdShellPageStack; i++)
            lines.AppendLine("<!--x-->");

        var diags = await RunWithAxamlAsync(lines.ToString(), MfdShellViewXamlLeanAnalyzer.PageStackFileName);
        var d = Assert.Single(diags);
        Assert.Equal(MfdShellViewXamlLeanAnalyzer.TooLongId, d.Id);
    }

    [Fact]
    public async Task ListBox_Reports018()
    {
        var xaml = """
            <UserControl>
              <ListBox></ListBox>
            </UserControl>
            """;
        var diags = await RunWithAxamlAsync(xaml);
        var d = Assert.Single(diags);
        Assert.Equal(MfdShellViewXamlLeanAnalyzer.InlineControlsId, d.Id);
    }
}

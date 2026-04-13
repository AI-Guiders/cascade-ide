using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.Services.Lsp;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class EnvironmentReadinessSnapshotBuilderTests
{
    [Fact]
    public void BuildLspRows_ParseOnly_NoHost_IsInfo()
    {
        var settings = new CascadeIdeSettings { CSharpLsp = new CSharpLspSettings { Provider = CSharpLspProviderIds.ParseOnly } };
        var rows = EnvironmentReadinessSnapshotBuilder.BuildLspRows(
            settings,
            solutionPath: null,
            csharpHost: null,
            markdownHost: null);

        Assert.Contains(rows, r => r.Title == "C# LSP" && r.Level == EnvironmentReadinessLevel.Info);
    }

    [Fact]
    public void BuildLspRows_MarkdownOff_IsInfo()
    {
        var settings = new CascadeIdeSettings
        {
            CSharpLsp = new CSharpLspSettings { Provider = CSharpLspProviderIds.ParseOnly },
            MarkdownLsp = new MarkdownLspSettings { Provider = MarkdownLspProviderIds.Off }
        };
        var rows = EnvironmentReadinessSnapshotBuilder.BuildLspRows(settings, null, null, null);

        Assert.Contains(rows, r => r.Title == "Markdown LSP" && r.Level == EnvironmentReadinessLevel.Info);
    }

    [Fact]
    public void BuildLspRows_CSharpProcess_NoSolution_IsWarning()
    {
        var settings = new CascadeIdeSettings { CSharpLsp = new CSharpLspSettings { Provider = CSharpLspProviderIds.CSharpLs } };
        var rows = EnvironmentReadinessSnapshotBuilder.BuildLspRows(settings, null, null, null);

        var row = Assert.Single(rows, r => r.Title == "C# LSP");
        Assert.Equal(EnvironmentReadinessLevel.Warning, row.Level);
    }
}

using CascadeIDE.Cockpit.Composition.EnvironmentReadiness;
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
        var settings = new CascadeIdeSettings
        {
            Languages = new LanguagesSettings
            {
                CSharp = new LanguageServerProfile { Provider = CSharpLspProviderIds.ParseOnly }
            }
        };
        var rows = EnvironmentReadinessSnapshotBuilder.BuildLspRows(
            settings,
            solutionPath: null,
            csharpHost: null,
            markdownHost: null);

        Assert.Contains(rows, r => r.Title == "C# LSP" && r.Level == AnnunciatorLampLevel.Advisory);
    }

    [Fact]
    public void BuildLspRows_MarkdownOff_IsInfo()
    {
        var settings = new CascadeIdeSettings
        {
            Languages = new LanguagesSettings
            {
                CSharp = new LanguageServerProfile { Provider = CSharpLspProviderIds.ParseOnly },
                Markdown = new LanguageServerProfile { Provider = MarkdownLspProviderIds.Off }
            }
        };
        var rows = EnvironmentReadinessSnapshotBuilder.BuildLspRows(settings, null, null, null);

        Assert.Contains(rows, r => r.Title == "Markdown LSP" && r.Level == AnnunciatorLampLevel.Advisory);
    }

    [Fact]
    public void BuildLspRows_CSharpProcess_NoSolution_IsWarning()
    {
        var settings = new CascadeIdeSettings
        {
            Languages = new LanguagesSettings
            {
                CSharp = new LanguageServerProfile { Provider = CSharpLspProviderIds.CSharpLs }
            }
        };
        var rows = EnvironmentReadinessSnapshotBuilder.BuildLspRows(settings, null, null, null);

        var row = Assert.Single(rows, r => r.Title == "C# LSP");
        Assert.Equal(AnnunciatorLampLevel.Caution, row.Level);
    }

    [Fact]
    public async Task BuildAllRowsAsync_cell_order_matches_environment_readiness_instrument_deck()
    {
        var settings = new CascadeIdeSettings
        {
            Languages = new LanguagesSettings
            {
                CSharp = new LanguageServerProfile { Provider = CSharpLspProviderIds.ParseOnly },
                Markdown = new LanguageServerProfile { Provider = MarkdownLspProviderIds.Off }
            }
        };

        var rows = await EnvironmentReadinessSnapshotBuilder.BuildAllRowsAsync(settings, null, null, null);

        Assert.Equal(EnvironmentReadinessInstrumentDeck.OrderedCellIds.Count, rows.Count);
        for (var i = 0; i < rows.Count; i++)
            Assert.Equal(EnvironmentReadinessInstrumentDeck.OrderedCellIds[i], rows[i].Id);

        Assert.Equal(EnvironmentReadinessCellIds.Agent, rows[0].Id);
        Assert.Equal("AI", rows[0].LampShortLabel);
    }
}

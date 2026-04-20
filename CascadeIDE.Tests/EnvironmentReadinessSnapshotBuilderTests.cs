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

        Assert.Equal(EnvironmentReadinessCellIds.DevToolsSection, rows[0].Id);
        Assert.Equal("DEV", rows[0].LampShortLabel);
        Assert.Equal(AnnunciatorLampLevel.Caution, rows[0].Level);

        Assert.Equal(EnvironmentReadinessCellIds.Agent, rows[1].Id);
        Assert.Equal("Off", rows[1].LampShortLabel);
        Assert.Equal(AnnunciatorLampLevel.Caution, rows[1].Level);

        Assert.Equal(EnvironmentReadinessCellIds.CSharpLsp, rows[2].Id);
        Assert.Equal("C#", rows[2].LampShortLabel);
        Assert.Equal(AnnunciatorLampLevel.Advisory, rows[2].Level);

        Assert.Equal(EnvironmentReadinessCellIds.EnvSection, rows[5].Id);
        Assert.Equal("ENV", rows[5].LampShortLabel);
    }

    [Fact]
    public void AggregateEnvBlockLevel_only_ok_and_advisory_yields_ok_for_section_lamp()
    {
        Assert.Equal(
            AnnunciatorLampLevel.Ok,
            EnvironmentReadinessSnapshotBuilder.AggregateEnvBlockLevel(
                AnnunciatorLampLevel.Ok,
                AnnunciatorLampLevel.Advisory,
                AnnunciatorLampLevel.Advisory));
    }

    [Fact]
    public void AggregateEnvBlockLevel_critical_dominates()
    {
        Assert.Equal(
            AnnunciatorLampLevel.Critical,
            EnvironmentReadinessSnapshotBuilder.AggregateEnvBlockLevel(
                AnnunciatorLampLevel.Advisory,
                AnnunciatorLampLevel.Critical,
                AnnunciatorLampLevel.Ok));
    }

    [Fact]
    public void BuildEnvSectionRow_matches_aggregate_and_labels()
    {
        var rows = new[]
        {
            new AnnunciatorLampItem(EnvironmentReadinessCellIds.AgentNotesFile, "a", "", AnnunciatorLampLevel.Ok, "Notes"),
            new AnnunciatorLampItem(EnvironmentReadinessCellIds.AgentNotesCanonPath, "b", "", AnnunciatorLampLevel.Advisory, "KB"),
            new AnnunciatorLampItem(EnvironmentReadinessCellIds.NetcoreDbgPath, "c", "", AnnunciatorLampLevel.Advisory, "Dbg"),
        };
        var section = EnvironmentReadinessSnapshotBuilder.BuildEnvSectionRow(rows);
        Assert.Equal(EnvironmentReadinessCellIds.EnvSection, section.Id);
        Assert.Equal(AnnunciatorLampLevel.Ok, section.Level);
        Assert.Equal("Переменные окружения", section.Title);
        Assert.Equal("ENV", section.LampShortLabel);
        Assert.Empty(section.Detail);
    }

    [Fact]
    public void BuildDevToolsSectionRow_mcp_advisory_yields_ok_lamp()
    {
        var agent = new AnnunciatorLampItem(
            EnvironmentReadinessCellIds.Agent,
            "Агент (MCP)",
            "",
            AnnunciatorLampLevel.Advisory,
            "MCP");
        var section = EnvironmentReadinessSnapshotBuilder.BuildDevToolsSectionRow([agent]);
        Assert.Equal(EnvironmentReadinessCellIds.DevToolsSection, section.Id);
        Assert.Equal(AnnunciatorLampLevel.Ok, section.Level);
        Assert.Equal("Dev Tools", section.Title);
        Assert.Equal("DEV", section.LampShortLabel);
        Assert.Empty(section.Detail);
    }

    [Fact]
    public void BuildDevToolsSectionRow_no_bridge_caution_preserved()
    {
        var agent = new AnnunciatorLampItem(
            EnvironmentReadinessCellIds.Agent,
            "Агент (нет моста)",
            "",
            AnnunciatorLampLevel.Caution,
            "Off");
        var section = EnvironmentReadinessSnapshotBuilder.BuildDevToolsSectionRow([agent]);
        Assert.Equal(AnnunciatorLampLevel.Caution, section.Level);
        Assert.NotEmpty(section.Detail);
    }

    [Fact]
    public async Task BuildAllRowsAsync_agent_row_mcp_stdio_is_advisory()
    {
        var settings = new CascadeIdeSettings
        {
            Languages = new LanguagesSettings
            {
                CSharp = new LanguageServerProfile { Provider = CSharpLspProviderIds.ParseOnly },
                Markdown = new LanguageServerProfile { Provider = MarkdownLspProviderIds.Off }
            }
        };

        var rows = await EnvironmentReadinessSnapshotBuilder.BuildAllRowsAsync(
            settings, null, null, null,
            isMcpStdioHost: true, activeAiProvider: "Ollama");

        Assert.Equal("DEV", rows[0].LampShortLabel);
        Assert.Equal(AnnunciatorLampLevel.Ok, rows[0].Level);
        Assert.Equal("MCP", rows[1].LampShortLabel);
        Assert.Equal(AnnunciatorLampLevel.Advisory, rows[1].Level);
    }

    [Fact]
    public async Task BuildAllRowsAsync_agent_row_cursor_acp_is_advisory()
    {
        var settings = new CascadeIdeSettings
        {
            Languages = new LanguagesSettings
            {
                CSharp = new LanguageServerProfile { Provider = CSharpLspProviderIds.ParseOnly },
                Markdown = new LanguageServerProfile { Provider = MarkdownLspProviderIds.Off }
            }
        };

        var rows = await EnvironmentReadinessSnapshotBuilder.BuildAllRowsAsync(
            settings, null, null, null,
            isMcpStdioHost: false, activeAiProvider: "CursorACP");

        Assert.Equal("DEV", rows[0].LampShortLabel);
        Assert.Equal(AnnunciatorLampLevel.Ok, rows[0].Level);
        Assert.Equal("ACP", rows[1].LampShortLabel);
        Assert.Equal(AnnunciatorLampLevel.Advisory, rows[1].Level);
    }
}

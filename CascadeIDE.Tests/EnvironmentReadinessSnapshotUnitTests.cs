using CascadeIDE.Cockpit.Channels.EnvironmentReadiness;
using CascadeIDE.Cockpit.ComputingUnits;
using CascadeIDE.Cockpit.ComputingUnits.EnvironmentReadiness;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Models;
using CascadeIDE.Services.Lsp;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class EnvironmentReadinessSnapshotUnitTests
{
    [Fact]
    public async Task BuildAsync_returns_rows_for_environment_readiness_deck_order()
    {
        var settings = new CascadeIdeSettings
        {
            Languages = new LanguagesSettings
            {
                CSharp = new CSharpLanguageServerSettings { Mode = CSharpLspProviderIds.ParseOnly },
                Markdown = new MarkdownLanguageServerSettings { Mode = MarkdownLspProviderIds.Off }
            }
        };

        var rows = await EnvironmentReadinessSnapshotUnit.Default.BuildAsync(
            new EnvironmentReadinessChannelContext(
                settings,
                SolutionPath: null,
                Lsp: default,
                IsMcpStdioHost: false,
                ActiveAiProvider: null));

        Assert.NotEmpty(rows);
        Assert.Equal(EnvironmentReadinessCellIds.DevToolsSection, rows[0].Id);
        Assert.Equal(EnvironmentReadinessCellIds.Agent, rows[1].Id);
    }

    [Fact]
    public void EnvironmentReadinessSnapshotUnit_default_implements_ICockpitComputeUnit()
    {
        ICockpitComputeUnit unit = EnvironmentReadinessSnapshotUnit.Default;
        Assert.NotNull(unit);
    }
}

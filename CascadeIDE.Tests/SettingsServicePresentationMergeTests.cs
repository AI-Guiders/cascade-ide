using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SettingsServicePresentationMergeTests
{
    [Fact]
    public void ApplyPresentationFromDisk_CopiesDisplayScreensTopologyAndGrammar()
    {
        var target = new CascadeIdeSettings
        {
            Display = new DisplaySettings
            {
                Screens = new DisplayScreensSettings
                {
                    Topology = "",
                    Grammar = new PresentationGrammarSettings(),
                },
            },
        };
        var disk = new CascadeIdeSettings
        {
            Display = new DisplaySettings
            {
                Screens = new DisplayScreensSettings
                {
                    Topology = "(P+F) (M)",
                    Grammar = new PresentationGrammarSettings
                    {
                        Pfd = "P",
                        Forward = "F",
                        Mfd = "M",
                    },
                },
            },
        };

        SettingsService.ApplyPresentationFromDisk(target, disk);

        Assert.Equal("(P+F) (M)", target.Display.Screens.Topology);
        Assert.Equal("P", target.Display.Screens.Grammar.Pfd);
    }
}

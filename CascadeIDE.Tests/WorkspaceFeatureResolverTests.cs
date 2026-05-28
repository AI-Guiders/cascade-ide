using CascadeIDE.Features.Workspace;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceFeatureResolverTests
{
    [Fact]
    public void ResolveFeature_longest_prefix_wins()
    {
        var w = new RepositoryWorkspaceToml
        {
            Workspace = new RepositoryWorkspaceSectionToml
            {
                Features = new RepositoryFeaturesToml
                {
                    Feature =
                    [
                        new RepositoryFeatureToml
                        {
                            Id = "ui",
                            Title = "UI",
                            Paths = ["Features/"],
                            Docs = ["docs/adr/0006-presentation-layers-and-feature-slices.md"]
                        },
                        new RepositoryFeatureToml
                        {
                            Id = "uichrome",
                            Title = "UiChrome",
                            Paths = ["Features/UiChrome/"],
                            Docs = ["docs/adr/0010-ui-mode-catalog.md"]
                        },
                    ]
                }
            }
        };

        var f = WorkspaceFeatureResolver.ResolveFeatureFromWorkspaceToml(
            w,
            repositoryRootDirectory: "D:/w",
            absoluteFilePath: "D:/w/Features/UiChrome/UiModeCatalog.cs");

        Assert.NotNull(f);
        Assert.Equal("uichrome", f!.Id);
        Assert.Contains("docs/adr/0010-ui-mode-catalog.md", f.Docs);
        Assert.Contains("Feature: UiChrome (uichrome)", WorkspaceFeatureResolver.BuildFeatureLine(f));
    }
}


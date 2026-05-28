using CascadeIDE.Features.UiChrome;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceAdrMapResolverTests
{
    [Fact]
    public void ResolveAdrDocPaths_longest_prefix_wins_over_star()
    {
        var w = new UiWorkspaceToml
        {
            Workspace = new UiWorkspaceWorkspaceToml
            {
                Adr = new UiWorkspaceAdrToml
                {
                    Map = new Dictionary<string, object>
                    {
                        ["*"] = "docs/adr/0006-presentation-layers-and-feature-slices.md",
                        ["Features/UiChrome/"] = "docs/adr/0010-ui-mode-catalog.md",
                        ["Features/UiChrome/UiModeCatalog.cs"] = "docs/adr/0010-ui-mode-catalog.md",
                    }
                }
            }
        };

        var r = WorkspaceAdrMapResolver.ResolveAdrDocPathsFromWorkspaceToml(
            w,
            repositoryRootDirectory: "D:/w",
            absoluteFilePath: "D:/w/Features/UiChrome/UiModeCatalog.cs");

        Assert.Single(r);
        Assert.Equal("docs/adr/0010-ui-mode-catalog.md", r[0]);
        Assert.Equal("ADR: ADR 0010", WorkspaceAdrMapResolver.BuildAdrIndicatorLine(r));
    }

    [Fact]
    public void ResolveAdrDocPaths_array_values_are_supported()
    {
        var w = new UiWorkspaceToml
        {
            Workspace = new UiWorkspaceWorkspaceToml
            {
                Adr = new UiWorkspaceAdrToml
                {
                    Map = new Dictionary<string, object>
                    {
                        ["src/ui/"] = new object[]
                        {
                            "docs/adr/0055-skia-instrument-composition-pipeline.md",
                            "docs/adr/0049-skia-surface-rollout-over-avalonia-host.md",
                        }
                    }
                }
            }
        };

        var r = WorkspaceAdrMapResolver.ResolveAdrDocPathsFromWorkspaceToml(
            w,
            repositoryRootDirectory: "D:/w",
            absoluteFilePath: "D:/w/src/ui/skia/Graph.cs");

        Assert.Equal(2, r.Count);
        Assert.Contains("docs/adr/0055-skia-instrument-composition-pipeline.md", r);
        Assert.Contains("docs/adr/0049-skia-surface-rollout-over-avalonia-host.md", r);
    }

    [Fact]
    public void ExtractLinkedAdrDocPathsFromMarkdown_supports_relative_and_docs_adr_links()
    {
        var md = """
            # ADR 9999

            See [ADR 0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md) and
            [ADR 0155](../adr/0155-documentation-code-correspondence-and-architectural-drift.md).
            Ignore [web](https://example.com/a.md) and [non-adr](../design/feature-archetype-v1.md).
            """;

        var linked = WorkspaceAdrMapResolver.ExtractLinkedAdrDocPathsFromMarkdown(
            md,
            currentDocRepoRelativePath: "docs/adr/9999-sample.md");

        Assert.Contains("docs/adr/0061-context-aware-adr-map-pfd-knowledge-indicator.md", linked);
        Assert.Contains("docs/adr/0155-documentation-code-correspondence-and-architectural-drift.md", linked);
        Assert.DoesNotContain(linked, x => x.Contains("feature-archetype", StringComparison.OrdinalIgnoreCase));
    }
}


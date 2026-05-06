using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class KbBaseKnowledgeListMergerTests
{
    [Fact]
    public void Merge_OverlayWinsDuplicatePath()
    {
        const string overlay =
            """
            {
              "path": "/o",
              "files": [
                { "path": "a.md", "size_bytes": 1, "modified_utc": "2020-01-01T00:00:00Z" }
              ],
              "total": 1
            }
            """;
        const string embedded =
            """
            {
              "path": "/e",
              "files": [
                { "path": "a.md", "size_bytes": 999, "modified_utc": "1999-01-01T00:00:00Z" },
                { "path": "b.md", "size_bytes": 2, "modified_utc": "2020-01-02T00:00:00Z" }
              ],
              "total": 2
            }
            """;

        var merged = KbBaseKnowledgeListMerger.Merge(overlay, embedded, "hint");

        Assert.Contains("\"path\": \"hint\"", merged, StringComparison.Ordinal);
        Assert.Contains("\"a.md\"", merged, StringComparison.Ordinal);
        Assert.Contains("\"size_bytes\": 1", merged, StringComparison.Ordinal);
        Assert.Contains("\"b.md\"", merged, StringComparison.Ordinal);
        Assert.DoesNotContain("999", merged, StringComparison.Ordinal);
    }
}

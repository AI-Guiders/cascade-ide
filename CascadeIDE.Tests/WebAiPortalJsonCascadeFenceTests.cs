using CascadeIDE.Features.WebAiPortal.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WebAiPortalJsonCascadeFenceTests
{
    [Fact]
    public void TryExtractFirst_finds_single_block_trimmed()
    {
        const string md = """
            Explanation.

            ```json-cascade
            { "command_id": "codebase_index_search", "args": { "query": "x" } }
            ```

            Done.
            """;
        Assert.True(WebAiPortalJsonCascadeFence.TryExtractFirst(md, out var json));
        Assert.Equal("""{ "command_id": "codebase_index_search", "args": { "query": "x" } }""", json);
    }

    [Fact]
    public void TryExtractFirst_takes_first_of_two_blocks()
    {
        const string md = """
            ```json-cascade
            {"command_id":"a"}
            ```
            ```json-cascade
            {"command_id":"b"}
            ```
            """;
        Assert.True(WebAiPortalJsonCascadeFence.TryExtractFirst(md, out var json));
        Assert.Contains("\"a\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"b\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void TryExtractFirst_false_when_no_fence()
    {
        Assert.False(WebAiPortalJsonCascadeFence.TryExtractFirst("only ```json\n{}", out _));
        Assert.False(WebAiPortalJsonCascadeFence.TryExtractFirst(null, out _));
        Assert.False(WebAiPortalJsonCascadeFence.TryExtractFirst("", out _));
    }

    [Fact]
    public void TryExtractFirst_false_when_inner_empty()
    {
        Assert.False(WebAiPortalJsonCascadeFence.TryExtractFirst("```json-cascade\n```", out _));
    }
}

#nullable enable
using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassTestOutputParseTests
{
    const string Sample = """
        Passed  CascadeIDE.Tests.FooTest.Bar [12 ms]
        Failed  CascadeIDE.Tests.FooTest.Baz [3 ms]
          Error Message:
           Assert.Equal() Failure
        Passed  CascadeIDE.Tests.FooTest.Qux
        Failed!  - Failed:     1, Passed:     2, Skipped:     0, Total:     3
        """;

    [Fact]
    public void ParseFails_lists_failed_tests_with_message()
    {
        var rows = GlassTestOutputParse.ParseFails(Sample);
        Assert.Single(rows);
        Assert.Equal("CascadeIDE.Tests.FooTest.Baz", rows[0].Name);
        Assert.Contains("Assert.Equal", rows[0].Message);
        Assert.StartsWith("✗", rows[0].Display);
    }

    [Fact]
    public void ParseSummary_reads_pass_fail_counts()
    {
        var summary = GlassTestOutputParse.ParseSummary(Sample);
        Assert.Equal(3, summary.Total);
        Assert.Equal(2, summary.Passed);
        Assert.Equal(1, summary.Failed);
        Assert.False(summary.Success);
        Assert.Contains("2 passed", summary.Label);
    }

    [Fact]
    public void ParseFails_keeps_stack_path_for_jump()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "SoftFlBrokenTests.cs");
        File.WriteAllText(tmp, "// softfl jump fixture\n");
        try
        {
            var sample = $"""
                Failed  SoftFlTestFail.BrokenTests.FailsOnPurpose [3 ms]
                  Error Message:
                   softfl-tests-fail-jump
                  Stack Trace:
                     at SoftFlTestFail.BrokenTests.FailsOnPurpose() in {tmp}:line 10
                Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1
                """;
            var rows = GlassTestOutputParse.ParseFails(sample);
            Assert.Single(rows);
            Assert.Contains(".cs", rows[0].Message);
            Assert.True(GlassTestOutputParse.TryResolveFailJump(rows[0], workspaceRoot: null, out var path, out var line));
            Assert.Equal(tmp, path);
            Assert.Equal(10, line);
        }
        finally
        {
            File.Delete(tmp);
        }
    }
}

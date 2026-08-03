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
}

using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SourceLineMetricsTests
{
    [Fact]
    public void CountNonEmptyLines_skips_blank_and_whitespace_only()
    {
        const string text = "a\n\n  \t  \nb\r\n";
        Assert.Equal(2, SourceLineMetrics.CountNonEmptyLines(text));
    }
}

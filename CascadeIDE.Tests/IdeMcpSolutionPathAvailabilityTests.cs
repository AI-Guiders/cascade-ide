using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeMcpSolutionPathAvailabilityTests
{
    [Fact]
    public void IsRunnableSolutionFile_false_for_null_and_whitespace()
    {
        Assert.False(IdeMcpSolutionPathAvailability.IsRunnableSolutionFile(null));
        Assert.False(IdeMcpSolutionPathAvailability.IsRunnableSolutionFile(""));
        Assert.False(IdeMcpSolutionPathAvailability.IsRunnableSolutionFile("   "));
    }

    [Fact]
    public void IsRunnableSolutionFile_false_for_missing_file()
    {
        Assert.False(IdeMcpSolutionPathAvailability.IsRunnableSolutionFile(@"Z:\CascadeIDE\Tests\no-file-ever-831c.sln"));
    }

    [Fact]
    public void IdeMcpTestRunInstrumentationMutation_from_exception_opens_no_page()
    {
        var m = IdeMcpBuildTestOrchestrator.IdeMcpTestRunInstrumentationMutation.FromThrownException(
            existingInstrumentationOutput: "",
            exceptionMessage: "boom");
        Assert.False(m.ShouldOpenTestsPage);
        Assert.Equal(0, m.ImpactedTestsBadge);
        Assert.Contains("boom", m.UpdatedTestResultsOutput, StringComparison.Ordinal);
    }
}

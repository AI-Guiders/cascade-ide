using Xunit;

namespace SoftFlTestFail;

public sealed class BrokenTests
{
    [Fact]
    public void FailsOnPurpose()
    {
        Assert.True(false, "softfl-tests-fail-jump");
    }
}

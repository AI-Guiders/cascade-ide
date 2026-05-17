using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatTopicOverviewPolicyTests
{
    [Theory]
    [InlineData(1, -1, false, false)]
    [InlineData(2, -1, false, true)]
    [InlineData(1, 2, true, false)]
    [InlineData(2, 1, false, true)]
    [InlineData(3, 2, true, true)]
    public void ResolveNextOverviewMode_MatchesAdr0072(int threadCount, int lastCount, bool current, bool expected) =>
        Assert.Equal(expected, ChatTopicOverviewPolicy.ResolveNextOverviewMode(threadCount, lastCount, current));

    [Fact]
    public void ApplyAdaptiveDefault_UpdatesLastCountOnce()
    {
        var last = -1;
        var mode = false;
        ChatTopicOverviewPolicy.ApplyAdaptiveDefault(2, ref last, v => mode = v, () => mode);
        Assert.Equal(2, last);
        Assert.True(mode);

        ChatTopicOverviewPolicy.ApplyAdaptiveDefault(2, ref last, v => mode = v, () => mode);
        Assert.True(mode);
    }
}

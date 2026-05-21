using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatTopicOverviewPolicyTests
{
    [Fact]
    public void ResolveNextOverviewMode_FirstLoad_StartsInDetailWithTabs()
    {
        Assert.False(ChatTopicOverviewPolicy.ResolveNextOverviewMode(threadCount: 3, lastOverviewThreadCount: -1, currentOverviewMode: false));
    }

    [Fact]
    public void ResolveNextOverviewMode_SingleThread_StaysDetail()
    {
        Assert.False(ChatTopicOverviewPolicy.ResolveNextOverviewMode(threadCount: 1, lastOverviewThreadCount: -1, currentOverviewMode: false));
    }
}

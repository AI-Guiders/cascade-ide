using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SlashRouteCatalogIndexTests
{
    [Fact]
    public void IsKnownIntercomInnerVerb_TopicOpen_FromCatalog()
    {
        Assert.True(SlashRouteCatalogIndex.IsKnownIntercomInnerVerb("topic", "open"));
        Assert.False(SlashRouteCatalogIndex.IsKnownIntercomInnerVerb("topic", "not-a-verb"));
    }

    [Fact]
    public void RouteRequiresArgTail_FileOpen_HasCompletion()
    {
        Assert.True(SlashRouteCatalogIndex.RouteRequiresArgTail("/file open"));
    }

    [Fact]
    public void RouteRequiresArgTail_TopicCards_AutoRunWithoutArgs()
    {
        Assert.False(SlashRouteCatalogIndex.RouteRequiresArgTail("/intercom topic cards"));
    }

    [Fact]
    public void RouteRequiresArgTail_TopicOpen_AutoRunWithoutArgTail()
    {
        Assert.False(SlashRouteCatalogIndex.RouteRequiresArgTail("/intercom topic open"));
    }

    [Fact]
    public void GetArgTailKind_ServerStart_Optional()
    {
        Assert.Equal(SlashArgTailKind.Optional, SlashRouteCatalogIndex.GetArgTailKind("/intercom server start"));
    }

    [Fact]
    public void GetArgTailKind_ServerStatus_None()
    {
        Assert.Equal(SlashArgTailKind.None, SlashRouteCatalogIndex.GetArgTailKind("/intercom server status"));
    }
}

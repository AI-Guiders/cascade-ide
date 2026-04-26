using CascadeIDE.Cockpit.DataBus;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class DataBusEventPolicyLoaderTests
{
    [Fact]
    public void TryParse_maps_burst_and_reliable()
    {
        const string toml = """
[events]
FooEvent = "burst"
BarEvent = "reliable"
""";

        Assert.True(DataBusEventPolicyLoader.TryParse(toml, out var policy));
        Assert.True(policy.IsBurst(typeof(FooEvent)));
        Assert.False(policy.IsBurst(typeof(BarEvent)));
    }

    [Fact]
    public void Load_succeeds_from_embedded_resource()
    {
        var policy = DataBusEventPolicyLoader.Load();
        Assert.True(policy.IsBurst(typeof(DebugStateChanged)));
        Assert.True(policy.IsBurst(typeof(GitStateChanged)));
        Assert.True(policy.IsBurst(typeof(IdeHostStateChanged)));
        Assert.False(policy.IsBurst(typeof(BuildStateChanged)));
    }

    private struct FooEvent;
    private struct BarEvent;
}

using CascadeIDE.Cockpit.DataBus;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class InMemoryDataBusTests
{
    [Fact]
    public void Publish_sync_mode_delivers_immediately()
    {
        using var bus = new InMemoryDataBus(asynchronousDispatch: false);
        var delivered = false;
        using var sub = bus.Subscribe<BuildStateChanged>(evt => delivered = evt.IsBuilding);

        bus.Publish(new BuildStateChanged(true));

        Assert.True(delivered);
    }

    [Fact]
    public void Publish_sync_mode_isolates_faulty_subscriber()
    {
        using var bus = new InMemoryDataBus(asynchronousDispatch: false);
        var delivered = false;
        using var sub1 = bus.Subscribe<BuildStateChanged>(_ => throw new InvalidOperationException("boom"));
        using var sub2 = bus.Subscribe<BuildStateChanged>(evt => delivered = evt.IsBuilding);

        bus.Publish(new BuildStateChanged(true));

        Assert.True(delivered);
    }

    [Fact]
    public void Publish_async_mode_eventually_delivers()
    {
        using var bus = new InMemoryDataBus(asynchronousDispatch: true);
        var delivered = 0;
        using var sub = bus.Subscribe<BuildStateChanged>(evt =>
        {
            if (evt.IsBuilding)
                Interlocked.Exchange(ref delivered, 1);
        });

        bus.Publish(new BuildStateChanged(true));

        Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref delivered) == 1, millisecondsTimeout: 1000));
    }
}

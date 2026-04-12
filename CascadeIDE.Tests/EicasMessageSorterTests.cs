using System.Collections.ObjectModel;
using CascadeIDE.Cockpit.Channels.Eicas;
using CascadeIDE.Cockpit.Composition.Eicas;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class EicasMessageSorterTests
{
    [Fact]
    public void Rebuild_orders_warning_before_caution_before_advisory()
    {
        var target = new ObservableCollection<EicasMessage>();
        var a = new EicasMessage(EicasSeverity.Advisory, "a");
        var w = new EicasMessage(EicasSeverity.Warning, "w");
        var c = new EicasMessage(EicasSeverity.Caution, "c");

        EicasMessageSorter.Rebuild(target, [a, w, c]);

        Assert.Equal(3, target.Count);
        Assert.Equal(EicasSeverity.Warning, target[0].Severity);
        Assert.Equal(EicasSeverity.Caution, target[1].Severity);
        Assert.Equal(EicasSeverity.Advisory, target[2].Severity);
    }

    [Fact]
    public void Rebuild_same_severity_sorted_by_time_then_text()
    {
        var t0 = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var t1 = DateTimeOffset.Parse("2026-01-02T00:00:00Z");
        var target = new ObservableCollection<EicasMessage>();
        var m1 = new EicasMessage(EicasSeverity.Caution, "b", CreatedUtc: t1);
        var m0 = new EicasMessage(EicasSeverity.Caution, "a", CreatedUtc: t0);

        EicasMessageSorter.Rebuild(target, [m1, m0]);

        Assert.Equal("a", target[0].Text);
        Assert.Equal("b", target[1].Text);
    }
}

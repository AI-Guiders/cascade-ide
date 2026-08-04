#nullable enable

using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassIntercomHudTests
{
    [Fact]
    public void FormatHdgCrs_extracts_first_priority()
    {
        var body =
            "## operator_priority (SEALED)\n" +
            "1. Glass Done (human flight)\n" +
            "2. Citizen Done\n" +
            "Before act (not resume):\n" +
            "- Viewer?\n";
        Assert.Equal("HDG/CRS · Glass Done (human flight)", GlassIntercomHud.FormatHdgCrs(body));
    }

    [Fact]
    public void ParseIgniteJson_reads_korrys()
    {
        var snap = GlassIntercomHud.ParseIgniteJson(
            "{\"schema\":\"cide_ignite_latch/v1\",\"autonomous\":true,\"hild\":false,\"vad\":false,\"active\":true,\"course\":\"1. Intercom HUD\",\"pulse\":\"ignite · continuity · armed=1\"}");
        Assert.True(snap.Autoi);
        Assert.False(snap.Hild);
        Assert.Equal("HDG/CRS · Intercom HUD", snap.HdgCrs);
    }

    [Fact]
    public void ToggleOp_flips_autoi_and_hild()
    {
        Assert.Equal("autonomous_on", GlassIntercomHud.ToggleOp("AUTOI", currentlyOn: false));
        Assert.Equal("autonomous_off", GlassIntercomHud.ToggleOp("autoi", currentlyOn: true));
        Assert.Equal("hild_on", GlassIntercomHud.ToggleOp("hild", currentlyOn: false));
        Assert.Equal("", GlassIntercomHud.ToggleOp("vad", currentlyOn: false));
    }
}

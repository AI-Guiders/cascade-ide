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
        Assert.Equal("fly", snap.Mode);
        Assert.Equal("AUTOI", snap.AutoiLabel);
        Assert.Equal("HDG/CRS · Intercom HUD", snap.HdgCrs);
    }

    [Fact]
    public void ParseIgniteJson_talk_forces_autoi_off()
    {
        var snap = GlassIntercomHud.ParseIgniteJson(
            "{\"schema\":\"cide_ignite_latch/v1\",\"autonomous\":true,\"await_partner\":true,\"mode\":\"talk\",\"active\":true,\"pulse\":\"ignite · continuity · awaiting_partner\"}");
        Assert.False(snap.Autoi);
        Assert.True(snap.AwaitPartner);
        Assert.Equal("talk", snap.Mode);
        Assert.Equal("TALK", snap.AutoiLabel);
        Assert.Equal("HDG/CRS · TALK · Autoi OFF", snap.HdgCrs);
    }

    [Fact]
    public void ParseIgniteJson_halt_label()
    {
        var snap = GlassIntercomHud.ParseIgniteJson(
            "{\"schema\":\"cide_ignite_latch/v1\",\"autonomous\":false,\"await_partner\":true,\"mode\":\"halt\",\"active\":true}");
        Assert.False(snap.Autoi);
        Assert.Equal("HALT", snap.AutoiLabel);
        Assert.Equal("HDG/CRS · HALT · Autoi OFF", snap.HdgCrs);
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

#nullable enable

using CascadeIDE.Intercom;
using CascadeIDE.SoftInstrument;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassRadioPointerTests
{
    [Fact]
    public void FromBody_peels_delta_arrow_as_instrument_card()
    {
        var peel = GlassRadioPointer.FromBody(
            "На PFD: режим без SolutionExplorer.\n" +
            "delta → Right:Editor · verify on Applies\n" +
            "короткий ack.");

        Assert.Equal("На PFD: режим без SolutionExplorer.\nкороткий ack.", peel.Body);
        Assert.Single(peel.Pointers);
        Assert.Equal(new GlassGlanceChip("DELTA", "Right:Editor · verify on Applies", "ok"), peel.Pointers[0]);
    }

    [Fact]
    public void FromBody_peels_bare_arrow_zones()
    {
        var peel = GlassRadioPointer.FromBody("→ MFD:Problems\n→ PFD.NEXT\nprose stays");

        Assert.Equal("prose stays", peel.Body);
        Assert.Equal(2, peel.Pointers.Count);
        Assert.Equal("MFD", peel.Pointers[0].Label);
        Assert.Equal("PFD", peel.Pointers[1].Label);
    }

    [Fact]
    public void FromBody_keeps_generic_arrow_as_prose()
    {
        var peel = GlassRadioPointer.FromBody("→ random bullet\n→ Current TM leaf");
        Assert.Equal("→ random bullet\n→ Current TM leaf", peel.Body);
        Assert.Empty(peel.Pointers);
    }

    [Fact]
    public void TryPeelLine_rejects_plain_prose()
    {
        Assert.False(GlassRadioPointer.TryPeelLine("shipped tint on glass", out _));
        Assert.False(GlassRadioPointer.TryPeelLine("delta", out _));
    }
}

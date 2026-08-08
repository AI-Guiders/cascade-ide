#nullable enable
using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SoftOrganFaceHandbookTests
{
    [Fact]
    public void ChipsFor_qrh_is_card_deck_not_wall()
    {
        var chips = SoftOrganFaceHandbook.ChipsFor("qrh");
        Assert.Equal(new GlassGlanceChip("LEVEL", "QRH", "ok"), chips[0]);
        Assert.Contains(chips, c => c.Label == "intake-brief");
        Assert.True(chips.Count >= 5);
    }

    [Fact]
    public void ChipsFor_filter_matches_id_or_use()
    {
        var chips = SoftOrganFaceHandbook.ChipsFor("ecl", "remount");
        Assert.Contains(chips, c => c.Label == "not-connected");
        Assert.DoesNotContain(chips, c => c.Label == "composer-stop");
    }

    [Fact]
    public void MfdPageFor_maps_soft_organs()
    {
        Assert.Equal("QRH", SoftOrganFaceHandbook.MfdPageFor("qrh"));
        Assert.Equal("ECL", SoftOrganFaceHandbook.MfdPageFor("ecl"));
        Assert.Equal("Alert", SoftOrganFaceHandbook.MfdPageFor("alert"));
        Assert.True(SoftOrganFaceHandbook.IsSoftOrganGlancePage("QRH"));
    }
}

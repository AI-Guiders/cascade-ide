using CascadeIDE.Features.UiChrome;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class AttentionZoneTests
{
    [Theory]
    [InlineData("forward", AttentionZone.Forward)]
    [InlineData("pfd", AttentionZone.Pfd)]
    [InlineData("mfd", AttentionZone.Mfd)]
    [InlineData("eicas", AttentionZone.Eicas)]
    [InlineData("hud", AttentionZone.Hud)]
    public void TryParseCanonicalId_accepts_adr_tokens(string raw, AttentionZone expected)
    {
        Assert.True(AttentionZoneExtensions.TryParseCanonicalId(raw, out var z));
        Assert.Equal(expected, z);
        Assert.Equal(raw, z.ToCanonicalId());
    }

    [Fact]
    public void TryParseCanonicalId_rejects_null_empty_and_garbage()
    {
        Assert.False(AttentionZoneExtensions.TryParseCanonicalId(null, out _));
        Assert.False(AttentionZoneExtensions.TryParseCanonicalId("", out _));
        Assert.False(AttentionZoneExtensions.TryParseCanonicalId("Forward", out _));
        Assert.False(AttentionZoneExtensions.TryParseCanonicalId("PFD", out _));
        Assert.False(AttentionZoneExtensions.TryParseCanonicalId(" primary ", out _));
    }

    [Fact]
    public void All_lists_five_ids_in_lockstep_with_enum()
    {
        Assert.Equal(5, AttentionZoneIds.All.Length);
        foreach (var id in AttentionZoneIds.All)
        {
            Assert.True(AttentionZoneExtensions.TryParseCanonicalId(id, out var z));
            Assert.Equal(id, z.ToCanonicalId());
        }
    }

    [Fact]
    public void Spatial_anchor_flags_match_adr()
    {
        Assert.True(AttentionZone.Forward.IsSpatialAnchor());
        Assert.True(AttentionZone.Pfd.IsSpatialAnchor());
        Assert.True(AttentionZone.Mfd.IsSpatialAnchor());
        Assert.False(AttentionZone.Eicas.IsSpatialAnchor());
        Assert.False(AttentionZone.Hud.IsSpatialAnchor());
    }

    [Fact]
    public void Eicas_and_Hud_classifiers()
    {
        Assert.True(AttentionZone.Eicas.IsAlertingChannel());
        Assert.False(AttentionZone.Pfd.IsAlertingChannel());

        Assert.True(AttentionZone.Hud.IsHudLayer());
        Assert.False(AttentionZone.Forward.IsHudLayer());
    }
}

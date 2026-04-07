using CascadeIDE.Features.UiChrome;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class AttentionZoneContainerTests
{
    [Fact]
    public void Zone_sets_canonical_id_and_css_classes()
    {
        var c = new AttentionZoneContainer { Zone = AttentionZone.Pfd };
        Assert.Equal("pfd", c.CanonicalZoneId);
        Assert.Equal(AttentionZoneIds.Pfd, c.Zone.ToCanonicalId());
        Assert.Contains("attention-zone", c.Classes);
        Assert.Contains("attention-zone-pfd", c.Classes);
    }

    [Fact]
    public void Frontal_zone_uses_frontal_class()
    {
        var c = new AttentionZoneContainer { Zone = AttentionZone.Frontal };
        Assert.Contains("attention-zone-frontal", c.Classes);
    }

    [Fact]
    public void Eicas_uses_channel_class_not_spatial_zone_prefix()
    {
        var c = new AttentionZoneContainer { Zone = AttentionZone.Eicas };
        Assert.Equal("eicas", c.CanonicalZoneId);
        Assert.Contains("attention-channel-eicas", c.Classes);
        Assert.DoesNotContain("attention-zone", c.Classes);
    }

    [Fact]
    public void Hud_uses_layer_class_not_spatial_zone_prefix()
    {
        var c = new AttentionZoneContainer { Zone = AttentionZone.Hud };
        Assert.Equal("hud", c.CanonicalZoneId);
        Assert.Contains("attention-layer-hud", c.Classes);
        Assert.DoesNotContain("attention-zone", c.Classes);
    }
}

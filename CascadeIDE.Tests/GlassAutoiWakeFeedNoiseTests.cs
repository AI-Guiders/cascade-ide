#nullable enable

using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassAutoiWakeFeedNoiseTests
{
    const string Charge =
        "Resume the current authorized local development task from Task Manager. Habitat=CDP.\n"
        + "---\nIf you feel completely lost / thread amnesia: compaction likely happened.\n"
        + "cdp_pressure op=recall";

    [Fact]
    public void Body_charge_is_feed_noise() =>
        Assert.True(GlassAutoiWakeFeed.IsNoise(Charge));

    [Fact]
    public void AutoI_name_is_feed_noise_even_without_body_markers() =>
        Assert.True(GlassAutoiWakeFeed.IsNoise("timer fired", name: "AutoI", kind: "guest"));

    [Fact]
    public void Wake_kind_is_feed_noise() =>
        Assert.True(GlassAutoiWakeFeed.IsNoise("short", kind: "wake"));

    [Fact]
    public void Ordinary_guest_chat_is_not_noise() =>
        Assert.False(GlassAutoiWakeFeed.IsNoise("shipped tint", name: "Кир", kind: "guest"));

    [Fact]
    public void Kir_voice_cannon_face_tip_not_noise_even_as_AutoI()
    {
        const string tip = "Radio · Composer Stop · @Kir wake pending (пушка ждёт Voice)";
        Assert.True(GlassAutoiWakeFeed.IsKirVoiceCannonFaceTip(tip));
        Assert.False(GlassAutoiWakeFeed.IsNoise(tip, name: "AutoI", kind: "guest"));
    }

    [Fact]
    public void Kir_voice_cannon_fail_tip_not_noise()
    {
        const string tip = "Radio · @Kir wake fail · click_failed";
        Assert.True(GlassAutoiWakeFeed.IsKirVoiceCannonFaceTip(tip));
        Assert.False(GlassAutoiWakeFeed.IsNoise(tip, name: "AutoI", kind: "guest"));
    }

    [Fact]
    public void Autoi_radio_pointer_misattributed_as_citizen_is_noise() =>
        Assert.True(GlassAutoiWakeFeed.IsNoise(
            "Autoi \u00B7 remount\n\u2192 PFD.NEXT\ndelta \u2192 Plan \u00B7 remount-initialized",
            name: "Citizen",
            kind: "citizen"));
}

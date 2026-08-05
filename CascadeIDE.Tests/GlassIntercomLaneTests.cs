#nullable enable

using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassIntercomLaneTests
{
    [Fact]
    public void Parse_codes_and_legacy_labels()
    {
        Assert.Equal(GlassIntercomLane.Kind.Cit, GlassIntercomLane.Parse("cit"));
        Assert.Equal(GlassIntercomLane.Kind.Host, GlassIntercomLane.Parse("composer"));
        Assert.Equal(GlassIntercomLane.Kind.Pf, GlassIntercomLane.Parse("habitat"));
        Assert.Equal(GlassIntercomLane.Kind.Cit, GlassIntercomLane.FromLegacyModelChoice("Citizen · default"));
        Assert.Equal(GlassIntercomLane.Kind.Host, GlassIntercomLane.FromLegacyModelChoice("Composer · host"));
        Assert.Equal(GlassIntercomLane.Kind.Pf, GlassIntercomLane.FromLegacyModelChoice("PF · habitat"));
    }

    [Fact]
    public void ModelAxisLit_only_for_cit()
    {
        Assert.True(GlassIntercomLane.ModelAxisLit(GlassIntercomLane.Kind.Cit));
        Assert.False(GlassIntercomLane.ModelAxisLit(GlassIntercomLane.Kind.Host));
        Assert.False(GlassIntercomLane.ModelAxisLit(GlassIntercomLane.Kind.Pf));
    }

    [Fact]
    public void ParseLatchJson_migrates_legacy_model_file()
    {
        var snap = GlassIntercomLane.ParseLatchJson("""{"model":"Composer · host","stamped_utc":"2026-08-05T00:00:00Z"}""");
        Assert.Equal(GlassIntercomLane.Kind.Host, snap.Lane);
        Assert.Null(snap.ModelId);
    }

    [Fact]
    public void ParseLatchJson_reads_lane_and_model_id()
    {
        var snap = GlassIntercomLane.ParseLatchJson("""{"schema":"glass_intercom_lane/v0","lane":"cit","model_id":"glm-4","stamped_utc":"2026-08-05T00:00:00Z"}""");
        Assert.Equal(GlassIntercomLane.Kind.Cit, snap.Lane);
        Assert.Equal("glm-4", snap.ModelId);
    }

    [Fact]
    public void FormatLatchJson_roundtrips()
    {
        var json = GlassIntercomLane.FormatLatchJson(GlassIntercomLane.Kind.Cit, "glm-4", DateTimeOffset.Parse("2026-08-05T00:00:00Z"));
        var snap = GlassIntercomLane.ParseLatchJson(json);
        Assert.Equal(GlassIntercomLane.Kind.Cit, snap.Lane);
        Assert.Equal("glm-4", snap.ModelId);
    }

    [Fact]
    public void IsComposerPlaceholder_covers_lane_hints()
    {
        Assert.True(GlassIntercomLane.IsComposerPlaceholder("Message @PF…"));
        Assert.True(GlassIntercomLane.IsComposerPlaceholder("Message @CIT…"));
        Assert.True(GlassIntercomLane.IsComposerPlaceholder("Message @HOST…"));
        Assert.False(GlassIntercomLane.IsComposerPlaceholder("hello"));
    }
}

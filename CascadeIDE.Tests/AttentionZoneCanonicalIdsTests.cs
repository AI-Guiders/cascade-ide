using System.Text.Json;
using CascadeIDE.Contracts.Experimental;
using CascadeIDE.Contracts.Experimental.Capabilities;
using CascadeIDE.Features.UiChrome;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class AttentionZoneCanonicalIdsTests
{
    [Fact]
    public void AttentionZoneIds_strings_match_contracts_single_source()
    {
        Assert.Equal(AttentionZoneCanonicalIds.Forward, AttentionZoneIds.Forward);
        Assert.Equal(AttentionZoneCanonicalIds.Pfd, AttentionZoneIds.Pfd);
        Assert.Equal(AttentionZoneCanonicalIds.Mfd, AttentionZoneIds.Mfd);
        Assert.Equal(AttentionZoneCanonicalIds.Eicas, AttentionZoneIds.Eicas);
        Assert.Equal(AttentionZoneCanonicalIds.Hud, AttentionZoneIds.Hud);
        Assert.Equal(AttentionZoneIds.All.Length, AttentionZoneCanonicalIds.All.Length);
    }

    [Theory]
    [InlineData("forward", true)]
    [InlineData("pfd", true)]
    [InlineData("mfd", true)]
    [InlineData("eicas", true)]
    [InlineData("hud", true)]
    [InlineData("Forward", false)]
    [InlineData("PFD", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsKnownCanonicalId_is_strict(string? id, bool expected) =>
        Assert.Equal(expected, AttentionZoneCanonicalIds.IsKnownCanonicalId(id));

    [Fact]
    public void Command_descriptor_JSON_round_trips_PrimaryAttentionZoneId()
    {
        var d = new CommandCapabilityDescriptor
        {
            Id = "test.cmd",
            OwnerModuleId = "test.module",
            Title = "T",
            PrimaryAttentionZoneId = AttentionZoneCanonicalIds.Mfd
        };
        var json = JsonSerializer.Serialize(d);
        var back = JsonSerializer.Deserialize<CommandCapabilityDescriptor>(json);
        Assert.NotNull(back);
        Assert.Equal(AttentionZoneCanonicalIds.Mfd, back.PrimaryAttentionZoneId);
    }

    [Fact]
    public void Ui_surface_descriptor_JSON_round_trips_zone_and_panel()
    {
        var d = new UiSurfaceCapabilityDescriptor
        {
            Id = "ui.x",
            OwnerModuleId = "m",
            DisplayName = "X",
            PrimaryAttentionZoneId = AttentionZoneCanonicalIds.Pfd,
            HostAttentionPanelId = AttentionPanelCanonicalIds.SolutionExplorer
        };
        var json = JsonSerializer.Serialize(d);
        var back = JsonSerializer.Deserialize<UiSurfaceCapabilityDescriptor>(json);
        Assert.NotNull(back);
        Assert.Equal(AttentionZoneCanonicalIds.Pfd, back.PrimaryAttentionZoneId);
        Assert.Equal(AttentionPanelCanonicalIds.SolutionExplorer, back.HostAttentionPanelId);
    }
}

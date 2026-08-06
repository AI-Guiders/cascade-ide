#nullable enable

using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassIntercomContactsTests
{
    [Fact]
    public void DefaultRoster_equal_standing_humans_and_agents()
    {
        var roster = GlassIntercomContacts.DefaultRoster("Sveta", "AutoI");
        Assert.Equal(3, roster.Count);
        Assert.Equal("operator", roster[0].Id);
        Assert.Equal(GlassIntercomContacts.Standing.Human, roster[0].Standing);
        Assert.Equal("Sveta · human", roster[0].Line);
        Assert.Equal(GlassIntercomContacts.Standing.Agent, roster[1].Standing);
        Assert.Equal("AutoI · agent", roster[1].Line);
        var collide = GlassIntercomContacts.DefaultRoster("Op", "Citizen");
        Assert.Equal("Кир · agent", collide[1].Line);
        Assert.Equal("Citizen · agent", collide[2].Line);
        Assert.Equal("citizen", roster[2].Id);
        Assert.Equal("Citizen · agent", roster[2].Line);
    }

    [Fact]
    public void FormatLatchJson_roundtrips_selected()
    {
        var roster = GlassIntercomContacts.DefaultRoster();
        var json = GlassIntercomContacts.FormatLatchJson(
            "citizen",
            roster,
            DateTimeOffset.Parse("2026-08-06T00:00:00Z"));
        var snap = GlassIntercomContacts.ParseLatchJson(json);
        Assert.Equal("citizen", snap.SelectedId);
        Assert.Equal(3, snap.Roster.Count);
        Assert.Equal("Citizen", GlassIntercomContacts.Find(snap.Roster, snap.SelectedId)!.Value.Display);
    }

    [Fact]
    public void ParseLatchJson_falls_back_when_selected_missing()
    {
        var json = """
            {"schema":"glass_intercom_contacts/v0","selected_id":"ghost","contacts":[{"id":"operator","display":"Op","standing":"human"}]}
            """;
        var snap = GlassIntercomContacts.ParseLatchJson(json);
        Assert.Equal("operator", snap.SelectedId);
        Assert.Single(snap.Roster);
    }

    [Fact]
    public void ParseStanding_aliases()
    {
        Assert.Equal(GlassIntercomContacts.Standing.Human, GlassIntercomContacts.ParseStanding("operator"));
        Assert.Equal(GlassIntercomContacts.Standing.Agent, GlassIntercomContacts.ParseStanding("citizen"));
    }
}

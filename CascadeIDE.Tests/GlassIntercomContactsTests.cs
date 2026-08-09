#nullable enable

using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassIntercomContactsTests
{
    [Fact]
    public void DefaultRoster_operator_plus_face_only()
    {
        var roster = GlassIntercomContacts.DefaultRoster("Sveta", "AutoI");
        Assert.Equal(2, roster.Count);
        Assert.Equal("operator", roster[0].Id);
        Assert.Equal(GlassIntercomContacts.Standing.Human, roster[0].Standing);
        Assert.Equal("Sveta · human", roster[0].Line);
        Assert.Equal("citizen", roster[1].Id);
        Assert.Equal(GlassIntercomContacts.Standing.Agent, roster[1].Standing);
        Assert.Equal("Citizen · agent", roster[1].Line);

        // partnerDisplay ignored — tip ≠ DM row
        var ignorePartner = GlassIntercomContacts.DefaultRoster("Op", "Citizen");
        Assert.Equal(2, ignorePartner.Count);
        Assert.Equal("Citizen · agent", ignorePartner[1].Line);

        var named = GlassIntercomContacts.DefaultRoster("Света", partnerDisplay: "Кир", citizenDisplay: "Sierra");
        Assert.Equal("Sierra · agent", named[1].Line);
    }

    [Fact]
    public void ResolveSelectedId_migrates_partner_to_citizen()
    {
        var roster = GlassIntercomContacts.DefaultRoster("Света", citizenDisplay: "Sierra");
        Assert.Equal("citizen", GlassIntercomContacts.ResolveSelectedId(roster, "partner"));
    }

    [Fact]
    public void ParseLatchJson_with_roster_ignores_poisoned_contacts()
    {
        var live = GlassIntercomContacts.DefaultRoster("Света", citizenDisplay: "Sierra");
        var json = """
            {"schema":"glass_intercom_contacts/v0","selected_id":"partner","contacts":[{"id":"partner","display":"Citizen","standing":"agent"},{"id":"citizen","display":"Citizen","standing":"agent"}]}
            """;
        var snap = GlassIntercomContacts.ParseLatchJson(json, live);
        Assert.Equal(2, snap.Roster.Count);
        Assert.Equal("Sierra", snap.Roster[1].Display);
        Assert.Equal("citizen", snap.SelectedId);
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
        Assert.Equal(2, snap.Roster.Count);
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

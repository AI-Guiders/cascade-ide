#nullable enable
using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SoftOrganFaceHandbookTests
{
    [Fact]
    public void ChipsFor_qrh_is_situations_not_wall()
    {
        var chips = SoftOrganFaceHandbook.ChipsFor("qrh");
        Assert.Contains(chips, c => c.Label == "cabin-start");
        Assert.Contains(chips, c => c.Value.Contains("Кабина", StringComparison.Ordinal));
        Assert.True(chips.Count >= 3);
    }

    [Fact]
    public void ChipsFor_filter_matches_title_or_when()
    {
        var chips = SoftOrganFaceHandbook.ChipsFor("ecl", "connected");
        Assert.Contains(chips, c => c.Label == "not-connected");
        Assert.DoesNotContain(chips, c => c.Label == "hard-deploy");
    }

    [Fact]
    public void MfdPageFor_maps_soft_organs_and_here()
    {
        Assert.Equal("QRH", SoftOrganFaceHandbook.MfdPageFor("qrh"));
        Assert.Equal("ECL", SoftOrganFaceHandbook.MfdPageFor("ecl"));
        Assert.Equal("Alert", SoftOrganFaceHandbook.MfdPageFor("alert"));
        Assert.Equal("HereNext", SoftOrganFaceHandbook.MfdPageFor("here"));
        Assert.True(SoftOrganFaceHandbook.IsSoftOrganGlancePage("QRH"));
        Assert.True(SoftOrganFaceHandbook.IsSoftOrganGlancePage("HereNext"));
    }
}

public sealed class OperatorSituationCatalogTests
{
    [Fact]
    public void PickHere_without_project_opens_open_project()
    {
        var s = OperatorSituationCatalog.PickHere(new OperatorHereLocus(
            CabinUp: true, WorkspaceRoot: null, HasProjectSignals: false, EditorPath: null, MfdPage: "HereNext"));
        Assert.Equal("open-project", s.Id);
        Assert.True(s.Steps.Count >= 2);
    }

    [Fact]
    public void FormatHereLine_includes_project_and_file()
    {
        var line = OperatorSituationCatalog.FormatHereLine(new OperatorHereLocus(
            true, @"D:\work\CascadeIDE", true, @"D:\work\CascadeIDE\MainWindow.cs", "HereNext"));
        Assert.Contains("HERE ·", line, StringComparison.Ordinal);
        Assert.Contains("CascadeIDE", line, StringComparison.Ordinal);
        Assert.Contains("MainWindow.cs", line, StringComparison.Ordinal);
        Assert.Contains("HereNext", line, StringComparison.Ordinal);
    }

    [Fact]
    public void Situation_has_ordered_next_steps()
    {
        var cabin = OperatorSituationCatalog.Find("cabin-start");
        Assert.NotNull(cabin);
        Assert.Equal("когда: окно Glass открыто, ещё не ясно что делать", cabin!.When);
        Assert.Contains(cabin.Steps, st => st.Text.Contains("HERE/NEXT", StringComparison.Ordinal));
    }
}

using System.Text.Json;
using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public class GlassAttachChipPeelTests
{
    [Fact]
    public void FromBody_peels_path_line_and_range()
    {
        var chips = GlassAttachChipPeel.FromBody(
            "see [LatchPaint.cs:40] and [MainWindow.xaml:10-12] please");
        Assert.Equal(2, chips.Count);
        Assert.Equal("LatchPaint.cs:40", chips[0].Label);
        Assert.Equal(40, chips[0].LineStart);
        Assert.Equal("MainWindow.xaml:10-12", chips[1].Label);
        Assert.Equal(10, chips[1].LineStart);
        Assert.Equal(12, chips[1].LineEnd);
    }

    [Fact]
    public void FromBody_skips_member_markers()
    {
        var chips = GlassAttachChipPeel.FromBody("scope [M:Foo] only");
        Assert.Empty(chips);
    }

    [Fact]
    public void Peel_merges_json_attachments_with_body()
    {
        using var doc = JsonDocument.Parse("""
            [{"file":"Services/A.cs","line_start":3,"display_label":"A › L3"},{"file":"B.cs"}]
            """);
        var chips = GlassAttachChipPeel.Peel("also [B.cs:9]", doc.RootElement);
        Assert.Equal(3, chips.Count);
        Assert.Equal("A › L3", chips[0].Label);
        Assert.Equal("B.cs", chips[1].Label);
        Assert.Equal("B.cs:9", chips[2].Label);
    }

    [Fact]
    public void FormatBracket_roundtrips()
    {
        Assert.Equal("[a/b.cs:2-5]", GlassAttachChipPeel.FormatBracket("a\\b.cs", 2, 5));
        Assert.Equal("[x.cs:1]", GlassAttachChipPeel.FormatBracket("x.cs", 1));
    }

    [Fact]
    public void StripBracketsForDisplay_keeps_prose_drops_path_chips()
    {
        var body = GlassAttachChipPeel.StripBracketsForDisplay(
            "see [LatchPaint.cs:40] and [M:Foo] please");
        Assert.Contains("see", body);
        Assert.Contains("[M:Foo]", body);
        Assert.DoesNotContain("LatchPaint", body);
        Assert.Contains("please", body);
    }

    [Fact]
    public void ResolveAgainstDisk_marks_missing()
    {
        var chip = new GlassAttachChip("Nope.cs", "Nope.cs", 1);
        var resolved = GlassAttachChipPeel.ResolveAgainstDisk(chip, workspaceRoot: null);
        Assert.False(resolved.Resolved);
    }

    [Fact]
    public void FromBody_skips_telegram_desktop_timestamps()
    {
        var body =
            "Sierra, [06.08.2026 7:32]\n" +
            "hi\n" +
            "Operator, [06.08.2026 7:33]\n" +
            "and also [LatchPaint.cs:40]";
        var chips = GlassAttachChipPeel.FromBody(body);
        Assert.Single(chips);
        Assert.Equal("LatchPaint.cs:40", chips[0].Label);

        var stripped = GlassAttachChipPeel.StripBracketsForDisplay(body);
        Assert.Contains("[06.08.2026 7:32]", stripped);
        Assert.Contains("[06.08.2026 7:33]", stripped);
        Assert.DoesNotContain("LatchPaint", stripped);
    }
}

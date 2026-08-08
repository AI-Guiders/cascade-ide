using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassEditorFaceTests
{
    [Theory]
    [InlineData("Editor", true)]
    [InlineData("editor", true)]
    [InlineData("Git", false)]
    [InlineData(null, false)]
    public void PreferEditorHost_only_Editor(string? page, bool expect) =>
        Assert.Equal(expect, GlassEditorFace.PreferEditorHost(page));

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void PreferParkOnMfdWhenReleased_follows_ADR0120(bool intercomForward, bool expect) =>
        Assert.Equal(expect, GlassEditorFace.PreferParkOnMfdWhenReleased(intercomForward));

    [Fact]
    public void SoftOrgan_does_not_claim_Editor_for_peel_overlay()
    {
        Assert.Null(SoftOrganMfdGlance.TryOrganIdForMfdPage(GlassEditorFace.MfdPage));
    }

    [Fact]
    public void MfdBody_source_does_not_route_Editor_to_Avalonia_FormatMfdStub()
    {
        // Regression: F=editor left AvalonEdit on Forward → FormatMfdStub("Editor", "on Forward").
        var path = FindRepoFile("CDP.GlassCockpit.Windows", "MainWindow.MfdBody.cs");
        Assert.True(File.Exists(path), path);
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("\"Editor\" => FormatMfdStub", text, StringComparison.Ordinal);
        Assert.Contains("PreferEditorHost", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EditorSurface_source_mounts_Editor_Face_on_Mfd_not_Parent_gate_alone()
    {
        var path = FindRepoFile("CDP.GlassCockpit.Windows", "MainWindow.EditorSurface.cs");
        Assert.True(File.Exists(path), path);
        var text = File.ReadAllText(path);
        Assert.Contains("GlassEditorFace.PreferEditorHost", text, StringComparison.Ordinal);
        Assert.Contains("MountEditor", text, StringComparison.Ordinal);
    }

    static string FindRepoFile(string folder, string file)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, folder, file);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return Path.Combine("..", "..", "..", "..", folder, file);
    }
}

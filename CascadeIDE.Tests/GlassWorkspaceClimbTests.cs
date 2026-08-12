#nullable enable
using CascadeIDE.SoftInstrument;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassWorkspaceClimbTests
{
    [Fact]
    public void Score_git_and_sln_beats_thin_cascade_ide_alone()
    {
        var root = Path.Combine(Path.GetTempPath(), "glass-climb-" + Guid.NewGuid().ToString("N"));
        var thin = Path.Combine(root, "thin");
        var strong = Path.Combine(root, "strong");
        try
        {
            Directory.CreateDirectory(Path.Combine(thin, ".cascade-ide"));
            Directory.CreateDirectory(Path.Combine(strong, ".git"));
            File.WriteAllText(Path.Combine(strong, "CascadeIDE.sln"), "Microsoft Visual Studio Solution File");

            Assert.True(GlassWorkspaceClimb.Score(strong) > GlassWorkspaceClimb.Score(thin));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* temp */ }
        }
    }
}

#nullable enable
using System.Text.RegularExpressions;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// GlassCore links host sources via Compile Include ..\… — CascadeIDE must
/// Compile Remove the same paths (single compile). Peel12 found peel7–9 dual-compile.
/// </summary>
public sealed class GlassCoreLinkRemoveParityTests
{
    static readonly Regex LinkedFromParent = new(
        @"Compile Include=""\.\.\\(?<rel>[^""]+)""",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    [Fact]
    public void Every_GlassCore_linked_source_is_Compile_Remove_on_CascadeIDE_host()
    {
        var root = RepoRoot();
        var glassProj = Path.Combine(root, "CascadeIDE.GlassCore", "CascadeIDE.GlassCore.csproj");
        var hostProj = Path.Combine(root, "CascadeIDE.csproj");
        Assert.True(File.Exists(glassProj), glassProj);
        Assert.True(File.Exists(hostProj), hostProj);

        var glass = File.ReadAllText(glassProj);
        var host = File.ReadAllText(hostProj);

        var linked = LinkedFromParent.Matches(glass)
            .Select(m => m.Groups["rel"].Value.Replace('/', '\\'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.NotEmpty(linked);

        var missing = linked
            .Where(rel => !host.Contains("Compile Remove=\"" + rel + "\"", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(
            missing.Length == 0,
            "Host missing Compile Remove for GlassCore-linked sources:\n" + string.Join("\n", missing));
    }

    static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "CascadeIDE.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("CascadeIDE.sln not found from test output path.");
    }
}

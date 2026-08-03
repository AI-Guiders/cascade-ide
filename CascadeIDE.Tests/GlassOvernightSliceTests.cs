#nullable enable

using CascadeIDE.Intercom;
using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassChordMelodyTests
{
    [Fact]
    public void NormalizeInput_keeps_parametric_separators()
    {
        Assert.Equal("els:10:20", GlassChordMelody.NormalizeInput("ELS:10:20"));
        Assert.Equal("wai:example.com", GlassChordMelody.NormalizeInput("WAI:Example.com"));
    }

    [Fact]
    public void FilterSuggestions_prefix_matches_intent_catalog()
    {
        var rows = GlassChordMelody.FilterSuggestions("gs");
        Assert.Contains(rows, r => r.Alias == "gs");
    }

    [Fact]
    public void Parametric_select_and_webai_resolve()
    {
        Assert.True(GlassChordMelody.TryResolveParametricSelect("els:10:20", out var a, out var b) && a == 10 && b == 20);
        Assert.True(GlassChordMelody.TryResolveParametricWebAi("wai:chat.example", out var url) && url == "chat.example");
    }

    [Fact]
    public void IsParametricTailPrefix_for_els_and_wai()
    {
        Assert.True(GlassChordMelody.IsParametricTailPrefix("els"));
        Assert.True(GlassChordMelody.IsParametricTailPrefix("wai:foo"));
        Assert.False(GlassChordMelody.IsParametricTailPrefix("gs"));
    }
}

public sealed class GlassRoslynDiagnosticsFeedTests
{
    [Fact]
    public void CollectForFile_syntax_error_on_bad_cs()
    {
        var path = Path.Combine(Path.GetTempPath(), "glass-roslyn-" + Guid.NewGuid().ToString("N") + ".cs");
        var rows = GlassRoslynDiagnosticsFeed.CollectForFile(path, "class {");
        Assert.NotEmpty(rows);
        Assert.All(rows, r => Assert.Contains(r.Severity, new[] { "error", "warning" }));
    }
}

public sealed class GlassHybridIndexStatusProbeTests
{
    [Fact]
    public void TryFetchStatusJson_returns_json_for_temp_workspace()
    {
        var root = Path.Combine(Path.GetTempPath(), "glass-hci-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var json = GlassHybridIndexStatusProbe.TryFetchStatusJson(root);
            Assert.False(string.IsNullOrWhiteSpace(json));
            Assert.Contains("databaseExists", json!, StringComparison.Ordinal);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }
}

#nullable enable

namespace CascadeIDE.SoftInstrument;

/// <summary>
/// File-situ Applies on locus (Q2): diags/tests scoped to the open file — human-face summary, not Problems dump.
/// Hop to MFD Problems / Tests for the list.
/// </summary>
public static class GlassEditorAppliesLocus
{
    public sealed record Face(
        string Line,
        int Errors,
        int Warnings,
        int TestFails,
        IReadOnlyList<int> ErrorLines,
        IReadOnlyList<int> WarnLines,
        bool Clean)
    {
        public bool HasTint => ErrorLines.Count > 0 || WarnLines.Count > 0;
    }

    public static Face Collect(
        string? editorPath,
        string? sourceText = null,
        IReadOnlyList<GlassProblemItem>? buildProblems = null,
        IReadOnlyList<GlassTestOutputParse.FailRow>? testFails = null)
    {
        if (string.IsNullOrWhiteSpace(editorPath))
            return Empty();

        var text = sourceText;
        if (string.IsNullOrEmpty(text) && File.Exists(editorPath))
        {
            try { text = File.ReadAllText(editorPath); }
            catch { text = null; }
        }

        var roslyn = GlassRoslynDiagnosticsFeed.CollectForFile(editorPath, text);
        var scopedBuild = ScopeProblemsToFile(buildProblems, editorPath);
        var merged = GlassRoslynDiagnosticsFeed.MergeDistinct(roslyn, scopedBuild);
        var tests = ScopeTestFailsToFile(testFails, editorPath);
        return Summarize(merged, tests.Count);
    }

    public static Face Summarize(IReadOnlyList<GlassProblemItem> problems, int testFails = 0)
    {
        var errors = 0;
        var warns = 0;
        var errLines = new List<int>();
        var warnLines = new List<int>();
        foreach (var p in problems)
        {
            if (p.IsError)
            {
                errors++;
                if (!errLines.Contains(p.Line))
                    errLines.Add(p.Line);
            }
            else
            {
                warns++;
                if (!warnLines.Contains(p.Line))
                    warnLines.Add(p.Line);
            }
        }

        var clean = errors == 0 && warns == 0 && testFails == 0;
        if (clean)
            return new Face("CLEAN · problems on MFD", 0, 0, 0, [], [], Clean: true);

        var bits = new List<string>(3);
        if (errors > 0 || warns > 0)
            bits.Add($"E{errors} W{warns}");
        if (testFails > 0)
            bits.Add($"T{testFails}");
        bits.Add("problems on MFD");
        return new Face(string.Join(" · ", bits), errors, warns, testFails, errLines, warnLines, Clean: false);
    }

    static Face Empty() => new("", 0, 0, 0, [], [], Clean: true);

    static IReadOnlyList<GlassProblemItem> ScopeProblemsToFile(
        IReadOnlyList<GlassProblemItem>? problems,
        string editorPath)
    {
        if (problems is null || problems.Count == 0)
            return [];

        string full;
        try { full = Path.GetFullPath(editorPath); }
        catch { return []; }

        var name = Path.GetFileName(full);
        var list = new List<GlassProblemItem>();
        foreach (var p in problems)
        {
            if (string.IsNullOrWhiteSpace(p.FilePath))
                continue;
            try
            {
                if (string.Equals(Path.GetFullPath(p.FilePath), full, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Path.GetFileName(p.FilePath), name, StringComparison.OrdinalIgnoreCase))
                    list.Add(p);
            }
            catch
            {
                if (string.Equals(Path.GetFileName(p.FilePath), name, StringComparison.OrdinalIgnoreCase))
                    list.Add(p);
            }
        }

        return list;
    }

    static IReadOnlyList<GlassTestOutputParse.FailRow> ScopeTestFailsToFile(
        IReadOnlyList<GlassTestOutputParse.FailRow>? fails,
        string editorPath)
    {
        if (fails is null || fails.Count == 0)
            return [];

        var name = Path.GetFileNameWithoutExtension(editorPath);
        if (string.IsNullOrWhiteSpace(name))
            return [];

        return fails
            .Where(f =>
                (!string.IsNullOrWhiteSpace(f.Name) && f.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(f.Message) && f.Message.Contains(Path.GetFileName(editorPath), StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(f.Display) && f.Display.Contains(Path.GetFileName(editorPath), StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
}

#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.SoftOrgan;

/// <summary>Lexer/syntax Roslyn diagnostics for open editor (MSBuild semantic stays in build output).</summary>
public static class GlassRoslynDiagnosticsFeed
{
    public static IReadOnlyList<GlassProblemItem> CollectForFile(string? filePath, string? sourceText)
    {
        if (string.IsNullOrWhiteSpace(filePath)
            || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(sourceText))
            return [];

        try
        {
            var tree = CSharpSyntaxTree.ParseText(
                SourceText.From(sourceText),
                path: filePath);
            var rows = new List<GlassProblemItem>();
            foreach (var d in tree.GetDiagnostics())
            {
                if (d.Severity is not (DiagnosticSeverity.Error or DiagnosticSeverity.Warning))
                    continue;

                var line = d.Location.GetLineSpan().StartLinePosition.Line + 1;
                var col = d.Location.GetLineSpan().StartLinePosition.Character + 1;
                var sev = d.Severity == DiagnosticSeverity.Error ? "error" : "warning";
                rows.Add(new GlassProblemItem(
                    filePath,
                    Math.Max(1, line),
                    Math.Max(1, col),
                    sev,
                    d.Id,
                    d.GetMessage()));
                if (rows.Count >= 64)
                    break;
            }

            return rows;
        }
        catch
        {
            return [];
        }
    }

    public static IReadOnlyList<GlassProblemItem> MergeDistinct(
        IEnumerable<GlassProblemItem> primary,
        IEnumerable<GlassProblemItem> extra)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var merged = new List<GlassProblemItem>();
        foreach (var item in primary.Concat(extra))
        {
            var key = $"{item.FilePath}|{item.Line}|{item.Column}|{item.Severity}|{item.Id}|{item.Message}";
            if (!seen.Add(key))
                continue;
            merged.Add(item);
        }

        return merged;
    }
}

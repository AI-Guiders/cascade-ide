#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.SoftOrgan;

/// <summary>Syntax + lightweight semantic Roslyn diagnostics for open editor (MSBuild stays in build output).</summary>
public static class GlassRoslynDiagnosticsFeed
{
    static readonly Lazy<IReadOnlyList<MetadataReference>> BasicRefs = new(LoadBasicReferences);

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
            var compilation = CSharpCompilation.Create(
                "glass-applies-locus",
                [tree],
                BasicRefs.Value,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var rows = new List<GlassProblemItem>();
            foreach (var d in tree.GetDiagnostics().Concat(compilation.GetDiagnostics()))
            {
                if (d.Severity is not (DiagnosticSeverity.Error or DiagnosticSeverity.Warning))
                    continue;

                var span = d.Location.GetLineSpan();
                var line = span.StartLinePosition.Line + 1;
                var col = span.StartLinePosition.Character + 1;
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

            return Dedup(rows);
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

    static IReadOnlyList<GlassProblemItem> Dedup(IReadOnlyList<GlassProblemItem> rows) =>
        MergeDistinct(rows, []);

    static IReadOnlyList<MetadataReference> LoadBasicReferences()
    {
        var list = new List<MetadataReference>(4);
        void Add(Type t)
        {
            try
            {
                var loc = t.Assembly.Location;
                if (!string.IsNullOrWhiteSpace(loc))
                    list.Add(MetadataReference.CreateFromFile(loc));
            }
            catch
            {
                /* skip */
            }
        }

        Add(typeof(object));
        Add(typeof(Enumerable));
        Add(typeof(Uri));
        return list;
    }
}

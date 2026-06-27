using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Services;

public sealed partial class CSharpLanguageService
{
    /// <summary>Sort using directives and remove duplicates (Roslyn fast path).</summary>
    public string OrganizeUsings(string filePath, string sourceText, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath))
            return sourceText;

        try
        {
            var tree = CSharpSyntaxTree.ParseText(sourceText, path: filePath, cancellationToken: ct);
            if (tree.GetRoot(ct) is not CompilationUnitSyntax root)
                return sourceText;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var sorted = new List<UsingDirectiveSyntax>();
            foreach (var u in root.Usings.OrderBy(static u => u.Name?.ToString() ?? "", StringComparer.Ordinal))
            {
                var key = u.ToFullString().Trim();
                if (!seen.Add(key))
                    continue;
                sorted.Add(u);
            }

            var newRoot = root.WithUsings(SyntaxFactory.List(sorted));
            return newRoot.ToFullString();
        }
        catch
        {
            return sourceText;
        }
    }
}

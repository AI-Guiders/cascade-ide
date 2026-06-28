using Microsoft.CodeAnalysis.CSharp;

namespace CascadeIDE.Services;

internal static class CSharpCompletionKeywords
{
    public static IReadOnlyList<string> All { get; } = Build();

    private static string[] Build()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (SyntaxKind kind in Enum.GetValues<SyntaxKind>())
        {
            if (!SyntaxFacts.IsKeywordKind(kind) && !SyntaxFacts.IsContextualKeyword(kind))
                continue;

            var text = SyntaxFacts.GetText(kind);
            if (text.Length > 0)
                set.Add(text);
        }

        var keywords = set.ToArray();
        Array.Sort(keywords, StringComparer.Ordinal);
        return keywords;
    }
}

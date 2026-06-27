using CascadeIDE.Services;

namespace CascadeIDE.Features.Editor.Application.Monaco;

public static class CideEditorCompletionMerger
{
    public static IReadOnlyList<CideEditorCompletionItem> Merge(
        IReadOnlyList<CideEditorCompletionItem> lspItems,
        IReadOnlyList<CideEditorCompletionItem> roslynItems,
        string prefix)
    {
        var filteredLsp = string.IsNullOrEmpty(prefix)
            ? lspItems.ToList()
            : Filter(lspItems, prefix);
        if (filteredLsp.Count == 0 && lspItems.Count > 0)
            filteredLsp = lspItems.ToList();

        if (string.IsNullOrEmpty(prefix))
            return filteredLsp.Count > 0 ? filteredLsp : roslynItems;

        var seen = new HashSet<string>(filteredLsp.Select(i => i.Label), StringComparer.OrdinalIgnoreCase);
        var merged = new List<CideEditorCompletionItem>(filteredLsp.Count + roslynItems.Count);
        merged.AddRange(filteredLsp);

        foreach (var item in roslynItems
                     .Where(i => CSharpCompletionMatcher.Matches(i.Label, prefix))
                     .Where(i => seen.Add(i.Label))
                     .OrderBy(i => i.Label, Comparer<string>.Create((a, b) =>
                         CSharpCompletionMatcher.CompareByRelevance(a, b, prefix))))
        {
            merged.Add(item);
        }

        return merged;
    }

    public static IReadOnlyList<CideEditorCompletionItem> Filter(
        IReadOnlyList<CideEditorCompletionItem> items,
        string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            return items.ToList();

        return items
            .Where(i => CSharpCompletionMatcher.Matches(i.Label, prefix))
            .OrderBy(i => i.Label, Comparer<string>.Create((a, b) =>
                CSharpCompletionMatcher.CompareByRelevance(a, b, prefix)))
            .ToList();
    }
}

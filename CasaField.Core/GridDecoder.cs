namespace CasaField.Core;

public static class CasaCanonical
{
    public const int FieldMinVotes = 1;
    public const int RepairMinVotes = 2;
}

public sealed record DecodedGrid(
    IReadOnlyList<string> ConceptIds,
    IReadOnlyList<string> Edges,
    IReadOnlyList<string> PresentTokens,
    int MinVotes);

public static class GridDecoder
{
    public static DecodedGrid Decode(IReadOnlyList<IReadOnlyList<string>> cells, int minVotes)
    {
        var vote = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var cell in cells)
        {
            foreach (var token in cell)
            {
                vote[token] = vote.GetValueOrDefault(token) + 1;
            }
        }

        var present = vote
            .Where(kv => kv.Value >= minVotes)
            .Select(kv => kv.Key)
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToList();

        var concepts = present
            .Where(t => t.StartsWith("C:", StringComparison.Ordinal))
            .Select(t => t[2..])
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var edges = present
            .Where(t => t.StartsWith("E:", StringComparison.Ordinal))
            .Select(t => t[2..])
            .ToList();

        return new DecodedGrid(concepts, edges, present, minVotes);
    }
}

#nullable enable

namespace CascadeIDE.SoftInstrument;

/// <summary>Compat shim — prefer <see cref="GlassRelatedFilesFeed"/> (WNM RelatedRow shape).</summary>
public static class GlassRelatedFilesHeuristic
{
    public sealed record Item(string FilePath, string Reason)
    {
        public string Display => $"{Path.GetFileName(FilePath)} · {Reason}";
    }

    public static IReadOnlyList<Item> Collect(string? workspaceRoot, string? editorPath, int max = 64) =>
        GlassRelatedFilesFeed.Collect(workspaceRoot, editorPath, max)
            .Select(i => new Item(i.FullPath, string.IsNullOrWhiteSpace(i.Rationale) ? i.Kind : i.Rationale))
            .ToList();
}

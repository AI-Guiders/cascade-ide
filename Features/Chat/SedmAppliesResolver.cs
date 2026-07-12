#nullable enable
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>0061 applies one-liner для T2 context card (ADR 0174 S3).</summary>
internal static class SedmAppliesResolver
{
    public static IReadOnlyList<SedmAppliesEntryPayload> Resolve(
        string? workspaceRoot,
        string anchorPath,
        int maxEntries = 3)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || string.IsNullOrWhiteSpace(anchorPath))
            return [];

        var root = workspaceRoot.Trim();
        var rel = anchorPath.Replace('\\', '/').Trim();
        var abs = WorkspaceAdrMapResolver.TryResolveAbsoluteDocPath(root, rel)
            ?? Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));

        var correspondence = WorkspaceCorrespondenceResolver.Resolve(root, abs);
        if (correspondence.AdrDocPaths.Length == 0)
            return [];

        var applies = new List<SedmAppliesEntryPayload>(Math.Min(maxEntries, correspondence.AdrDocPaths.Length));
        foreach (var docPath in correspondence.AdrDocPaths.Take(maxEntries))
        {
            var adrRef = WorkspaceAdrMapResolver.GuessAdrPreviewTitle(docPath);
            if (string.IsNullOrWhiteSpace(adrRef))
                continue;

            applies.Add(new SedmAppliesEntryPayload(
                "adr",
                adrRef,
                BuildOneLiner(adrRef, correspondence.FeatureLine),
                "path_map"));
        }

        return applies;
    }

    public static string? BuildPathHint(
        IReadOnlyList<SedmAppliesEntryPayload>? applies,
        string anchorPath)
    {
        if (applies is not { Count: > 0 })
            return null;

        var chain = string.Join(" → ", applies.Select(static a => $"ADR {a.Ref}"));
        return $"{chain} → {anchorPath.Replace('\\', '/')}";
    }

    private static string BuildOneLiner(string adrRef, string featureLine)
    {
        if (!string.IsNullOrWhiteSpace(featureLine))
            return Truncate(featureLine.Trim(), 56);
        return $"ADR {adrRef}";
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";
}

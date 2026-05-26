#nullable enable

using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Services.Intercom;

/// <summary>Inferred index message ordinal ↔ code anchors (ADR 0137 фаза 1).</summary>
public static class IntercomMessageCodeCorrespondenceProjector
{
    public const string MatchKindInferred = "inferred";
    public const string MatchKindExplicit = "explicit";

    public sealed record LaneMessage(
        int Ordinal,
        int MessageIndex,
        Guid MessageId,
        IReadOnlyList<AttachmentAnchor> Attachments);

    public sealed record InferredEntry(
        int Ordinal,
        int MessageIndex,
        Guid MessageId,
        string File,
        string? MemberKey,
        string? AttachmentShape,
        string? AnchorId,
        int? LineStart,
        int? LineEnd,
        string MatchKind);

    public sealed record MatchHit(
        int Ordinal,
        int MessageIndex,
        Guid MessageId,
        string MatchKind);

    public static IReadOnlyList<InferredEntry> BuildInferred(IReadOnlyList<LaneMessage> lane)
    {
        var list = new List<InferredEntry>();
        foreach (var msg in lane)
        {
            foreach (var anchor in msg.Attachments)
            {
                if (string.IsNullOrWhiteSpace(anchor.File))
                    continue;

                var file = anchor.File.Replace('\\', '/');
                int? start = anchor.LineStart;
                int? end = anchor.LineEnd ?? anchor.LineStart;
                list.Add(new InferredEntry(
                    msg.Ordinal,
                    msg.MessageIndex,
                    msg.MessageId,
                    file,
                    anchor.MemberKey,
                    anchor.AttachmentShape,
                    anchor.Id,
                    start,
                    end,
                    MatchKindInferred));
            }
        }

        return list;
    }

    public static IReadOnlyList<InferredEntry> BuildCombined(
        IReadOnlyList<LaneMessage> lane,
        IReadOnlyList<IntercomMessageRangeRelatedProjector.ExplicitRelate> explicitRelates)
    {
        var list = BuildInferred(lane).ToList();
        if (explicitRelates.Count == 0)
            return list;

        var byOrdinal = lane.ToDictionary(m => m.Ordinal);
        foreach (var relate in explicitRelates)
        {
            foreach (var segment in relate.OrdinalSegments)
            {
                for (var ordinal = segment.StartOrdinal; ordinal <= segment.EndOrdinal; ordinal++)
                {
                    if (!byOrdinal.TryGetValue(ordinal, out var msg))
                        continue;

                    appendEntriesFromAnchor(
                        list,
                        msg.Ordinal,
                        msg.MessageIndex,
                        msg.MessageId,
                        relate.CodeRef,
                        MatchKindExplicit);
                }
            }
        }

        return list;
    }

    public static IReadOnlyList<MatchHit> Find(
        IReadOnlyList<InferredEntry> entries,
        IntercomCodeRefQuery query,
        string? workspaceRoot)
    {
        var queryFile = normalizeFileKey(query.File, workspaceRoot);
        if (queryFile.Length == 0)
            return [];

        var hits = new List<MatchHit>();
        var seen = new HashSet<(int Ordinal, int MessageIndex)>();

        foreach (var entry in entries)
        {
            var entryFile = normalizeFileKey(entry.File, workspaceRoot);
            if (!string.Equals(entryFile, queryFile, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!matches(query, entry))
                continue;

            var key = (entry.Ordinal, entry.MessageIndex);
            if (!seen.Add(key))
                continue;

            hits.Add(new MatchHit(entry.Ordinal, entry.MessageIndex, entry.MessageId, entry.MatchKind));
        }

        return hits.OrderBy(h => h.Ordinal).ToList();
    }

    private static bool matches(IntercomCodeRefQuery query, InferredEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(query.MemberKey) && !string.IsNullOrWhiteSpace(entry.MemberKey))
            return string.Equals(query.MemberKey, entry.MemberKey, StringComparison.Ordinal);

        if (!query.HasLineRange)
            return string.IsNullOrWhiteSpace(query.MemberKey);

        return matchesLines(query, entry);
    }

    private static bool matchesLines(IntercomCodeRefQuery query, InferredEntry entry)
    {
        if (!query.HasLineRange)
            return true;

        if (entry.LineStart is null)
            return false;

        var entryEnd = entry.LineEnd ?? entry.LineStart.Value;
        return rangesOverlap(
            query.LineStart!.Value,
            query.LineEnd!.Value,
            entry.LineStart.Value,
            entryEnd);
    }

    private static bool rangesOverlap(int aStart, int aEnd, int bStart, int bEnd) =>
        aStart <= bEnd && bStart <= aEnd;

    private static void appendEntriesFromAnchor(
        List<InferredEntry> list,
        int ordinal,
        int messageIndex,
        Guid messageId,
        AttachmentAnchor anchor,
        string matchKind)
    {
        if (string.IsNullOrWhiteSpace(anchor.File))
            return;

        var file = anchor.File.Replace('\\', '/');
        int? start = anchor.LineStart;
        int? end = anchor.LineEnd ?? anchor.LineStart;
        list.Add(new InferredEntry(
            ordinal,
            messageIndex,
            messageId,
            file,
            anchor.MemberKey,
            anchor.AttachmentShape,
            anchor.Id,
            start,
            end,
            matchKind));
    }

    private static string normalizeFileKey(string file, string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(file))
            return "";

        var trimmed = file.Trim().Replace('\\', '/');
        if (Path.IsPathRooted(trimmed))
        {
            var rel = AttachmentAnchorPaths.ToWorkspaceRelative(trimmed, workspaceRoot);
            return (rel ?? trimmed).Replace('\\', '/');
        }

        return trimmed;
    }
}

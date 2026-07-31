#nullable enable

using System.Text.Json;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    private static bool TryParseOrdinalSegmentsFromMcp(
        IReadOnlyDictionary<string, JsonElement> args,
        out IReadOnlyList<ChatHistoryMessageOrdinalSegment> ordinalSegments,
        out IReadOnlyList<ParametricIntRange> parametricSegments,
        out string error)
    {
        ordinalSegments = [];
        parametricSegments = [];
        error = "";

        if (args.TryGetValue("range_expr", out var rangeExpr)
            && rangeExpr.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(rangeExpr.GetString()))
        {
            if (!ParametricSegmentListParser.TryParse(rangeExpr.GetString(), out parametricSegments, out error))
                return false;

            ordinalSegments = ToOrdinalSegments(parametricSegments);
            return true;
        }

        if (args.TryGetValue("ordinal_segments", out var segmentsEl)
            && segmentsEl.ValueKind == JsonValueKind.Array)
        {
            var list = new List<ChatHistoryMessageOrdinalSegment>();
            foreach (var item in segmentsEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    error = "ordinal_segments: каждый элемент — объект { start_ordinal, end_ordinal }.";
                    return false;
                }

                if (!item.TryGetProperty("start_ordinal", out var startEl)
                    || !item.TryGetProperty("end_ordinal", out var endEl)
                    || !startEl.TryGetInt32(out var start)
                    || !endEl.TryGetInt32(out var end))
                {
                    error = "ordinal_segments: укажи start_ordinal и end_ordinal (integer ≥ 1).";
                    return false;
                }

                list.Add(new ChatHistoryMessageOrdinalSegment(start, end));
            }

            if (list.Count == 0)
            {
                error = "ordinal_segments не может быть пустым.";
                return false;
            }

            ordinalSegments = list;
            parametricSegments = list
                .Select(s => new ParametricIntRange(s.StartOrdinal, s.EndOrdinal))
                .ToList();
            return true;
        }

        var startOrdinal = McpCommandJsonArgs.OptionalInt32(args, "start_ordinal");
        var endOrdinal = McpCommandJsonArgs.OptionalInt32(args, "end_ordinal") ?? startOrdinal;
        if (startOrdinal is null or < 1 || endOrdinal is null or < 1 || endOrdinal < startOrdinal)
        {
            error = "Укажи start_ordinal (1-based) и опционально end_ordinal, либо range_expr / ordinal_segments для disjoint.";
            return false;
        }

        ordinalSegments = [new ChatHistoryMessageOrdinalSegment(startOrdinal.Value, endOrdinal.Value)];
        parametricSegments = [new ParametricIntRange(startOrdinal.Value, endOrdinal.Value)];
        return true;
    }

    private static IReadOnlyList<ChatHistoryMessageOrdinalSegment> ToOrdinalSegments(
        IReadOnlyList<ParametricIntRange> segments) =>
        segments.Select(s => new ChatHistoryMessageOrdinalSegment(s.Start, s.End)).ToList();
}

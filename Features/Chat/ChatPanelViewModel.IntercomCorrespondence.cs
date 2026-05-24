#nullable enable

using System.Text.Json;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    private IReadOnlyList<IntercomMessageRangeRelatedProjector.ExplicitRelate> _explicitMessageRangeRelates = [];

    /// <summary>Найти сообщения в активной ветке по коду (ADR 0137: inferred + explicit relate).</summary>
    public string FindMessagesForCodeRef(string? codeRefTail)
    {
        var editor = BuildAttachEditorSnapshot();
        var workspace = ResolveAttachWorkspaceRoot();
        var solution = ResolveAttachSolutionPath();
        if (!IntercomCodeRefParser.TryParse(
                codeRefTail,
                editor,
                workspace,
                solution,
                out var query,
                out var parseError,
                ResolveAttachIndexDirectoryRelative()))
        {
            return parseError;
        }

        return formatFindResult(executeFind(query, workspace));
    }

    /// <summary>
    /// Явно связать диапазон gutter-сообщений с кодом; пишет <see cref="ChatHistoryEventKind.MessageRangeRelated"/> (ADR 0137).
    /// </summary>
    public string RelateMessageRangeToCodeRef(string? relateTail)
    {
        if (!IntercomMessageRelateArgs.TryParse(relateTail, out var segments, out var codeRefTail, out var parseError))
            return parseError;

        if (IsChatOverviewMode)
            return "Открой тему (detail): /intercom topic open или клик по карточке.";

        if (_activeThreadId == Guid.Empty)
            return "Нет активной ветки.";

        if (!TryGetActiveDetailLaneMessageIndices(out var indices))
            return "В активной ветке нет сообщений.";

        var ordinalSegments = segments
            .Select(s => new ChatHistoryMessageOrdinalSegment(s.Start, s.End))
            .ToList();

        if (!IntercomMessageRangeRelatedSupport.TryValidateSegmentsInLane(ordinalSegments, indices.Count, out var rangeError))
            return rangeError;

        var selectResult = segments.Count == 1
            ? SelectMessageByOrdinalRangeInDetailLane(segments[0].Start, segments[0].End)
            : SelectMessagesByOrdinalRangesInDetailLane(segments);

        if (!string.Equals(selectResult, "OK", StringComparison.Ordinal))
            return selectResult;

        var editor = BuildAttachEditorSnapshot();
        var workspace = ResolveAttachWorkspaceRoot();
        var solution = ResolveAttachSolutionPath();
        if (!IntercomCodeRefParser.TryResolveAnchor(
                codeRefTail,
                editor,
                workspace,
                solution,
                out var anchor,
                out var anchorError,
                ResolveAttachIndexDirectoryRelative()))
        {
            return anchorError;
        }

        var payload = IntercomMessageRangeRelatedSupport.CreatePayload(
            _activeThreadId.ToString("N"),
            ordinalSegments,
            anchor,
            "slash");

        _ = PersistEventAsync(ChatHistoryEventKind.MessageRangeRelated, payload, _activeThreadId);
        appendExplicitRelateInMemory(payload);

        var label = IntercomMessageRangeRelatedSupport.FormatOrdinalSummary(ordinalSegments);
        return $"Связь сообщений {label} с кодом записана ({anchor.DisplayLabel ?? anchor.File}).";
    }

    /// <summary>JSON для MCP <c>intercom.messages_for_code</c>.</summary>
    public string FindMessagesForCodeRefFromMcp(IReadOnlyDictionary<string, JsonElement>? args)
    {
        var editor = BuildAttachEditorSnapshot();
        var workspace = ResolveAttachWorkspaceRoot();
        var solution = ResolveAttachSolutionPath();
        if (!IntercomCodeRefParser.TryParseFromMcp(args, editor, workspace, solution, out var query, out var parseError))
            return JsonSerializer.Serialize(new { error = "parse", message = parseError });

        var result = executeFind(query, workspace);
        if (result.Error is { } err)
            return JsonSerializer.Serialize(new { error = err.Kind, message = err.Message });

        return JsonSerializer.Serialize(new
        {
            query = new
            {
                file = query.File,
                line_start = query.LineStart,
                line_end = query.LineEnd,
                member_key = query.MemberKey,
                anchor_id = query.ResolvedAnchor?.Id,
            },
            hits = result.Hits!.Select(h => new
            {
                ordinal = h.Ordinal,
                message_index = h.MessageIndex,
                message_id = h.MessageId.ToString("N"),
                match_kind = h.MatchKind,
            }),
            selected_ordinal = result.SelectedOrdinal,
            branch_message_count = result.BranchMessageCount,
        });
    }

    /// <summary>JSON для MCP <c>intercom.message_relate</c>.</summary>
    public string RelateMessageRangeToCodeRefFromMcp(IReadOnlyDictionary<string, JsonElement>? args)
    {
        if (args is null)
            return JsonSerializer.Serialize(new { error = "parse", message = "Отсутствуют аргументы." });

        if (!TryParseOrdinalSegmentsFromMcp(args, out var ordinalSegments, out var parametricSegments, out var segmentParseError))
        {
            return JsonSerializer.Serialize(new { error = "parse", message = segmentParseError });
        }

        var editor = BuildAttachEditorSnapshot();
        var workspace = ResolveAttachWorkspaceRoot();
        var solution = ResolveAttachSolutionPath();
        if (!IntercomCodeRefParser.TryResolveAnchorFromMcp(
                args,
                editor,
                workspace,
                solution,
                out var anchor,
                out var anchorError,
                ResolveAttachIndexDirectoryRelative()))
        {
            return JsonSerializer.Serialize(new { error = "parse", message = anchorError });
        }

        if (IsChatOverviewMode)
            return JsonSerializer.Serialize(new { error = "overview_mode", message = "Открой detail-ветку." });

        if (_activeThreadId == Guid.Empty)
            return JsonSerializer.Serialize(new { error = "no_thread", message = "Нет активной ветки." });

        if (!TryGetActiveDetailLaneMessageIndices(out var indices))
        {
            return JsonSerializer.Serialize(new { error = "empty_lane", message = "В активной ветке нет сообщений." });
        }

        if (!IntercomMessageRangeRelatedSupport.TryValidateSegmentsInLane(ordinalSegments, indices.Count, out var rangeError))
        {
            return JsonSerializer.Serialize(new { error = "range", message = rangeError });
        }

        var selectResult = parametricSegments.Count == 1
            ? SelectMessageByOrdinalRangeInDetailLane(parametricSegments[0].Start, parametricSegments[0].End)
            : SelectMessagesByOrdinalRangesInDetailLane(parametricSegments);

        if (!string.Equals(selectResult, "OK", StringComparison.Ordinal))
        {
            return JsonSerializer.Serialize(new { error = "range", message = selectResult });
        }

        var payload = IntercomMessageRangeRelatedSupport.CreatePayload(
            _activeThreadId.ToString("N"),
            ordinalSegments,
            anchor,
            "mcp");

        _ = PersistEventAsync(ChatHistoryEventKind.MessageRangeRelated, payload, _activeThreadId);
        appendExplicitRelateInMemory(payload);

        return JsonSerializer.Serialize(new
        {
            ok = true,
            thread_id = payload.ThreadId,
            start_ordinal = payload.StartOrdinal,
            end_ordinal = payload.EndOrdinal,
            ordinal_segments = ordinalSegments.Select(s => new
            {
                start_ordinal = s.StartOrdinal,
                end_ordinal = s.EndOrdinal,
            }),
            code_ref = new
            {
                id = anchor.Id,
                file = anchor.File,
                member_key = anchor.MemberKey,
                attachment_shape = anchor.AttachmentShape,
                line_start = anchor.LineStart,
                line_end = anchor.LineEnd,
            },
        });
    }

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

    private void rebuildExplicitRelatesFromEvents(IReadOnlyList<ChatHistoryEvent> events) =>
        _explicitMessageRangeRelates = IntercomMessageRangeRelatedProjector.Project(events);

    private void appendExplicitRelateInMemory(ChatHistoryMessageRangeRelatedPayload payload)
    {
        if (!Guid.TryParse(payload.ThreadId, out var threadId) || threadId == Guid.Empty)
            return;

        var list = _explicitMessageRangeRelates.ToList();
        list.Add(new IntercomMessageRangeRelatedProjector.ExplicitRelate(
            threadId,
            payload.StartOrdinal,
            payload.EndOrdinal,
            IntercomMessageRangeRelatedSupport.ResolveSegments(payload),
            payload.CodeRef,
            payload.Source));
        _explicitMessageRangeRelates = list;
    }

    private FindExecutionResult executeFind(IntercomCodeRefQuery query, string? workspace)
    {
        if (IsChatOverviewMode)
            return FindExecutionResult.Fail("overview_mode", "Открой тему (detail): /intercom topic open или клик по карточке.");

        if (!TryGetActiveDetailLaneMessageIndices(out var indices))
            return FindExecutionResult.Fail("empty_lane", "В активной ветке нет сообщений.");

        var lane = buildLaneMessages(indices);
        var explicitForThread = IntercomMessageRangeRelatedProjector.ForThread(_explicitMessageRangeRelates, _activeThreadId);
        var entries = IntercomMessageCodeCorrespondenceProjector.BuildCombined(lane, explicitForThread);
        var hits = IntercomMessageCodeCorrespondenceProjector.Find(entries, query, workspace);
        if (hits.Count == 0)
            return FindExecutionResult.Fail("no_hits", "Нет сообщений в ветке, связанных с этим фрагментом кода.");

        var ordinals = hits.Select(h => h.Ordinal).OrderBy(o => o).ToList();
        int? selected = null;
        if (string.Equals(SelectMessageByOrdinalRangeInDetailLane(ordinals[0], ordinals[^1]), "OK", StringComparison.Ordinal))
            selected = ordinals[^1];

        return new FindExecutionResult(hits, ordinals, selected, indices.Count, null);
    }

    private static string formatFindResult(FindExecutionResult result)
    {
        if (result.Error is { } err)
            return err.Message;

        var ordinals = result.Ordinals!;
        var label = ordinals.Count == 1
            ? $"#{ordinals[0]}"
            : $"#{ordinals[0]}–#{ordinals[^1]}";
        return $"Связанные сообщения: {label} ({result.Hits!.Count}). Активно #{ordinals[^1]}.";
    }

    private IReadOnlyList<IntercomMessageCodeCorrespondenceProjector.LaneMessage> buildLaneMessages(
        IReadOnlyList<int> indices)
    {
        var list = new List<IntercomMessageCodeCorrespondenceProjector.LaneMessage>();
        for (var i = 0; i < indices.Count; i++)
        {
            var messageIndex = indices[i];
            if (messageIndex < 0 || messageIndex >= ChatMessages.Count)
                continue;

            var m = ChatMessages[messageIndex];
            list.Add(new IntercomMessageCodeCorrespondenceProjector.LaneMessage(
                i + 1,
                messageIndex,
                m.MessageId,
                m.Attachments));
        }

        return list;
    }

    private sealed record FindExecutionResult(
        IReadOnlyList<IntercomMessageCodeCorrespondenceProjector.MatchHit>? Hits,
        IReadOnlyList<int>? Ordinals,
        int? SelectedOrdinal,
        int BranchMessageCount,
        FindError? Error)
    {
        public static FindExecutionResult Fail(string kind, string message) =>
            new(null, null, null, 0, new FindError(kind, message));
    }

    private sealed record FindError(string Kind, string Message);
}

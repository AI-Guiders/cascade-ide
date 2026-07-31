#nullable enable

using System.Text.Json;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Chat;

/// <summary>MCP JSON entry points for messages-for-code and message-relate (ADR 0137).</summary>
public partial class ChatPanelViewModel
{
    /// <summary>JSON для MCP <c>intercom.messages_for_code</c>.</summary>
    public string FindMessagesForCodeRefFromMcp(IReadOnlyDictionary<string, JsonElement>? args)
    {
        var editor = BuildAttachEditorSnapshot();
        var workspace = ResolveAttachWorkspaceRoot();
        var solution = ResolveAttachSolutionPath();
        if (!IntercomCodeRefParser.TryParseFromMcp(args, editor, workspace, solution, out var query, out var parseError))
            return JsonSerializer.Serialize(new { error = "parse", message = parseError });

        if (!TryGetActiveDetailLaneMessageIndices(out var indices))
            return JsonSerializer.Serialize(new { error = "empty_lane", message = "В активной ветке нет сообщений." });

        var result = IntercomCorrespondenceOperations.ExecuteFind(
            query,
            workspace,
            IsChatOverviewMode,
            indices.Count,
            buildLaneMessages(indices),
            IntercomMessageRangeRelatedProjector.ForThread(_explicitMessageRangeRelates, _activeThreadId),
            SelectMessageByOrdinalRangeInDetailLane);
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
}

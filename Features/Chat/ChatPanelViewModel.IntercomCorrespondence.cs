#nullable enable

using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Chat;

/// <summary>Slash find/relate message↔code correspondence + in-memory explicit relates (ADR 0137).</summary>
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

        if (!TryGetActiveDetailLaneMessageIndices(out var indices))
            return "В активной ветке нет сообщений.";

        return IntercomCorrespondenceOperations.FormatFindResult(
            IntercomCorrespondenceOperations.ExecuteFind(
                query,
                workspace,
                IsChatOverviewMode,
                indices.Count,
                buildLaneMessages(indices),
                IntercomMessageRangeRelatedProjector.ForThread(_explicitMessageRangeRelates, _activeThreadId),
                SelectMessageByOrdinalRangeInDetailLane));
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
}

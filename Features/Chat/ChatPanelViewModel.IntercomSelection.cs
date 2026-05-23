#nullable enable

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    /// <summary>Выбор сообщения по номеру gutter (1-based) в активной ветке detail-ленты (ADR 0136).</summary>
    public string SelectMessageByOrdinalInDetailLane(int ordinal) =>
        SelectMessageByOrdinalRangeInDetailLane(ordinal, ordinal);

    /// <summary>
    /// Диапазон gutter-номеров (1-based), синтаксис как <c>/editor line select</c>: одно число, «L R» или «L:R».
    /// Активным становится сообщение с номером <paramref name="endOrdinal"/> (конец диапазона).
    /// </summary>
    public string SelectMessageByOrdinalRangeInDetailLane(int startOrdinal, int endOrdinal)
    {
        if (IsChatOverviewMode)
            return "Открой тему (detail): /intercom topic open или клик по карточке.";

        if (startOrdinal < 1 || endOrdinal < 1)
            return "Номера сообщений должны быть ≥ 1.";

        if (endOrdinal < startOrdinal)
            return "Конец диапазона не может быть меньше начала.";

        if (!TryGetActiveDetailLaneMessageIndices(out var indices))
            return "В активной ветке нет сообщений.";

        if (startOrdinal > indices.Count)
            return $"Нет сообщения #{startOrdinal} (в ветке {indices.Count}).";

        if (endOrdinal > indices.Count)
            return $"Нет сообщения #{endOrdinal} (в ветке {indices.Count}).";

        ApplyMessageOrdinalSelection(indices, startOrdinal, endOrdinal);
        return "OK";
    }

    /// <summary>Disjoint multi-range по gutter (ADR 0138). Активным — конец последнего сегмента.</summary>
    public string SelectMessagesByOrdinalRangesInDetailLane(IReadOnlyList<ParametricIntRange> segments)
    {
        if (IsChatOverviewMode)
            return "Открой тему (detail): /intercom topic open или клик по карточке.";

        if (segments.Count == 0)
            return "Укажи хотя бы один сегмент [n] или [a;b].";

        if (!TryGetActiveDetailLaneMessageIndices(out var indices))
            return "В активной ветке нет сообщений.";

        var lastSegment = segments[^1];
        if (lastSegment.End > indices.Count)
            return $"Нет сообщения #{lastSegment.End} (в ветке {indices.Count}).";

        foreach (var segment in segments)
        {
            if (segment.Start < 1 || segment.End < 1)
                return "Номера сообщений должны быть ≥ 1.";
            if (segment.End < segment.Start)
                return "Конец диапазона не может быть меньше начала.";
            if (segment.Start > indices.Count)
                return $"Нет сообщения #{segment.Start} (в ветке {indices.Count}).";
            if (segment.End > indices.Count)
                return $"Нет сообщения #{segment.End} (в ветке {indices.Count}).";
        }

        ApplyMessageOrdinalSelection(indices, segments[0].Start, lastSegment.End, segments);
        return "OK";
    }

    /// <summary>Сбросить multi-highlight и активное сообщение в detail-ленте (slash <c>/intercom message select clear</c>).</summary>
    public string ClearMessageSelectionInDetailLane()
    {
        if (IsChatOverviewMode)
            return "Открой тему (detail): /intercom topic open или клик по карточке.";

        HighlightedMessageIndices = new HashSet<int>();
        SelectedMessageIndex = -1;
        RefreshChatSurfaceSnapshot();
        return "OK";
    }

    private void ApplyMessageOrdinalSelection(
        IReadOnlyList<int> laneIndices,
        int startOrdinal,
        int endOrdinal,
        IReadOnlyList<ParametricIntRange>? highlightSegments = null)
    {
        var highlighted = new HashSet<int>();
        if (highlightSegments is { Count: > 1 })
        {
            foreach (var segment in highlightSegments)
            {
                for (var ord = segment.Start; ord <= segment.End; ord++)
                    highlighted.Add(laneIndices[ord - 1]);
            }
        }
        else
        {
            for (var ord = startOrdinal; ord <= endOrdinal; ord++)
                highlighted.Add(laneIndices[ord - 1]);
        }

        HighlightedMessageIndices = highlighted;
        SelectedMessageIndex = laneIndices[endOrdinal - 1];
        RefreshChatSurfaceSnapshot();
    }

    public bool TryGetFeedOrdinalForMessageIndex(int messageIndex, out int ordinal)
    {
        ordinal = 0;
        if (!TryGetActiveDetailLaneMessageIndices(out var indices))
            return false;

        for (var i = 0; i < indices.Count; i++)
        {
            if (indices[i] != messageIndex)
                continue;
            ordinal = i + 1;
            return true;
        }

        return false;
    }

    private bool TryGetActiveDetailLaneMessageIndices(out IReadOnlyList<int> messageIndices)
    {
        messageIndices = [];
        var threadId = _activeThreadId;
        if (threadId == Guid.Empty)
            return false;

        foreach (var lane in ChatSurfaceSnapshot.Layout.Lanes)
        {
            if (lane.Thread.ThreadId != threadId)
                continue;

            var list = new List<int>();
            foreach (var entry in lane.Entries)
            {
                if (entry.Kind != ChatSurfaceEntryKind.Message)
                    continue;
                if (entry.MessageIndex is { } messageIndex)
                    list.Add(messageIndex);
            }

            messageIndices = list;
            return list.Count > 0;
        }

        return false;
    }
}

#nullable enable
using System.Text.Json;
using CascadeIDE.Models.AgentChat;

namespace CascadeIDE.Features.Chat;

/// <summary>Детерминированная проекция SEDM-событий из append-only log (ADR 0173/0174 MLP).</summary>
public static class SedmEventProjector
{
    public sealed record DecisionState(
        Guid EventId,
        SedmDecisionRecordedPayload Payload,
        string Status);

    public sealed record WorklineProjection(
        string WorklineId,
        SedmContextCardMaterializedPayload? ContextCard,
        SedmIntentCardRecordedPayload? IntentCard,
        DecisionState? ActiveDecision,
        IReadOnlyList<DecisionState> DecisionHistory);

    public sealed record SessionProjection(
        IReadOnlyDictionary<string, WorklineProjection> ByWorkline,
        int OpenWorklineCount);

    public static SessionProjection Project(
        IReadOnlyList<ChatHistoryEvent> events,
        Guid activeWorklineId,
        IReadOnlyDictionary<Guid, string>? threadTitles = null,
        int openWorklineCount = 1)
    {
        var worklines = new Dictionary<string, MutableWorkline>(StringComparer.OrdinalIgnoreCase);
        var decisions = new Dictionary<string, DecisionState>(StringComparer.OrdinalIgnoreCase);

        foreach (var ev in events)
        {
            switch (ev.Kind)
            {
                case ChatHistoryEventKind.ContextCardMaterialized:
                    if (TryDeserialize<SedmContextCardMaterializedPayload>(ev.PayloadJson, out var ctx))
                        GetOrAdd(worklines, ctx.WorklineId).ContextCard = ctx;
                    break;

                case ChatHistoryEventKind.IntentCardRecorded:
                    if (TryDeserialize<SedmIntentCardRecordedPayload>(ev.PayloadJson, out var intent))
                        GetOrAdd(worklines, intent.WorklineId).IntentCard = intent;
                    break;

                case ChatHistoryEventKind.DecisionRecorded:
                    if (TryDeserialize<SedmDecisionRecordedPayload>(ev.PayloadJson, out var decision))
                    {
                        var state = new DecisionState(ev.EventId, decision, NormalizeStatus(decision.Status));
                        decisions[ev.EventId.ToString("N")] = state;
                        GetOrAdd(worklines, decision.WorklineId).DecisionHistory.Add(state);
                    }
                    break;

                case ChatHistoryEventKind.DecisionMarkedStale:
                    if (TryDeserialize<SedmDecisionLifecyclePayload>(ev.PayloadJson, out var stale)
                        && decisions.TryGetValue(stale.DecisionEventId, out var staleTarget))
                    {
                        decisions[stale.DecisionEventId] = staleTarget with { Status = "stale" };
                        ReplaceInHistory(worklines, stale.DecisionEventId, "stale");
                    }
                    break;

                case ChatHistoryEventKind.DecisionSuperseded:
                    if (TryDeserialize<SedmDecisionLifecyclePayload>(ev.PayloadJson, out var superseded)
                        && decisions.TryGetValue(superseded.DecisionEventId, out var supersededTarget))
                    {
                        decisions[superseded.DecisionEventId] = supersededTarget with { Status = "superseded" };
                        ReplaceInHistory(worklines, superseded.DecisionEventId, "superseded");
                    }
                    break;
            }
        }

        var byWorkline = worklines.ToDictionary(
            static kv => kv.Key,
            static kv => ToProjection(kv.Value),
            StringComparer.OrdinalIgnoreCase);

        if (activeWorklineId != Guid.Empty)
        {
            var activeKey = activeWorklineId.ToString("N");
            if (!byWorkline.ContainsKey(activeKey))
            {
                var label = threadTitles is not null && threadTitles.TryGetValue(activeWorklineId, out var title)
                    ? title
                    : null;
                byWorkline[activeKey] = new WorklineProjection(
                    activeKey,
                    null,
                    null,
                    null,
                    []);
            }
        }

        return new SessionProjection(byWorkline, Math.Max(1, openWorklineCount));
    }

    public static WorklineProjection ResolveWorkline(SessionProjection projection, Guid worklineId)
    {
        if (worklineId == Guid.Empty)
            return EmptyWorkline("");

        var key = worklineId.ToString("N");
        return projection.ByWorkline.TryGetValue(key, out var wl)
            ? wl
            : EmptyWorkline(key);
    }

    public static bool IsSameContextCard(SedmContextCardMaterializedPayload? left, SedmContextCardMaterializedPayload right) =>
        left is not null
        && string.Equals(left.WorklineId, right.WorklineId, StringComparison.OrdinalIgnoreCase)
        && string.Equals(left.Anchor.Path, right.Anchor.Path, StringComparison.OrdinalIgnoreCase)
        && string.Equals(left.Anchor.Symbol, right.Anchor.Symbol, StringComparison.OrdinalIgnoreCase);

    private static WorklineProjection ToProjection(MutableWorkline state)
    {
        DecisionState? active = null;
        for (var i = state.DecisionHistory.Count - 1; i >= 0; i--)
        {
            var candidate = state.DecisionHistory[i];
            if (string.Equals(candidate.Status, "active", StringComparison.OrdinalIgnoreCase))
            {
                active = candidate;
                break;
            }
        }

        return new WorklineProjection(
            state.WorklineId,
            state.ContextCard,
            state.IntentCard,
            active,
            state.DecisionHistory);
    }

    private static WorklineProjection EmptyWorkline(string worklineId) =>
        new(worklineId, null, null, null, []);

    private static MutableWorkline GetOrAdd(Dictionary<string, MutableWorkline> worklines, string worklineId)
    {
        if (!worklines.TryGetValue(worklineId, out var wl))
        {
            wl = new MutableWorkline(worklineId);
            worklines[worklineId] = wl;
        }

        return wl;
    }

    private static void ReplaceInHistory(
        Dictionary<string, MutableWorkline> worklines,
        string decisionEventId,
        string status)
    {
        foreach (var wl in worklines.Values)
        {
            for (var i = 0; i < wl.DecisionHistory.Count; i++)
            {
                if (!string.Equals(wl.DecisionHistory[i].EventId.ToString("N"), decisionEventId, StringComparison.OrdinalIgnoreCase))
                    continue;
                wl.DecisionHistory[i] = wl.DecisionHistory[i] with { Status = status };
            }
        }
    }

    private static string NormalizeStatus(string? status) =>
        string.IsNullOrWhiteSpace(status) ? "active" : status.Trim().ToLowerInvariant();

    private static bool TryDeserialize<T>(string payloadJson, out T value)
    {
        value = default!;
        try
        {
            var parsed = JsonSerializer.Deserialize<T>(payloadJson, ChatHistoryJson.Options);
            if (parsed is null)
                return false;
            value = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed class MutableWorkline(string worklineId)
    {
        public string WorklineId { get; } = worklineId;
        public SedmContextCardMaterializedPayload? ContextCard { get; set; }
        public SedmIntentCardRecordedPayload? IntentCard { get; set; }
        public List<DecisionState> DecisionHistory { get; } = [];
    }
}

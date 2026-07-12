#nullable enable
using CascadeIDE.Models.AgentChat;

namespace CascadeIDE.Features.Chat;

/// <summary>Системная карточка SEDM в ленте workline (ADR 0173 P1).</summary>
public sealed record ChatSedmTimelineEntry(
    Guid EventId,
    Guid WorklineId,
    string Kind,
    string Title,
    string Body,
    ChatMessageVisualRole VisualRole,
    DateTimeOffset AtUtc,
    int Sequence);

/// <summary>Проекция SEDM-событий → timeline entries для активной workline.</summary>
internal static class SedmTimelineBuilder
{
    public static IReadOnlyList<ChatSedmTimelineEntry> Build(
        IReadOnlyList<ChatHistoryEvent> events,
        Guid worklineId)
    {
        if (worklineId == Guid.Empty || events.Count == 0)
            return [];

        var key = worklineId.ToString("N");
        var result = new List<ChatSedmTimelineEntry>();
        var seq = 0;

        foreach (var ev in events.OrderBy(static e => e.AtUtc).ThenBy(static e => e.EventId))
        {
            if (!BelongsToWorkline(ev, key))
                continue;

            if (TryMap(ev, worklineId, ref seq, out var entry))
                result.Add(entry);
        }

        return result;
    }

    private static bool BelongsToWorkline(ChatHistoryEvent ev, string worklineKey)
    {
        if (ev.ThreadId is { } tid && string.Equals(tid, worklineKey, StringComparison.OrdinalIgnoreCase))
            return true;

        return ev.PayloadJson.Contains($"\"workline_id\":\"{worklineKey}\"", StringComparison.OrdinalIgnoreCase)
            || ev.PayloadJson.Contains($"\"workline_id\": \"{worklineKey}\"", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryMap(
        ChatHistoryEvent ev,
        Guid worklineId,
        ref int seq,
        out ChatSedmTimelineEntry entry)
    {
        entry = default!;
        try
        {
            switch (ev.Kind)
            {
                case ChatHistoryEventKind.ContextCardMaterialized:
                    var ctx = Deserialize<SedmContextCardMaterializedPayload>(ev.PayloadJson);
                    if (ctx is null)
                        return false;
                    entry = new ChatSedmTimelineEntry(
                        ev.EventId,
                        worklineId,
                        ev.Kind,
                        "Context",
                        FormatContextBody(ctx),
                        ChatMessageVisualRole.SedmContext,
                        ev.AtUtc,
                        seq++);
                    return true;

                case ChatHistoryEventKind.IntentCardRecorded:
                    var intent = Deserialize<SedmIntentCardRecordedPayload>(ev.PayloadJson);
                    if (intent is null)
                        return false;
                    entry = new ChatSedmTimelineEntry(
                        ev.EventId,
                        worklineId,
                        ev.Kind,
                        "Intent",
                        FormatIntentBody(intent),
                        ChatMessageVisualRole.SedmIntent,
                        ev.AtUtc,
                        seq++);
                    return true;

                case ChatHistoryEventKind.DecisionRecorded:
                    var decision = Deserialize<SedmDecisionRecordedPayload>(ev.PayloadJson);
                    if (decision is null)
                        return false;
                    entry = new ChatSedmTimelineEntry(
                        ev.EventId,
                        worklineId,
                        ev.Kind,
                        "Decision",
                        FormatDecisionBody(decision),
                        ChatMessageVisualRole.SedmDecision,
                        ev.AtUtc,
                        seq++);
                    return true;

                case ChatHistoryEventKind.DecisionMarkedStale:
                case ChatHistoryEventKind.DecisionSuperseded:
                    var life = Deserialize<SedmDecisionLifecyclePayload>(ev.PayloadJson);
                    if (life is null)
                        return false;
                    var label = ev.Kind == ChatHistoryEventKind.DecisionMarkedStale ? "Decision stale" : "Decision superseded";
                    entry = new ChatSedmTimelineEntry(
                        ev.EventId,
                        worklineId,
                        ev.Kind,
                        label,
                        string.IsNullOrWhiteSpace(life.Reason) ? life.DecisionEventId : life.Reason.Trim(),
                        ChatMessageVisualRole.SedmLifecycle,
                        ev.AtUtc,
                        seq++);
                    return true;

                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private static string FormatContextBody(SedmContextCardMaterializedPayload ctx)
    {
        var path = ctx.Anchor.Path;
        var symbol = string.IsNullOrWhiteSpace(ctx.Anchor.Symbol) ? "" : $" :: {ctx.Anchor.Symbol}";
        var applies = ctx.Applies?.FirstOrDefault();
        var appliesLine = applies is null ? "" : $"{Environment.NewLine}Applies: ADR {applies.Ref} — {applies.OneLiner}";
        var hint = string.IsNullOrWhiteSpace(ctx.PathHint) ? "" : $"{Environment.NewLine}Path: {ctx.PathHint}";
        return $"Here: {path}{symbol}{appliesLine}{hint}".Trim();
    }

    private static string FormatIntentBody(SedmIntentCardRecordedPayload intent)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(intent.Card.Trigger))
            lines.Add("Trigger: " + intent.Card.Trigger.Trim());
        if (!string.IsNullOrWhiteSpace(intent.Card.Outcome))
            lines.Add("Outcome: " + intent.Card.Outcome.Trim());
        if (!string.IsNullOrWhiteSpace(intent.Card.ChosenApproach))
            lines.Add("Chosen: " + intent.Card.ChosenApproach.Trim());
        if (!string.IsNullOrWhiteSpace(intent.Card.SelectionRationale))
            lines.Add("Because: " + intent.Card.SelectionRationale.Trim());
        if (intent.Considered is { Count: > 0 })
        {
            lines.Add("Rejected:");
            foreach (var c in intent.Considered.Take(3))
                lines.Add($"  · {c.Approach}: {c.RejectedBecause}");
        }
        if (SedmCardCompleteness.IsIntentIncomplete(intent))
            lines.Add("(incomplete — add considered[] or rationale)");
        return lines.Count == 0 ? "Intent card" : string.Join(Environment.NewLine, lines);
    }

    private static string FormatDecisionBody(SedmDecisionRecordedPayload decision)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(decision.Card.Outcome))
            lines.Add("Outcome: " + decision.Card.Outcome.Trim());
        if (!string.IsNullOrWhiteSpace(decision.Card.ChosenApproach))
            lines.Add("Chosen: " + decision.Card.ChosenApproach.Trim());
        if (decision.Findings is { Count: > 0 })
        {
            lines.Add("Findings:");
            foreach (var f in decision.Findings.Take(4))
                lines.Add($"  · [{f.Kind}] {f.Summary}");
        }
        if (!string.Equals(decision.Status, "active", StringComparison.OrdinalIgnoreCase))
            lines.Add($"Status: {decision.Status}");
        return lines.Count == 0 ? "Decision record" : string.Join(Environment.NewLine, lines);
    }

    private static T? Deserialize<T>(string json) =>
        System.Text.Json.JsonSerializer.Deserialize<T>(json, ChatHistoryJson.Options);
}

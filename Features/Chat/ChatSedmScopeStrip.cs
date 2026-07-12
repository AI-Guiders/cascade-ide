#nullable enable
using CascadeIDE.Models.AgentChat;

namespace CascadeIDE.Features.Chat;

/// <summary>SEDM scope strip projection (T2 + T1 + decision one-liner) для UI и harness.</summary>
public sealed record ChatSedmScopeStrip(
    string? ContextOneLiner,
    string? IntentOneLiner,
    string? DecisionOneLiner,
    string? DecisionStatus,
    int OpenWorklineCount,
    bool IntentIncomplete)
{
    public static ChatSedmScopeStrip Empty { get; } = new(null, null, null, null, 1, false);

    public bool HasContent =>
        !string.IsNullOrWhiteSpace(ContextOneLiner)
        || !string.IsNullOrWhiteSpace(IntentOneLiner)
        || !string.IsNullOrWhiteSpace(DecisionOneLiner);

    public static ChatSedmScopeStrip FromProjection(
        SedmEventProjector.WorklineProjection workline,
        int openWorklineCount)
    {
        var context = FormatContextOneLiner(workline.ContextCard);
        var intent = FormatIntentOneLiner(workline.IntentCard, out var incomplete);
        var (decision, status) = FormatDecisionOneLiner(workline.ActiveDecision);
        return new ChatSedmScopeStrip(context, intent, decision, status, Math.Max(1, openWorklineCount), incomplete);
    }

    public string FormatStripText()
    {
        if (!HasContent && OpenWorklineCount <= 1)
            return "";

        var parts = new List<string>(4);
        if (OpenWorklineCount > 1)
            parts.Add($"open: {OpenWorklineCount}");

        if (!string.IsNullOrWhiteSpace(ContextOneLiner))
            parts.Add(ContextOneLiner);

        if (!string.IsNullOrWhiteSpace(IntentOneLiner))
        {
            var intent = IntentOneLiner;
            if (IntentIncomplete)
                intent += " (incomplete)";
            parts.Add("intent: " + intent);
        }

        if (!string.IsNullOrWhiteSpace(DecisionOneLiner))
        {
            var decision = DecisionOneLiner;
            if (!string.IsNullOrWhiteSpace(DecisionStatus) && !string.Equals(DecisionStatus, "active", StringComparison.OrdinalIgnoreCase))
                decision += $" [{DecisionStatus}]";
            parts.Add("decision: " + decision);
        }

        return parts.Count == 0 ? "" : string.Join(" · ", parts);
    }

    public string? BuildAgentContextPrefix()
    {
        if (!HasContent)
            return null;

        var lines = new List<string> { "[SEDM — сжатый срез активной workline]" };

        if (!string.IsNullOrWhiteSpace(ContextOneLiner))
            lines.Add("Here: " + ContextOneLiner);

        if (!string.IsNullOrWhiteSpace(IntentOneLiner))
            lines.Add("Intent: " + IntentOneLiner);

        if (!string.IsNullOrWhiteSpace(DecisionOneLiner))
        {
            var line = "Decision: " + DecisionOneLiner;
            if (!string.IsNullOrWhiteSpace(DecisionStatus) && !string.Equals(DecisionStatus, "active", StringComparison.OrdinalIgnoreCase))
                line += $" ({DecisionStatus} — re-verify before trust)";
            lines.Add(line);
        }

        if (OpenWorklineCount > 1)
            lines.Add($"Other open worklines: {OpenWorklineCount - 1} (not expanded)");

        lines.Add("---");
        return string.Join(Environment.NewLine, lines);
    }

    private static string? FormatContextOneLiner(SedmContextCardMaterializedPayload? card)
    {
        if (card is null)
            return null;

        var path = card.Anchor.Path.Trim();
        var symbol = string.IsNullOrWhiteSpace(card.Anchor.Symbol) ? null : card.Anchor.Symbol.Trim();
        var here = symbol is null ? path : $"{path}::{symbol}";

        var applies = card.Applies?.FirstOrDefault();
        if (applies is null)
            return here;

        return $"{here} · {applies.Ref} — {Truncate(applies.OneLiner, 48)}";
    }

    private static string? FormatIntentOneLiner(SedmIntentCardRecordedPayload? card, out bool incomplete)
    {
        incomplete = false;
        if (card is null)
            return null;

        incomplete = SedmCardCompleteness.IsIntentIncomplete(card);
        var outcome = card.Card.Outcome.Trim();
        var chosen = string.IsNullOrWhiteSpace(card.Card.ChosenApproach) ? null : card.Card.ChosenApproach.Trim();
        return chosen is null ? Truncate(outcome, 72) : $"{Truncate(outcome, 40)} → {Truncate(chosen, 32)}";
    }

    private static (string? OneLiner, string? Status) FormatDecisionOneLiner(SedmEventProjector.DecisionState? decision)
    {
        if (decision is null)
            return (null, null);

        var outcome = decision.Payload.Card.Outcome.Trim();
        var chosen = string.IsNullOrWhiteSpace(decision.Payload.Card.ChosenApproach)
            ? null
            : decision.Payload.Card.ChosenApproach.Trim();
        var oneLiner = chosen is null ? Truncate(outcome, 72) : $"{Truncate(outcome, 40)} → {Truncate(chosen, 32)}";
        return (oneLiner, decision.Status);
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";
}

/// <summary>Валидация полноты intent/decision card (ADR 0173 §3).</summary>
internal static class SedmCardCompleteness
{
    public static bool IsIntentIncomplete(SedmIntentCardRecordedPayload card)
    {
        if (string.IsNullOrWhiteSpace(card.Card.Outcome) || string.IsNullOrWhiteSpace(card.Card.Trigger))
            return true;

        if (string.IsNullOrWhiteSpace(card.Card.ChosenApproach))
            return false;

        if (string.IsNullOrWhiteSpace(card.Card.SelectionRationale))
            return true;

        return card.Considered is null || card.Considered.Count == 0;
    }
}

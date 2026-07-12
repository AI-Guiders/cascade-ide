#nullable enable
using System.Text;
using CascadeIDE.Models.AgentChat;

namespace CascadeIDE.Features.Chat;

/// <summary>Секция Decisions в chat_export_readable (ADR 0173).</summary>
internal static class ChatSedmReadableExport
{
    public static string? BuildDecisionsSection(IReadOnlyList<ChatHistoryEvent> events)
    {
        var projection = SedmEventProjector.Project(events, Guid.Empty, openWorklineCount: 1);
        if (projection.ByWorkline.Count == 0)
            return null;

        var sb = new StringBuilder();
        sb.AppendLine("## Decisions (SEDM)");
        var any = false;

        foreach (var pair in projection.ByWorkline.OrderBy(static kv => kv.Key, StringComparer.Ordinal))
        {
            var wl = pair.Value;
            if (wl.IntentCard is null && wl.DecisionHistory.Count == 0)
                continue;

            any = true;
            sb.AppendLine();
            sb.AppendLine($"### Workline {pair.Key}");

            if (wl.IntentCard is not null)
            {
                sb.AppendLine("- **Intent (operator)**");
                AppendCard(sb, wl.IntentCard.Card, wl.IntentCard.Considered);
            }

            foreach (var decision in wl.DecisionHistory)
            {
                sb.AppendLine($"- **Decision ({decision.Status})**");
                AppendCard(sb, decision.Payload.Card, decision.Payload.Considered);
                if (decision.Payload.Findings is { Count: > 0 })
                {
                    sb.AppendLine("  - Findings:");
                    foreach (var finding in decision.Payload.Findings)
                        sb.AppendLine($"    - [{finding.Kind}] {finding.Ref}: {finding.Summary}");
                }
            }
        }

        return any ? sb.ToString().TrimEnd() : null;
    }

    private static void AppendCard(
        StringBuilder sb,
        SedmIntentCardBodyPayload card,
        IReadOnlyList<SedmIntentConsideredOptionPayload>? considered)
    {
        if (!string.IsNullOrWhiteSpace(card.Outcome))
            sb.AppendLine($"  - Outcome: {card.Outcome.Trim()}");
        if (!string.IsNullOrWhiteSpace(card.ChosenApproach))
            sb.AppendLine($"  - Chosen: {card.ChosenApproach.Trim()}");
        if (!string.IsNullOrWhiteSpace(card.SelectionRationale))
            sb.AppendLine($"  - Rationale: {card.SelectionRationale.Trim()}");
        if (considered is { Count: > 0 })
        {
            sb.AppendLine("  - Rejected:");
            foreach (var option in considered)
                sb.AppendLine($"    - {option.Approach}: {option.RejectedBecause}");
        }
    }
}

#nullable enable
using System.Text;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>Локальные текстовые отчёты Intercom (<c>kind=report</c>, <c>report_handler</c>).</summary>
public static class ChatSlashSessionReports
{
    public static string? TryFormat(string slashPath, ChatSurfaceSnapshot snapshot)
    {
        if (!IntentSlashCatalog.TryGetRoute(slashPath, out var route)
            || route.ExecutionKind != ChatSlashCommandExecutionKind.LocalReport)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(route.ReportHandlerId)
            || !ChatSlashReportHandlers.TryFormat(route.ReportHandlerId, snapshot, out var text))
        {
            return $"Неизвестный отчёт: {slashPath}";
        }

        return text;
    }

    public static string FormatTopicList(ChatSurfaceSnapshot snapshot) =>
        ChatThreadPresentation.FormatTopicList(snapshot);

    public static string FormatTopicTree(ChatSurfaceSnapshot snapshot) =>
        ChatThreadPresentation.FormatTopicTree(snapshot);

    public static string FormatSpineList(ChatProductSpine spine)
    {
        if (!spine.HasContent)
            return "Spine пуст. Задай фокус в classic Intercom или /spine set <фокус>.";

        var title = ChatProductSpinePresentation.ResolveLineTitle(spine);
        var lines = new List<string> { $"Spine · {title}" };
        if (!string.IsNullOrWhiteSpace(spine.CurrentFocus))
            lines.Add("  Фокус: " + spine.CurrentFocus.Trim());

        if (spine.Milestones.Count > 0)
        {
            lines.Add($"  Вехи ({spine.Milestones.Count}):");
            foreach (var milestone in spine.Milestones)
            {
                if (!string.IsNullOrWhiteSpace(milestone))
                    lines.Add("    • " + milestone.Trim());
            }
        }
        else
        {
            lines.Add("  Вехи: —");
        }

        lines.Add("  " + ChatProductSpinePresentation.FormatAgentContextFooter(spine.IncludeInAgentContext));
        return string.Join(Environment.NewLine, lines);
    }

    public static string FormatSpineTree(ChatProductSpine spine)
    {
        if (!spine.HasContent)
            return "Spine пуст. Задай фокус в classic Intercom или /spine set <фокус>.";

        var title = ChatProductSpinePresentation.ResolveLineTitle(spine);
        var sb = new StringBuilder();
        sb.AppendLine(title);
        var focus = string.IsNullOrWhiteSpace(spine.CurrentFocus)
            ? "—"
            : spine.CurrentFocus.Trim();
        sb.AppendLine("└─ Фокус: " + focus);

        var milestones = spine.Milestones
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Select(m => m.Trim())
            .ToList();
        if (milestones.Count == 0)
            return sb.ToString().TrimEnd();

        for (var i = 0; i < milestones.Count; i++)
        {
            var branch = i == milestones.Count - 1 ? "   └─ " : "   ├─ ";
            sb.AppendLine(branch + milestones[i]);
        }

        var ctx = ChatProductSpinePresentation.FormatAgentContextFooter(spine.IncludeInAgentContext);
        sb.Append("   (" + ctx + ")");
        return sb.ToString().TrimEnd();
    }

    public static string FormatSedmScope(ChatSedmScopeStrip strip)
    {
        var text = strip.FormatStripText();
        return string.IsNullOrWhiteSpace(text)
            ? "SEDM scope strip пуст. Прикрепи файл или зафиксируй intent/decision."
            : "SEDM scope · " + text;
    }

    public static string FormatSedmScopeDetail(ChatSurfaceSnapshot snapshot)
    {
        var strip = snapshot.SedmScopeStrip;
        var lines = new List<string> { "SEDM · активная workline" };
        if (strip.OpenWorklineCount > 1)
            lines.Add($"Open worklines: {strip.OpenWorklineCount}");
        if (!string.IsNullOrWhiteSpace(strip.ContextOneLiner))
            lines.Add("Context: " + strip.ContextOneLiner);
        if (!string.IsNullOrWhiteSpace(strip.IntentOneLiner))
            lines.Add("Intent: " + strip.IntentOneLiner + (strip.IntentIncomplete ? " (incomplete)" : ""));
        if (!string.IsNullOrWhiteSpace(strip.DecisionOneLiner))
            lines.Add("Decision: " + strip.DecisionOneLiner + (string.IsNullOrWhiteSpace(strip.DecisionStatus) ? "" : $" [{strip.DecisionStatus}]"));

        var timeline = snapshot.Layout.Lanes
            .SelectMany(l => l.Entries)
            .Where(e => e.Kind == ChatSurfaceEntryKind.SedmCard)
            .ToList();
        if (timeline.Count > 0)
        {
            lines.Add($"Timeline cards: {timeline.Count}");
            foreach (var card in timeline.Take(6))
                lines.Add($"  · {card.Title}: {Truncate(card.Body, 72)}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";
}

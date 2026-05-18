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
}

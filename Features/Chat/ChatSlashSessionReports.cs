#nullable enable
using System.Text;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>Локальные текстовые отчёты Intercom: /topic list|tree, /spine list|tree (без MCP).</summary>
public static class ChatSlashSessionReports
{
    public static string? TryFormat(string slashPath, ChatSurfaceSnapshot snapshot)
    {
        if (!IntentSlashCatalog.TryGetRoute(slashPath, out var route)
            || route.ExecutionKind != ChatSlashCommandExecutionKind.LocalReport)
        {
            return null;
        }

        return slashPath.ToLowerInvariant() switch
        {
            "/topic list" => FormatTopicList(snapshot),
            "/topic tree" => FormatTopicTree(snapshot),
            "/spine list" => FormatSpineList(snapshot.ProductSpine),
            "/spine tree" => FormatSpineTree(snapshot.ProductSpine),
            _ => $"Неизвестный отчёт: {slashPath}",
        };
    }

    public static string FormatTopicList(ChatSurfaceSnapshot snapshot)
    {
        var threads = snapshot.State.Threads;
        if (threads.Count == 0)
            return "Тем пока нет. Отправь сообщение или /card <заголовок>.";

        var msgCounts = MessageCountsByThread(snapshot);
        var lines = new List<string> { $"Темы сессии ({threads.Count}):" };
        foreach (var thread in threads.OrderBy(t => t.Order))
        {
            msgCounts.TryGetValue(thread.ThreadId, out var count);
            lines.Add(FormatTopicListLine(thread, count, thread.Depth));
        }

        return string.Join(Environment.NewLine, lines);
    }

    public static string FormatTopicTree(ChatSurfaceSnapshot snapshot)
    {
        var threads = snapshot.State.Threads;
        if (threads.Count == 0)
            return "Тем пока нет. Отправь сообщение или /card <заголовок>.";

        var byId = threads.ToDictionary(t => t.ThreadId);
        var msgCounts = MessageCountsByThread(snapshot);
        var roots = threads
            .Where(t => t.ParentThreadId is null || !byId.ContainsKey(t.ParentThreadId.Value))
            .OrderBy(t => t.Order)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"Дерево тем ({threads.Count}):");
        for (var i = 0; i < roots.Count; i++)
            AppendTopicTreeNode(sb, roots[i], byId, msgCounts, prefix: "", isLast: i == roots.Count - 1);

        return sb.ToString().TrimEnd();
    }

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

    private static string FormatTopicListLine(ChatThreadNode thread, int messageCount, int depth)
    {
        var indent = new string(' ', depth * 2);
        var flags = new List<string>();
        if (thread.IsMainThread)
            flags.Add("main");
        if (thread.IsActive)
            flags.Add("active");
        var flagText = flags.Count == 0 ? "" : " [" + string.Join(", ", flags) + "]";
        var shortId = thread.ThreadId.ToString("N")[..8];
        return $"{indent}• {thread.Title}{flagText} — {messageCount} сообщ. — {shortId}";
    }

    private static void AppendTopicTreeNode(
        StringBuilder sb,
        ChatThreadNode node,
        IReadOnlyDictionary<Guid, ChatThreadNode> byId,
        IReadOnlyDictionary<Guid, int> msgCounts,
        string prefix,
        bool isLast)
    {
        msgCounts.TryGetValue(node.ThreadId, out var count);
        var branch = isLast ? "└─ " : "├─ ";
        var flags = new List<string>();
        if (node.IsMainThread)
            flags.Add("main");
        if (node.IsActive)
            flags.Add("active");
        var flagText = flags.Count == 0 ? "" : " [" + string.Join(", ", flags) + "]";
        sb.AppendLine($"{prefix}{branch}{node.Title}{flagText} ({count} сообщ.)");

        var childPrefix = prefix + (isLast ? "   " : "│  ");
        var children = byId.Values
            .Where(t => t.ParentThreadId == node.ThreadId)
            .OrderBy(t => t.Order)
            .ToList();
        for (var i = 0; i < children.Count; i++)
            AppendTopicTreeNode(sb, children[i], byId, msgCounts, childPrefix, i == children.Count - 1);
    }

    private static Dictionary<Guid, int> MessageCountsByThread(ChatSurfaceSnapshot snapshot)
    {
        var counts = snapshot.State.Messages
            .GroupBy(m => m.ThreadId)
            .ToDictionary(g => g.Key, g => g.Count());
        foreach (var lane in snapshot.Layout.Lanes)
        {
            var n = lane.Entries.Count(e => e.Kind == ChatSurfaceEntryKind.Message);
            if (!counts.TryGetValue(lane.Thread.ThreadId, out var existing) || n > existing)
                counts[lane.Thread.ThreadId] = n;
        }

        foreach (var thread in snapshot.State.Threads)
            counts.TryAdd(thread.ThreadId, 0);

        return counts;
    }
}

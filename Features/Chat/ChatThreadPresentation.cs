#nullable enable
using System.Text;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>Общая презентация тем (текстовые отчёты, picker, autocomplete).</summary>
public static class ChatThreadPresentation
{
    public const string EmptyTopicsHint =
        "Тем пока нет. Отправь сообщение или /intercom topic create <название>.";

    public sealed record PickerRow(Guid ThreadId, string Title, string Meta, int Depth);

    public static Dictionary<Guid, int> MessageCountsByThread(ChatSurfaceSnapshot snapshot)
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

    public static string FormatFlags(ChatThreadNode thread)
    {
        var flags = new List<string>();
        if (thread.IsMainThread)
            flags.Add("main");
        if (thread.IsActive)
            flags.Add("active");
        return flags.Count == 0 ? "" : string.Join(", ", flags);
    }

    public static string FormatMeta(ChatThreadNode thread, IReadOnlyDictionary<Guid, int> messageCounts)
    {
        messageCounts.TryGetValue(thread.ThreadId, out var count);
        var flags = FormatFlags(thread);
        var shortId = thread.ThreadId.ToString("N")[..8];
        return string.IsNullOrEmpty(flags)
            ? $"{count} · {shortId}"
            : $"{flags} · {count} · {shortId}";
    }

    public static string FormatListLine(ChatThreadNode thread, int messageCount, int depth)
    {
        var indent = new string(' ', depth * 2);
        var flags = FormatFlags(thread);
        var flagText = flags.Length == 0 ? "" : " [" + flags + "]";
        var shortId = thread.ThreadId.ToString("N")[..8];
        return $"{indent}• {thread.Title}{flagText} — {messageCount} сообщ. — {shortId}";
    }

    public static string FormatTopicList(ChatSurfaceSnapshot snapshot)
    {
        var threads = snapshot.State.Threads;
        if (threads.Count == 0)
            return EmptyTopicsHint;

        var msgCounts = MessageCountsByThread(snapshot);
        var lines = new List<string> { $"Темы сессии ({threads.Count}):" };
        foreach (var thread in threads.OrderBy(t => t.Order))
        {
            msgCounts.TryGetValue(thread.ThreadId, out var count);
            lines.Add(FormatListLine(thread, count, thread.Depth));
        }

        return string.Join(Environment.NewLine, lines);
    }

    public static string FormatTopicTree(ChatSurfaceSnapshot snapshot)
    {
        var threads = snapshot.State.Threads;
        if (threads.Count == 0)
            return EmptyTopicsHint;

        var byId = threads.ToDictionary(t => t.ThreadId);
        var msgCounts = MessageCountsByThread(snapshot);
        var roots = threads
            .Where(t => t.ParentThreadId is null || !byId.ContainsKey(t.ParentThreadId.Value))
            .OrderBy(t => t.Order)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"Дерево тем ({threads.Count}):");
        for (var i = 0; i < roots.Count; i++)
            AppendTreeNode(sb, roots[i], byId, msgCounts, prefix: "", isLast: i == roots.Count - 1);

        return sb.ToString().TrimEnd();
    }

    /// <summary>Строки Topic Navigator (дерево + опциональный фильтр по заголовку, ADR 0127-E).</summary>
    public static IReadOnlyList<PickerRow> BuildNavigatorRows(
        IReadOnlyList<ChatThreadNode> threads,
        IReadOnlyDictionary<Guid, int> messageCounts,
        string? searchQuery = null)
    {
        var rows = BuildPickerRows(TopicPickerPresentation.Tree, threads, messageCounts);
        return FilterNavigatorRows(rows, searchQuery);
    }

    public static IReadOnlyList<PickerRow> FilterNavigatorRows(
        IReadOnlyList<PickerRow> rows,
        string? searchQuery)
    {
        var q = searchQuery?.Trim() ?? "";
        if (q.Length == 0)
            return rows;

        return rows
            .Where(r => r.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                        || r.Meta.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static IReadOnlyList<PickerRow> BuildPickerRows(
        TopicPickerPresentation mode,
        IReadOnlyList<ChatThreadNode> threads,
        IReadOnlyDictionary<Guid, int> messageCounts)
    {
        if (threads.Count == 0)
            return [];

        if (mode == TopicPickerPresentation.List)
        {
            return threads
                .OrderBy(t => t.Order)
                .Select(t => new PickerRow(t.ThreadId, t.Title, FormatMeta(t, messageCounts), t.Depth))
                .ToList();
        }

        var byId = threads.ToDictionary(t => t.ThreadId);
        var roots = threads
            .Where(t => t.ParentThreadId is null || !byId.ContainsKey(t.ParentThreadId.Value))
            .OrderBy(t => t.Order)
            .ToList();

        var rows = new List<PickerRow>();
        foreach (var root in roots)
            AppendPickerTreeNode(rows, root, byId, messageCounts, depth: 0, isLast: true, prefix: "");
        return rows;
    }

    public static IEnumerable<ChatThreadNode> RankThreadsForCompletion(
        IReadOnlyList<ChatThreadNode> threads,
        string prefix)
    {
        if (threads.Count == 0)
            return [];

        if (prefix.Length == 0)
        {
            return threads
                .OrderBy(t => t.IsActive ? 0 : 1)
                .ThenBy(t => t.Title, StringComparer.OrdinalIgnoreCase);
        }

        return threads
            .Select(t => (t, RankPrefix(prefix, t)))
            .Where(x => x.Item2 < int.MaxValue)
            .OrderBy(x => x.Item2)
            .ThenBy(x => x.t.Title, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.t);
    }

    private static int RankPrefix(string prefix, ChatThreadNode thread)
    {
        var id = thread.ThreadId.ToString("N");
        if (id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return 0;
        if (thread.Title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return 1;
        if (thread.Title.Contains(prefix, StringComparison.OrdinalIgnoreCase))
            return 2;
        return int.MaxValue;
    }

    private static void AppendTreeNode(
        StringBuilder sb,
        ChatThreadNode node,
        IReadOnlyDictionary<Guid, ChatThreadNode> byId,
        IReadOnlyDictionary<Guid, int> msgCounts,
        string prefix,
        bool isLast)
    {
        msgCounts.TryGetValue(node.ThreadId, out var count);
        var branch = isLast ? "└─ " : "├─ ";
        var flags = FormatFlags(node);
        var flagText = flags.Length == 0 ? "" : " [" + flags + "]";
        sb.AppendLine($"{prefix}{branch}{node.Title}{flagText} ({count} сообщ.)");

        var childPrefix = prefix + (isLast ? "   " : "│  ");
        var children = byId.Values
            .Where(t => t.ParentThreadId == node.ThreadId)
            .OrderBy(t => t.Order)
            .ToList();
        for (var i = 0; i < children.Count; i++)
            AppendTreeNode(sb, children[i], byId, msgCounts, childPrefix, i == children.Count - 1);
    }

    private static void AppendPickerTreeNode(
        List<PickerRow> rows,
        ChatThreadNode node,
        IReadOnlyDictionary<Guid, ChatThreadNode> byId,
        IReadOnlyDictionary<Guid, int> messageCounts,
        int depth,
        bool isLast,
        string prefix)
    {
        rows.Add(new PickerRow(node.ThreadId, prefix + node.Title, FormatMeta(node, messageCounts), depth));
        var children = byId.Values
            .Where(t => t.ParentThreadId == node.ThreadId)
            .OrderBy(t => t.Order)
            .ToList();
        for (var i = 0; i < children.Count; i++)
        {
            var childPrefix = prefix + (isLast ? "  " : "│ ");
            AppendPickerTreeNode(rows, children[i], byId, messageCounts, depth + 1, i == children.Count - 1, childPrefix);
        }
    }
}

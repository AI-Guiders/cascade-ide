#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Разрешение темы по заголовку или короткому id (для /topic open).</summary>
public static class ChatSlashTopicResolver
{
    public static bool TryResolve(
        string? query,
        ChatSurfaceSnapshot snapshot,
        Guid selectedThreadId,
        out ChatThreadNode thread,
        out string? error)
    {
        thread = default!;
        error = null;

        var threads = snapshot.State.Threads;
        if (threads.Count == 0)
        {
            error = "Тем пока нет.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            if (TryPickDefault(threads, selectedThreadId, snapshot.State.ActiveThreadId, out thread))
                return true;

            error = "Укажи тему: /topic open <заголовок или id>";
            return false;
        }

        var q = query.Trim();
        if (Guid.TryParse(q, out var fullId))
        {
            var byGuid = threads.FirstOrDefault(t => t.ThreadId == fullId);
            if (byGuid is not null)
            {
                thread = byGuid;
                return true;
            }

            error = "Тема не найдена по id: " + q;
            return false;
        }

        if (q.Length >= 4 && q.All(Uri.IsHexDigit))
        {
            var byPrefix = threads
                .Where(t => t.ThreadId.ToString("N").StartsWith(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (byPrefix.Count == 1)
            {
                thread = byPrefix[0];
                return true;
            }

            if (byPrefix.Count > 1)
            {
                error = FormatAmbiguous(byPrefix.Select(t => t.Title));
                return false;
            }
        }

        var exact = threads
            .Where(t => string.Equals(t.Title, q, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (exact.Count == 1)
        {
            thread = exact[0];
            return true;
        }

        if (exact.Count > 1)
        {
            error = FormatAmbiguous(exact.Select(t => t.Title));
            return false;
        }

        var contains = threads
            .Where(t => t.Title.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (contains.Count == 1)
        {
            thread = contains[0];
            return true;
        }

        if (contains.Count > 1)
        {
            error = FormatAmbiguous(contains.Select(t => t.Title));
            return false;
        }

        error = "Тема не найдена: " + q;
        return false;
    }

    private static bool TryPickDefault(
        IReadOnlyList<ChatThreadNode> threads,
        Guid selectedThreadId,
        Guid activeThreadId,
        out ChatThreadNode thread)
    {
        thread = default!;
        if (selectedThreadId != Guid.Empty)
        {
            var selected = threads.FirstOrDefault(t => t.ThreadId == selectedThreadId);
            if (selected is not null)
            {
                thread = selected;
                return true;
            }
        }

        if (activeThreadId != Guid.Empty)
        {
            var active = threads.FirstOrDefault(t => t.ThreadId == activeThreadId);
            if (active is not null)
            {
                thread = active;
                return true;
            }
        }

        var main = threads.FirstOrDefault(t => t.IsMainThread) ?? threads[0];
        thread = main;
        return true;
    }

    private static string FormatAmbiguous(IEnumerable<string> titles)
    {
        var list = titles.ToList();
        var shown = string.Join("; ", list.Take(5));
        var suffix = list.Count > 5 ? "…" : "";
        return "Несколько тем подходят: " + shown + suffix + ". Уточни заголовок или id.";
    }
}

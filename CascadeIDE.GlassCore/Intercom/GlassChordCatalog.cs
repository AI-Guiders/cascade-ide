#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>Glass-local Ctrl+K melody aliases (Avalonia-free). Full CIDE intent-catalog stays in host.</summary>
public sealed record GlassChordEntry(string Alias, string Title, string ActionId, string Help);

public static class GlassChordCatalog
{
    static readonly GlassChordEntry[] Entries =
    [
        new("of", "Open file", "open_file", "Open a file in AvalonEdit"),
        new("sf", "Save file", "save_file", "Save the open editor buffer"),
        new("fc", "Focus composer", "focus_composer", "Focus Intercom message box"),
        new("h", "Slash /help", "slash_help", "List Glass slash commands"),
        new("st", "Slash /status", "slash_status", "Glass session / latch status"),
        new("tp", "Slash /topics", "slash_topics", "List Intercom topics"),
        new("lt", "Slash /letter", "slash_letter", "Agent Who letter links"),
        new("ta", "Topics: All", "topics_all", "Clear topic filter"),
        new("me", "MFD Editor", "mfd_editor", "Select MFD Editor page"),
        new("mt", "MFD Terminal", "mfd_terminal", "Select MFD Terminal page"),
        new("pq", "Command palette", "palette", "Open Ctrl+Q palette"),
    ];

    public static string Normalize(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "";
        Span<char> buf = stackalloc char[raw.Length];
        var n = 0;
        foreach (var c in raw)
        {
            if (c is >= 'A' and <= 'Z')
                buf[n++] = (char)(c + 32);
            else if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
                buf[n++] = c;
        }

        return n == 0 ? "" : new string(buf[..n]);
    }

    public static IReadOnlyList<GlassChordEntry> Filter(string? query)
    {
        var q = Normalize(query);
        if (q.Length == 0)
            return Entries;

        // Melody match = alias prefix only (not title search — that is Ctrl+Q).
        return Entries
            .Where(e => e.Alias.StartsWith(q, StringComparison.Ordinal))
            .ToArray();
    }

    public static GlassChordEntry? Exact(string? query)
    {
        var q = Normalize(query);
        if (q.Length == 0)
            return null;
        return Entries.FirstOrDefault(e => e.Alias.Equals(q, StringComparison.Ordinal));
    }
}

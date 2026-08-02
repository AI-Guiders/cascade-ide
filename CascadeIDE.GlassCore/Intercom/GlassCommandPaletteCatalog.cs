#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>Glass-local Ctrl+Q command list (Avalonia-free). Full CIDE palette stays in host.</summary>
public sealed record GlassPaletteEntry(string Id, string Title, string Help, string? Keywords = null);

public static class GlassCommandPaletteCatalog
{
    static readonly GlassPaletteEntry[] Entries =
    [
        new("open_file", "Open file…", "Open a file in AvalonEdit", "ctrl+o file open"),
        new("save_file", "Save file", "Save the open editor buffer", "ctrl+s save"),
        new("focus_composer", "Focus Intercom composer", "Move keyboard focus to the message box", "chat composer intercom"),
        new("slash_help", "Slash: /help", "List Glass slash commands", "/help slash"),
        new("slash_status", "Slash: /status", "Glass session / latch status", "/status"),
        new("slash_topics", "Slash: /topics", "List Intercom topics", "/topics"),
        new("slash_letter", "Slash: /letter", "Agent Who letter canon links", "/letter manifesto"),
        new("slash_fds", "Slash: /fds", "Open Flight Data Storage MFD shelf", "/fds fds flight data"),
        new("slash_attach", "Slash: /attach", "Insert [path:line] chip from editor selection", "/attach chip adr0128"),
        new("slash_open", "Slash: /open", "Open path[:line] in AvalonEdit", "/open path line"),
        new("topics_all", "Topics: All", "Clear topic filter — full Virtual History", "topics all"),
        new("mfd_editor", "MFD: Editor", "Select MFD Editor page", "mfd editor"),
        new("mfd_terminal", "MFD: Terminal", "Select MFD Terminal page", "mfd terminal"),
        new("mfd_fds", "MFD: Flight Data Storage", "Partner shelf — plans/reports/pressure", "mfd fds flight data storage shelf"),
        new("mfd_build", "MFD: Build", "Select MFD Build redirected log", "mfd build"),
        new("mfd_tests", "MFD: Tests", "Select MFD Tests redirected log", "mfd tests"),
    ];

    public static IReadOnlyList<GlassPaletteEntry> Filter(string? query)
    {
        var q = (query ?? "").Trim();
        if (q.Length == 0)
            return Entries;

        return Entries
            .Where(e =>
                e.Id.Contains(q, StringComparison.OrdinalIgnoreCase)
                || e.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                || e.Help.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (e.Keywords?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToArray();
    }
}

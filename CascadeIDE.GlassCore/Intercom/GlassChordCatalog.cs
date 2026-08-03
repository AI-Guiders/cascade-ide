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
        new("fd", "MFD FDS", "mfd_fds", "Flight Data Storage shelf"),
        new("at", "Slash /attach", "slash_attach", "Attach chip from editor selection"),
        new("op", "Slash /open", "slash_open", "Open path[:line] via slash"),
        new("cz", "Slash /citizen", "slash_citizen", "Habitat citizen dialog"),
        new("ta", "Topics: All", "topics_all", "Clear topic filter"),
        new("me", "MFD Editor", "mfd_editor", "Select MFD Editor page"),
        new("mt", "MFD Terminal", "mfd_terminal", "Select MFD Terminal page"),
        new("mb", "MFD Build", "mfd_build", "Select MFD Build page"),
        new("ms", "MFD Tests", "mfd_tests", "Select MFD Tests page"),
        new("mg", "MFD Git", "mfd_git", "Select MFD Git page"),
        new("mp", "MFD Problems", "mfd_problems", "Select MFD Problems host"),
        new("rf", "MFD RelatedFiles", "mfd_related_files", "Select MFD RelatedFiles host"),
        new("sm", "MFD SemanticMap", "mfd_semantic_map", "Select MFD SemanticMap host"),
        new("cr", "MFD Correspondence", "mfd_correspondence", "Select MFD Correspondence host"),
        new("md", "MFD Markdown", "mfd_markdown", "Select MFD MarkdownPreview host"),
        new("ds", "MFD DebugStack", "mfd_debug_stack", "Select MFD DebugStack host"),
        new("wa", "MFD WebAi", "mfd_webai", "Select MFD WebAiPortal host"),
        new("sx", "MFD SolutionExplorer", "mfd_solution_explorer", "Solution tree / workspace glance"),
        new("hi", "MFD HybridIndex", "mfd_hybrid_index", "Hybrid codebase index glance"),
        new("wh", "MFD WorkspaceHealth", "mfd_workspace_health", "Workspace health glance"),
        new("er", "MFD EnvReady", "mfd_env_ready", "Environment readiness glance"),
        new("ev", "MFD Events", "mfd_events", "Events / latch catalog glance"),
        new("hy", "MFD Hypotheses", "mfd_hypotheses", "Debug hypotheses glance"),
        new("ic", "MFD Chat", "mfd_chat", "Intercom presence MFD glance"),
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

    /// <summary>Melody-tail suggestions for AwaitMelodyTail overlay (intent-catalog SSOT).</summary>
    public static IReadOnlyList<GlassChordMelodyEntry> FilterMelodyTail(string? tailNormalized) =>
        GlassChordMelody.FilterSuggestions(GlassChordMelody.NormalizeInput(tailNormalized));
}

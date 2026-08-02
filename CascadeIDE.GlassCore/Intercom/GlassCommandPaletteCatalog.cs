#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>Glass-local Ctrl+Q command list (Avalonia-free). Full CIDE palette stays in host.</summary>
public sealed record GlassPaletteEntry(string Id, string Title, string Help, string? Keywords = null);

public static class GlassCommandPaletteCatalog
{
    public const string MelodyHintId = "melody_hint";
    public const string MelodyNoMatchId = "melody_no_match";

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
        new("slash_citizen", "Slash: /citizen", "Talk to habitat citizen (dialog peer)", "/citizen dialog habitat"),
        new("topics_all", "Topics: All", "Clear topic filter — full Virtual History", "topics all"),
        new("mfd_editor", "MFD: Editor", "Select MFD Editor page", "mfd editor"),
        new("mfd_terminal", "MFD: Terminal", "Select MFD Terminal page", "mfd terminal"),
        new("mfd_fds", "MFD: Flight Data Storage", "Partner shelf — plans/reports/pressure", "mfd fds flight data storage shelf"),
        new("mfd_build", "MFD: Build", "Select MFD Build redirected log", "mfd build"),
        new("mfd_tests", "MFD: Tests", "Select MFD Tests redirected log", "mfd tests"),
        new("mfd_git", "MFD: Git", "Select MFD Git redirected status", "mfd git status scm"),
        new("mfd_solution_explorer", "MFD: SolutionExplorer", "Solution tree / workspace glance", "mfd solution explorer tree"),
        new("mfd_hybrid_index", "MFD: HybridIndex", "Hybrid codebase index glance", "mfd hybrid index hci"),
        new("mfd_workspace_health", "MFD: WorkspaceHealth", "Workspace health glance", "mfd workspace health wh"),
        new("mfd_env_ready", "MFD: EnvironmentReadiness", "Environment readiness glance", "mfd env ready environment"),
        new("mfd_events", "MFD: Events", "Events / latch catalog glance", "mfd events latch"),
        new("mfd_hypotheses", "MFD: Hypotheses", "Debug hypotheses glance", "mfd hypotheses debug"),
        new("mfd_chat", "MFD: Chat", "Intercom presence MFD glance", "mfd chat intercom presence"),
    ];

    /// <summary>CIDE Command Melody prefix <c>c:</c> (ADR 0060) — Glass discoverability peel.</summary>
    public static bool TryGetMelodyTail(string? raw, out string tailNormalized)
    {
        tailNormalized = "";
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var t = raw.TrimStart();
        if (t.Length < 2 || char.ToLowerInvariant(t[0]) != 'c' || t[1] != ':')
            return false;

        tailNormalized = t.Length > 2 ? t[2..].Trim().ToLowerInvariant() : "";
        return true;
    }

    public static bool IsNonExecutableMelodyRow(string id) =>
        id is MelodyHintId or MelodyNoMatchId;

    public static IReadOnlyList<GlassPaletteEntry> Filter(string? query)
    {
        if (TryGetMelodyTail(query, out var tail))
            return FilterMelody(tail);

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

    /// <summary>
    /// <c>c:</c> → Glass chord aliases with Help (discoverability).
    /// Full Avalonia <c>intent-catalog.toml</c> MelodyInterpreter remains a later peel.
    /// </summary>
    public static IReadOnlyList<GlassPaletteEntry> FilterMelody(string tailNormalized)
    {
        var chords = GlassChordCatalog.Filter(tailNormalized)
            .Where(c => c.ActionId != "palette")
            .ToArray();

        if (string.IsNullOrEmpty(tailNormalized))
        {
            var samples = string.Join(", ", GlassChordCatalog.Filter("").Take(6).Select(c => "c:" + c.Alias));
            var hint = new GlassPaletteEntry(
                MelodyHintId,
                $"Command Melody: type tail after c: (e.g. {samples}).",
                "Discoverability — Glass chord aliases with Help. Full CIDE intent-catalog = next peel.",
                "c: melody intent");
            return
            [
                hint,
                ..chords.Select(ToMelodyEntry),
            ];
        }

        if (chords.Length == 0)
        {
            return
            [
                new GlassPaletteEntry(
                    MelodyNoMatchId,
                    "Нет alias для этого хвоста",
                    "c:",
                    null),
            ];
        }

        return chords.Select(ToMelodyEntry).ToArray();
    }

    static GlassPaletteEntry ToMelodyEntry(GlassChordEntry c) =>
        new(c.ActionId, $"c:{c.Alias} — {c.Title}", c.Help, c.Alias);
}

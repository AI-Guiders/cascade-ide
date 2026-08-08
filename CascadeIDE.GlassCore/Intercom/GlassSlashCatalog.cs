#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>Glass-local slash catalog (Avalonia-free). ArgTailKind = ADR 0150; segment steps + ADR 0125 path dynamics.</summary>
public sealed record GlassSlashSuggestion(string InsertText, string Title, string Help);

public static class GlassSlashCatalog
{
    public const int DefaultWorkspaceFileSuggestionLimit = 30;

    /// <summary>ADR 0150: none | optional | required — autocomplete Enter vs execute.</summary>
    public enum ArgTailKind
    {
        None,
        Optional,
        Required
    }

    public sealed record Command(string Id, string Path, string Help, ArgTailKind ArgTail = ArgTailKind.None);

    /// <summary>ADR 0125: optional workspace file matches for required path tails (/open, attach file).</summary>
    public delegate IReadOnlyList<(string InsertPath, string Help)> WorkspaceFileMatchSource(
        string pathPrefix,
        int limit);

    static readonly Command[] Commands =
    [
        new("help", "/help", "List Glass slash commands"),
        new("status", "/status", "Glass session / latch status line"),
        new("topics", "/topics", "List topic cards (30m gap) · /topics N to select", ArgTailKind.Optional),
        new("fds", "/fds", "Open Flight Data Storage MFD (plans/reports/notes)"),
        new("open", "/open", "Open path[:line] in AvalonEdit (thin attach↔code)", ArgTailKind.Required),
        new("file_open", "/file open", "Open file path in editor (ADR 0125)", ArgTailKind.Required),
        new("file_pick", "/file pick", "Open file dialog"),
        new("file_save", "/file save", "Save open editor buffer"),
        new("solution_open", "/solution open", "Open .sln / .csproj dialog"),
        new("solution_load", "/solution load", "Load .sln / .csproj / folder by path", ArgTailKind.Required),
        new("solution_new", "/solution new", "Create new solution (optional template)", ArgTailKind.Optional),
        new("solution_explorer_show", "/solution explorer show", "Show Solution Explorer MFD"),
        new("folder_open", "/folder open", "Open folder as workspace"),
        new("search", "/search", "Search workspace text → FindDesk", ArgTailKind.Required),
        new("attach", "/attach", "Insert [path:line] chip from editor selection (ADR 0128 thin)", ArgTailKind.Optional),
        new("attach_selection", "/intercom attach selection", "Attach chip from AvalonEdit selection (alias of bare /attach)"),
        new("attach_file", "/intercom attach file", "Attach chip from path[:line[-line]]", ArgTailKind.Required),
        new("attach_scope", "/intercom attach scope", "Honest refuse — Glass has no Roslyn caret scope yet (DIG REJECT SoftFL)"),
        new("citizen", "/citizen", "Talk to habitat citizen (dialog peer) — not guest @PF", ArgTailKind.Required),
        new("letter", "/letter", "Where the Agent Who letter lives (CDP canon)"),
        new("topic_overview", "/intercom overview", "Open topic cards overview (30m clusters)"),
        new("topic_cards", "/intercom topic cards", "Alias of /intercom overview"),
        new("topic_open", "/intercom topic open", "Enter focused topic · optional N ordinal", ArgTailKind.Optional),
        new("topic_next", "/intercom topic next", "Next topic card"),
        new("topic_prev", "/intercom topic prev", "Previous topic card"),
        new("spine_show", "/intercom spine show", "Show product spine strip (ADR 0096 latch)"),
        new("spine_toggle", "/intercom spine toggle", "Toggle product spine strip visibility"),
        new("message_find", "/intercom message find", "Find feed msgs by [path:line] chip (A4 denser thin)", ArgTailKind.Optional),
        new("message_relate", "/intercom message relate", "Honest refuse — Glass has no message↔code relate peel yet (Avalonia ADR 0137)", ArgTailKind.Optional),
        new("message_anchors", "/intercom message anchors", "List attach chips on selected (or all) feed msgs", ArgTailKind.Optional),
        new("select", "/intercom message select", "Select #N · N:M · [3;5] [8;15] · clear (ADR 0136/0150)", ArgTailKind.Required),
        new("message_next", "/intercom message next", "Select next feed message (ordinal)"),
        new("message_prev", "/intercom message prev", "Select previous feed message (ordinal)"),
    ];

    public static bool IsSlashLine(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        return raw.TrimStart()[0] == '/';
    }

    public static string ComposerInsert(Command cmd) =>
        cmd.ArgTail == ArgTailKind.None
            ? cmd.Path
            : cmd.Id == "select" ? "/select " : cmd.Path.TrimEnd() + " ";

    public static IReadOnlyList<GlassSlashSuggestion> Suggest(
        string? raw,
        WorkspaceFileMatchSource? workspaceFiles = null,
        int workspaceFileLimit = DefaultWorkspaceFileSuggestionLimit)
    {
        if (!IsSlashLine(raw))
            return [];

        var line = raw!.TrimStart();
        var body = line[1..];
        var endsWithSpace = body.Length > 0 && char.IsWhiteSpace(body[^1]);
        body = body.TrimStart();
        var tokens = body.Length == 0
            ? Array.Empty<string>()
            : body.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (TryWorkspaceFileSuggestions(tokens, endsWithSpace, workspaceFiles, workspaceFileLimit, out var files))
            return files;

        return GetSegmentSuggestions(tokens, endsWithSpace);
    }

    static bool TryWorkspaceFileSuggestions(
        IReadOnlyList<string> tokens,
        bool endsWithSpace,
        WorkspaceFileMatchSource? workspaceFiles,
        int limit,
        out IReadOnlyList<GlassSlashSuggestion> suggestions)
    {
        suggestions = [];
        if (workspaceFiles is null || limit <= 0)
            return false;

        if (!TryPathTailCommand(tokens, endsWithSpace, out var cmdPath, out var prefix))
            return false;

        var matches = workspaceFiles(prefix, limit);
        if (matches.Count == 0)
            return false;

        suggestions = matches
            .Select(m => new GlassSlashSuggestion(
                $"{cmdPath} {m.InsertPath}",
                m.InsertPath,
                m.Help))
            .ToArray();
        return true;
    }

    /// <summary>/open · /file open · /solution load · /intercom attach file — path-tail matches.</summary>
    static bool TryPathTailCommand(
        IReadOnlyList<string> tokens,
        bool endsWithSpace,
        out string cmdPath,
        out string pathPrefix)
    {
        cmdPath = "";
        pathPrefix = "";
        if (tokens.Count == 0)
            return false;

        if (tokens[0].Equals("open", StringComparison.OrdinalIgnoreCase))
        {
            cmdPath = "/open";
            if (tokens.Count == 1)
                return endsWithSpace;
            pathPrefix = string.Join(' ', tokens.Skip(1));
            if (endsWithSpace && tokens.Count > 1)
                pathPrefix += " ";
            // Mid-typing path without trailing space still filters.
            return true;
        }

        if (tokens.Count >= 2
            && tokens[0].Equals("file", StringComparison.OrdinalIgnoreCase)
            && tokens[1].Equals("open", StringComparison.OrdinalIgnoreCase))
        {
            cmdPath = "/file open";
            if (tokens.Count == 2)
                return endsWithSpace;
            pathPrefix = string.Join(' ', tokens.Skip(2));
            if (endsWithSpace && tokens.Count > 2)
                pathPrefix += " ";
            return true;
        }

        if (tokens.Count >= 2
            && tokens[0].Equals("solution", StringComparison.OrdinalIgnoreCase)
            && tokens[1].Equals("load", StringComparison.OrdinalIgnoreCase))
        {
            cmdPath = "/solution load";
            if (tokens.Count == 2)
                return endsWithSpace;
            pathPrefix = string.Join(' ', tokens.Skip(2));
            if (endsWithSpace && tokens.Count > 2)
                pathPrefix += " ";
            return true;
        }

        if (tokens.Count >= 3
            && tokens[0].Equals("intercom", StringComparison.OrdinalIgnoreCase)
            && tokens[1].Equals("attach", StringComparison.OrdinalIgnoreCase)
            && tokens[2].Equals("file", StringComparison.OrdinalIgnoreCase))
        {
            cmdPath = "/intercom attach file";
            if (tokens.Count == 3)
                return endsWithSpace;
            pathPrefix = string.Join(' ', tokens.Skip(3));
            return true;
        }

        return false;
    }

    /// <summary>CIDE ChatSlashAutocomplete segment steps — next path token only (ADR 0119 hierarchy).</summary>
    static IReadOnlyList<GlassSlashSuggestion> GetSegmentSuggestions(
        IReadOnlyList<string> tokens,
        bool endsWithSpace)
    {
        var depth = endsWithSpace ? tokens.Count : Math.Max(0, tokens.Count - 1);
        var partial = endsWithSpace || tokens.Count == 0 ? "" : tokens[^1];
        var prefixTokens = endsWithSpace
            ? tokens
            : tokens.Take(Math.Max(0, tokens.Count - 1)).ToArray();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<GlassSlashSuggestion>();

        foreach (var cmd in Commands)
        {
            var segs = cmd.Path.TrimStart('/').Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (segs.Length <= depth)
                continue;

            if (!PrefixMatches(segs, prefixTokens))
                continue;

            var next = segs[depth];
            if (partial.Length > 0
                && !next.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!seen.Add(next))
                continue;

            var insertSegs = prefixTokens.Concat([next]).ToArray();
            var more = segs.Length > depth + 1 || cmd.ArgTail != ArgTailKind.None;
            var insert = "/" + string.Join(' ', insertSegs) + (more ? " " : "");
            var title = next;
            var help = segs.Length == depth + 1
                ? cmd.Help
                : $"{cmd.Path} — {cmd.Help}";
            list.Add(new GlassSlashSuggestion(insert, title, help));
        }

        return list;
    }

    static bool PrefixMatches(string[] segs, IReadOnlyList<string> prefixTokens)
    {
        if (prefixTokens.Count > segs.Length)
            return false;
        for (var i = 0; i < prefixTokens.Count; i++)
        {
            if (!segs[i].Equals(prefixTokens[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    public static bool TryResolve(string? raw, out Command command, out string argsTail)
    {
        command = null!;
        argsTail = "";
        if (!IsSlashLine(raw))
            return false;

        var body = raw!.TrimStart()[1..].Trim();
        if (body.Length == 0)
            return false;

        foreach (var c in Commands.OrderByDescending(x => x.Path.Length))
        {
            var pathBody = c.Path.TrimStart('/');
            if (body.StartsWith(pathBody, StringComparison.OrdinalIgnoreCase)
                && (body.Length == pathBody.Length || body[pathBody.Length] == ' '))
            {
                command = c;
                argsTail = body.Length > pathBody.Length ? body[(pathBody.Length)..].TrimStart() : "";
                return true;
            }
        }

        var parts = body.Split(' ', 2, StringSplitOptions.TrimEntries);
        foreach (var c in Commands)
        {
            if (!c.Id.Equals(parts[0], StringComparison.OrdinalIgnoreCase))
                continue;
            command = c;
            argsTail = parts.Length > 1 ? parts[1] : "";
            return true;
        }

        return false;
    }

    public static bool ShouldAutoRunOnCommit(string? raw)
    {
        if (!TryResolve(raw, out var cmd, out var argsTail))
            return false;
        return cmd.ArgTail switch
        {
            ArgTailKind.Required => !string.IsNullOrWhiteSpace(argsTail),
            _ => true
        };
    }

    public static string FormatHelp()
    {
        var lines = Commands.Select(c => $"{c.Path,-28} [{c.ArgTail,-8}] {c.Help}");
        return "Glass slash (ADR 0150 ArgTail · ADR 0125 path steps):\n" + string.Join('\n', lines)
               + "\n\n(segment popup = next token · /open␠ = workspace files · /select short → /intercom message select)";
    }
}

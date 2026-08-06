#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>Glass-local slash catalog (Avalonia-free). Full CIDE ChatSlash stays in host.</summary>
public sealed record GlassSlashSuggestion(string InsertText, string Title, string Help);

public static class GlassSlashCatalog
{
    public sealed record Command(string Id, string Path, string Help, bool RequiresArgs = false);

    static readonly Command[] Commands =
    [
        new("help", "/help", "List Glass slash commands"),
        new("status", "/status", "Glass session / latch status line"),
        new("topics", "/topics", "List topic cards (30m gap) · /topics N to select"),
        new("fds", "/fds", "Open Flight Data Storage MFD (plans/reports/notes)"),
        new("open", "/open", "Open path[:line] in AvalonEdit (thin attach↔code)", RequiresArgs: true),
        // attach: bare OK when AvalonEdit has a selection — Autocomplete may auto-run; fail → park composer (no usage bubble).
        new("attach", "/attach", "Insert [path:line] chip from editor selection (ADR 0128 thin)"),
        new("citizen", "/citizen", "Talk to habitat citizen (dialog peer · GigaChat) — not guest @PF", RequiresArgs: true),
        new("letter", "/letter", "Where the Agent Who letter lives (CDP canon)"),
        // select: Autocomplete waits for N; bare TryRun → last message (not usage).
        new("select", "/intercom message select", "Select #N · N:M · [3;5] [8;15] · clear · bare=last (ADR 0136/0138)", RequiresArgs: true),
        new("message_next", "/intercom message next", "Select next feed message (ordinal)"),
        new("message_prev", "/intercom message prev", "Select previous feed message (ordinal)"),
    ];

    public static bool IsSlashLine(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        return raw.TrimStart()[0] == '/';
    }

    public static IReadOnlyList<GlassSlashSuggestion> Suggest(string? raw)
    {
        if (!IsSlashLine(raw))
            return [];

        var body = raw!.TrimStart()[1..].TrimStart();
        var filter = body.Split(' ', 2, StringSplitOptions.TrimEntries)[0];

        return Commands
            .Where(c => filter.Length == 0
                        || c.Id.StartsWith(filter, StringComparison.OrdinalIgnoreCase)
                        || c.Path.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .Select(c =>
            {
                // Short insert for select — Enter-commit must leave room for N (not run bare path).
                var insert = c.Id == "select" ? "/select " : c.Path + " ";
                return new GlassSlashSuggestion(insert, c.Path, c.Help);
            })
            .ToArray();
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

        // Longest path first (CIDE /intercom message select), then short id (/select, /topics…).
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

    /// <summary>CIDE parity: Autocomplete Enter auto-runs only when args are present or command allows bare.</summary>
    public static bool ShouldAutoRunOnCommit(string? raw)
    {
        if (!TryResolve(raw, out var cmd, out var argsTail))
            return false;
        if (cmd.RequiresArgs && string.IsNullOrWhiteSpace(argsTail))
            return false;
        return true;
    }

    public static string ComposerInsertForArgs(Command cmd) =>
        cmd.Id == "select" ? "/select " : cmd.Path.TrimEnd() + " ";

    public static string FormatHelp()
    {
        var lines = Commands.Select(c => $"{c.Path,-10} {c.Help}");
        return "Glass slash:\n" + string.Join('\n', lines)
               + "\n\n(/select short → /intercom message select · residual denser: find/relate/anchors/attach*/topic*/spine*)";
    }
}

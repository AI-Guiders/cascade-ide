#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>Glass-local slash catalog (Avalonia-free). ArgTailKind = ADR 0150 (CIDE intent-catalog parity).</summary>
public sealed record GlassSlashSuggestion(string InsertText, string Title, string Help);

public static class GlassSlashCatalog
{
    /// <summary>ADR 0150: none | optional | required — autocomplete Enter vs execute.</summary>
    public enum ArgTailKind
    {
        None,
        Optional,
        Required
    }

    public sealed record Command(string Id, string Path, string Help, ArgTailKind ArgTail = ArgTailKind.None);

    static readonly Command[] Commands =
    [
        new("help", "/help", "List Glass slash commands"),
        new("status", "/status", "Glass session / latch status line"),
        new("topics", "/topics", "List topic cards (30m gap) · /topics N to select", ArgTailKind.Optional),
        new("fds", "/fds", "Open Flight Data Storage MFD (plans/reports/notes)"),
        // Glass /open = path in AvalonEdit (not CIDE chat_open_selected_thread).
        new("open", "/open", "Open path[:line] in AvalonEdit (thin attach↔code)", ArgTailKind.Required),
        // optional: bare may use AvalonEdit selection; empty + no selection → honest usage.
        new("attach", "/attach", "Insert [path:line] chip from editor selection (ADR 0128 thin)", ArgTailKind.Optional),
        new("citizen", "/citizen", "Talk to habitat citizen (dialog peer) — not guest @PF", ArgTailKind.Required),
        new("letter", "/letter", "Where the Agent Who letter lives (CDP canon)"),
        // CIDE intent-catalog: /intercom message select · arg_tail = required (no bare=last invent).
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
            .Select(c => new GlassSlashSuggestion(ComposerInsert(c), c.Path, c.Help))
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

    /// <summary>ADR 0150 + CIDE <c>ShouldAutoExecuteAfterAutocompleteCommit</c>: required needs non-empty ArgTail.</summary>
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
        return "Glass slash (ADR 0150 ArgTail):\n" + string.Join('\n', lines)
               + "\n\n(/select short → /intercom message select · required → type N then Enter; residual: find/relate/anchors…)";
    }
}

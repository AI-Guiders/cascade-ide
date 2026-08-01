#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// Shared SoftOrgan chrome density — Avalonia WorkspaceChromeBand + Glass WPF band.
/// Latches stay latch-first; UI shows top-N by priority + overflow (expand optional).
/// </summary>
public static class SoftOrganChromeDensityPolicy
{
    public const int DefaultMaxVisible = 3;
    public const string CollapseLabel = "− collapse · SoftOrgan";

    public readonly record struct Hint(string Id, string Text, int Priority);

    public readonly record struct Result(
        IReadOnlyList<string> VisibleLines,
        int HiddenCount,
        string? OverflowLine,
        bool IsExpanded = false)
    {
        public bool HasOverflow => !string.IsNullOrWhiteSpace(OverflowLine);
    }

    /// <summary>
    /// Priority: continuity/focus first, SoftOrgan attention next, cold maps last.
    /// Lower number = higher priority. Ids pass through <see cref="SoftOrganLatchCatalog.Canonicalize"/>.
    /// </summary>
    public static int PriorityFor(string id)
    {
        id = SoftOrganLatchCatalog.Canonicalize(id);
        return id switch
        {
            "pressure" => 0,
            "ignite" => 1,
            "plan" => 2,
            "cabin" => 3,
            "scope" => 4,
            "alert" or "eicas" or "sa" => 5,
            "qrh" => 6,
            "ecl" or "chk" => 7,
            "review" => 8,
            "refactor" => 9,
            "plugins" => 10,
            "toolchain" => 11,
            "test_desk" => 12,
            "debug_desk" => 13,
            "files_desk" => 14,
            "find_desk" => 15,
            "crm" => 16,
            "report" => 17,
            "webcam" => 18,
            "sys" => 19,
            "onboard" => 20,
            "arch" => 21,
            "mcp" => 22,
            "learn" => 23,
            "domain" => 24,
            SoftOrganLatchCatalog.SaDesk => 25,
            _ => 50
        };
    }

    public static Result Collapse(
        IEnumerable<Hint> hints,
        int maxVisible = DefaultMaxVisible,
        bool expanded = false)
    {
        if (maxVisible < 1)
            maxVisible = DefaultMaxVisible;

        var ordered = hints
            .Where(h => !string.IsNullOrWhiteSpace(h.Text))
            .OrderBy(h => h.Priority)
            .ThenBy(h => h.Id, StringComparer.Ordinal)
            .Select(h => h.Text.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (ordered.Count == 0)
            return new Result(Array.Empty<string>(), 0, null, false);

        if (ordered.Count <= maxVisible)
            return new Result(ordered, 0, null, false);

        var hidden = ordered.Count - maxVisible;
        if (expanded)
            return new Result(ordered, 0, CollapseLabel, true);

        var visible = ordered.Take(maxVisible).ToArray();
        return new Result(visible, hidden, $"+{hidden} more · SoftOrgan latches", false);
    }

    /// <summary>Flip expand when overflow is actionable; returns new expanded state.</summary>
    public static bool ToggleExpanded(bool currentlyExpanded, int totalHintCount, int maxVisible = DefaultMaxVisible)
    {
        if (maxVisible < 1)
            maxVisible = DefaultMaxVisible;
        if (totalHintCount <= maxVisible)
            return false;
        return !currentlyExpanded;
    }

    public static Hint? From(string id, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        id = SoftOrganLatchCatalog.Canonicalize(id);
        if (id.Length == 0)
            return null;
        return new Hint(id, text.Trim(), PriorityFor(id));
    }

    /// <summary>Instrument mnemonic for human glass chips (not prose).</summary>
    public static string ShortLabel(string id)
    {
        id = SoftOrganLatchCatalog.Canonicalize(id);
        return id switch
        {
            "pressure" => "PRS",
            "ignite" => "IGN",
            "plan" => "PLAN",
            "cabin" => "CAB",
            "scope" => "SCP",
            "alert" => "ALRT",
            "eicas" => "EICAS",
            "sa" => "SA",
            SoftOrganLatchCatalog.SaDesk => "SA",
            "qrh" => "QRH",
            "ecl" => "ECL",
            "chk" => "CHK",
            "review" => "REV",
            "refactor" => "REF",
            "toolchain" => "TLC",
            "test_desk" => "TEST",
            "debug_desk" => "DBG",
            "files_desk" => "FILES",
            "find_desk" => "FIND",
            "mcp" => "MCP",
            "sys" => "SYS",
            "domain" => "DOM",
            "arch" => "ARCH",
            "learn" => "LRN",
            "onboard" => "ONB",
            "webcam" => "CAM",
            "crm" => "CRM",
            "report" => "RPT",
            "plugins" => "PLG",
            _ => id.Length <= 5 ? id.ToUpperInvariant() : id[..4].ToUpperInvariant()
        };
    }

    public static bool LooksHot(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        return text.Contains("WARN", StringComparison.OrdinalIgnoreCase)
            || text.Contains("FAIL", StringComparison.OrdinalIgnoreCase)
            || text.Contains("ERROR", StringComparison.OrdinalIgnoreCase)
            || text.Contains("ECL", StringComparison.OrdinalIgnoreCase)
            || text.Contains("blocked", StringComparison.OrdinalIgnoreCase);
    }
}

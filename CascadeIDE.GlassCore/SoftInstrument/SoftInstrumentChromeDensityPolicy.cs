#nullable enable

namespace CascadeIDE.SoftInstrument;

/// <summary>
/// Shared SoftInstrument chrome density — Avalonia WorkspaceChromeBand + Glass WPF band.
/// Latches stay latch-first; UI shows top-N by priority + overflow (expand optional).
/// </summary>
public static class SoftInstrumentChromeDensityPolicy
{
    public const int DefaultMaxVisible = 3;
    public const string CollapseLabel = "− collapse · SoftInstrument";

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
    /// Priority: continuity/focus first, SoftInstrument attention next, cold maps last.
    /// Lower number = higher priority. Ids pass through <see cref="SoftInstrumentLatchCatalog.Canonicalize"/>.
    /// </summary>
    public static int PriorityFor(string id)
    {
        id = SoftInstrumentLatchCatalog.Canonicalize(id);
        return id switch
        {
            "pressure" => 0,
            "ignite" => 1,
            "plan" => 2,
            SoftInstrumentLatchCatalog.Hands => 3,
            "cabin" => 4,
            "scope" => 5,
            "alert" or "eicas" or "sa" => 6,
            "qrh" => 7,
            "ecl" or "chk" => 8,
            "review" => 9,
            "refactor" => 10,
            "plugins" => 11,
            "toolchain" => 12,
            "test_desk" => 13,
            "debug_desk" => 14,
            "build_desk" => 15,
            "files_desk" => 16,
            "find_desk" => 17,
            "crm" => 18,
            "report" => 19,
            "webcam" => 20,
            "sys" => 21,
            "onboard" => 22,
            "arch" => 23,
            "mcp" => 24,
            "learn" => 25,
            "domain" => 26,
            SoftInstrumentLatchCatalog.SaDesk => 27,
            "md_author" => 28,
            "rules" => 29,
            "calendar" => 30,
            "fdr" => 31,
            "teeth" => 32,
            "postmortem" => 33,
            "glass" => 34,
            "problems" => 35,
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
        return new Result(visible, hidden, $"+{hidden} more · SoftInstrument latches", false);
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
        id = SoftInstrumentLatchCatalog.Canonicalize(id);
        if (id.Length == 0)
            return null;
        return new Hint(id, text.Trim(), PriorityFor(id));
    }

    /// <summary>Instrument mnemonic for human glass chips (not prose).</summary>
    public static string ShortLabel(string id)
    {
        id = SoftInstrumentLatchCatalog.Canonicalize(id);
        return id switch
        {
            "pressure" => "PRS",
            "ignite" => "IGN",
            "plan" => "PLAN",
            SoftInstrumentLatchCatalog.Hands => "HND",
            "cabin" => "CAB",
            "scope" => "SCP",
            "alert" => "ALRT",
            "eicas" => "EICAS",
            "sa" => "SA",
            SoftInstrumentLatchCatalog.SaDesk => "SA",
            "qrh" => "QRH",
            "ecl" => "ECL",
            "chk" => "CHK",
            "review" => "REV",
            "refactor" => "REF",
            "toolchain" => "TLC",
            "test_desk" => "TEST",
            "debug_desk" => "DBG",
            "build_desk" => "BLD",
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
            "md_author" => "MD",
            "rules" => "RULE",
            "calendar" => "CAL",
            "fdr" => "FDR",
            "teeth" => "TTH",
            "postmortem" => "PM",
            "glass" => "GLS",
            "problems" => "PRB",
            _ => id.Length <= 5 ? id.ToUpperInvariant() : id[..4].ToUpperInvariant()
        };
    }

    public static bool LooksHot(string text) =>
        ChipLevelFromHint(text) != GlassChipLevel.Quiet;

    /// <summary>Map chrome_hint prose → indication level (Fail &gt; Warn &gt; Caution).</summary>
    public static GlassChipLevel ChipLevelFromHint(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return GlassChipLevel.Quiet;

        if (text.Contains("FAIL", StringComparison.OrdinalIgnoreCase)
            || text.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
            return GlassChipLevel.Fail;

        if (text.Contains("WARN", StringComparison.OrdinalIgnoreCase)
            || text.Contains("ECL", StringComparison.OrdinalIgnoreCase)
            || text.Contains("blocked", StringComparison.OrdinalIgnoreCase))
            return GlassChipLevel.Warn;

        if (text.Contains("CAUTION", StringComparison.OrdinalIgnoreCase)
            || text.Contains("ON GND", StringComparison.OrdinalIgnoreCase)
            || text.Contains("RUNNING", StringComparison.OrdinalIgnoreCase))
            return GlassChipLevel.Caution;

        return GlassChipLevel.Quiet;
    }
}

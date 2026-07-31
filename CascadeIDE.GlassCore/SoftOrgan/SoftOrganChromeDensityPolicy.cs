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
            "crm" => 12,
            "report" => 13,
            "webcam" => 14,
            "sys" => 15,
            "onboard" => 16,
            "arch" => 17,
            "mcp" => 18,
            "learn" => 19,
            "domain" => 20,
            SoftOrganLatchCatalog.SaDesk => 21,
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
}

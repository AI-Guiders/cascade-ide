#nullable enable

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Collapse stacked SoftOrgan chrome hints for WorkspaceChromeBand.
/// Latches stay latch-first for agents; UI shows top-N by priority + overflow.
/// Overflow chip toggles expand/collapse (Glass SoftOrgan band parity).
/// </summary>
public static class AgentChromeHintDensityPolicy
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
    /// Lower number = higher priority.
    /// </summary>
    public static int PriorityFor(string id) => id switch
    {
        "pressure" => 0,
        "ignite" => 1,
        "plan" => 2,
        "cabin" => 3,
        "scope" => 4,
        "review" => 5,
        "refactor" => 6,
        "plugins" => 7,
        "toolchain" => 8,
        "crm" => 9,
        "report" => 10,
        "webcam" => 11,
        "sys" => 12,
        "onboard" => 13,
        "arch" => 14,
        "mcp" => 15,
        "learn" => 16,
        "domain" => 17,
        "sa_desk" or "sa-desk" => 18,
        _ => 50
    };

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
        return new Hint(id, text.Trim(), PriorityFor(id));
    }
}

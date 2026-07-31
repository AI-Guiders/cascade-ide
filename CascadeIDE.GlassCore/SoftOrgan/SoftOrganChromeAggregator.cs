#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// Collapse SoftOrgan chrome_hint latches for Glass top band.
/// Latches stay latch-first; UI shows top-N by priority + overflow
/// (Avalonia WorkspaceChromeBand parity — VisibleLines + OverflowLine).
/// Overflow chip toggles expand/collapse for operator density peek.
/// </summary>
public sealed class SoftOrganChromeAggregator
{
    public const int MaxVisible = 3;
    public const string CollapseLabel = "− collapse · SoftOrgan";

    public readonly record struct Band(
        IReadOnlyList<string> VisibleLines,
        int HiddenCount,
        string? OverflowLine,
        bool IsExpanded)
    {
        public bool HasContent => VisibleLines.Count > 0;
        public bool HasOverflow => !string.IsNullOrWhiteSpace(OverflowLine);
    }

    readonly Dictionary<string, string> _hints = new(StringComparer.OrdinalIgnoreCase);
    readonly object _gate = new();
    bool _expanded;

    public bool IsExpanded
    {
        get { lock (_gate) return _expanded; }
    }

    /// <summary>Flip expand when overflow is actionable; returns new expanded state.</summary>
    public bool ToggleExpanded()
    {
        lock (_gate)
        {
            if (OrderedHints().Count <= MaxVisible)
            {
                _expanded = false;
                return false;
            }

            _expanded = !_expanded;
            return _expanded;
        }
    }

    public void Collapse()
    {
        lock (_gate)
            _expanded = false;
    }

    public Band Snapshot()
    {
        lock (_gate)
        {
            var ordered = OrderedHints();

            if (ordered.Count == 0)
            {
                _expanded = false;
                return new Band(Array.Empty<string>(), 0, null, false);
            }

            if (ordered.Count <= MaxVisible)
            {
                _expanded = false;
                return new Band(ordered, 0, null, false);
            }

            var hidden = ordered.Count - MaxVisible;
            if (_expanded)
                return new Band(ordered, 0, CollapseLabel, true);

            var visible = ordered.Take(MaxVisible).ToArray();
            return new Band(visible, hidden, $"+{hidden} more · SoftOrgan latches", false);
        }
    }

    public void Apply(string organId, string? chromeHint)
    {
        if (string.IsNullOrWhiteSpace(organId))
            return;

        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(chromeHint))
                _hints.Remove(organId);
            else
                _hints[organId] = chromeHint.Trim();
        }
    }

    List<string> OrderedHints()
    {
        return _hints
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .OrderBy(kv => PriorityFor(kv.Key))
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => kv.Value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Lower number = higher priority (Glass SoftOrgan table).</summary>
    public static int PriorityFor(string id) => id switch
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
        "sa-desk" or "sa_desk" => 21,
        _ => 50
    };
}

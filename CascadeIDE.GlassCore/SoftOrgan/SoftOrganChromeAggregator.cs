#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// SoftOrgan chrome_hint latch store for Glass top band.
/// Density math lives in <see cref="SoftOrganChromeDensityPolicy"/> (shared with Avalonia).
/// </summary>
public sealed class SoftOrganChromeAggregator
{
    public const int MaxVisible = SoftOrganChromeDensityPolicy.DefaultMaxVisible;
    public const string CollapseLabel = SoftOrganChromeDensityPolicy.CollapseLabel;

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
            _expanded = SoftOrganChromeDensityPolicy.ToggleExpanded(_expanded, HintCountUnlocked());
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
            var r = SoftOrganChromeDensityPolicy.Collapse(HintCandidatesUnlocked(), expanded: _expanded);
            if (!r.IsExpanded && _expanded)
                _expanded = false;
            return new Band(r.VisibleLines, r.HiddenCount, r.OverflowLine, r.IsExpanded);
        }
    }

    /// <summary>Store or clear chrome_hint for a catalog SoftOrgan id; unknown ids are ignored.</summary>
    public void Apply(string organId, string? chromeHint)
    {
        var id = SoftOrganLatchCatalog.Canonicalize(organId);
        if (!SoftOrganLatchCatalog.Contains(id))
            return;

        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(chromeHint))
                _hints.Remove(id);
            else
                _hints[id] = chromeHint.Trim();
        }
    }

    int HintCountUnlocked() =>
        HintCandidatesUnlocked().Count();

    IEnumerable<SoftOrganChromeDensityPolicy.Hint> HintCandidatesUnlocked()
    {
        foreach (var kv in _hints)
        {
            if (SoftOrganChromeDensityPolicy.From(kv.Key, kv.Value) is { } h)
                yield return h;
        }
    }

    /// <summary>Lower number = higher priority (shared SoftOrgan table).</summary>
    public static int PriorityFor(string id) => SoftOrganChromeDensityPolicy.PriorityFor(id);
}

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

    /// <summary>Human glass: compact organ chips (label + tooltip), not multiline chrome_hint prose.</summary>
    public readonly record struct Chip(string Id, string Label, string ToolTip, bool Hot);

    public readonly record struct ChipBand(IReadOnlyList<Chip> Visible, int HiddenCount, bool IsExpanded)
    {
        public bool HasContent => Visible.Count > 0;
        public bool HasOverflow => HiddenCount > 0 || IsExpanded;
    }

    public ChipBand SnapshotChips(int maxVisible = 6)
    {
        lock (_gate)
        {
            var ordered = HintCandidatesUnlocked()
                .OrderBy(h => h.Priority)
                .ThenBy(h => h.Id, StringComparer.Ordinal)
                .ToList();

            if (ordered.Count == 0)
            {
                _expanded = false;
                return new ChipBand(Array.Empty<Chip>(), 0, false);
            }

            if (maxVisible < 1)
                maxVisible = 6;

            static Chip[] Map(IEnumerable<SoftOrganChromeDensityPolicy.Hint> hints) =>
                hints.Select(h => new Chip(
                    h.Id,
                    SoftOrganChromeDensityPolicy.ShortLabel(h.Id),
                    h.Text,
                    SoftOrganChromeDensityPolicy.LooksHot(h.Text))).ToArray();

            if (ordered.Count <= maxVisible)
            {
                _expanded = false;
                return new ChipBand(Map(ordered), 0, false);
            }

            if (_expanded)
                return new ChipBand(Map(ordered), 0, true);

            return new ChipBand(Map(ordered.Take(maxVisible)), ordered.Count - maxVisible, false);
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

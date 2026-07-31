#nullable enable

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Collapse SoftOrgan chrome_hint latches for WPF top band.
/// Priority table mirrors Avalonia AgentChromeHintDensityPolicy
/// (WPF host does not ProjectReference Avalonia Features).
/// </summary>
internal sealed class SoftOrganChromeAggregator
{
    public const int MaxVisible = 3;

    readonly Dictionary<string, string> _hints = new(StringComparer.OrdinalIgnoreCase);
    readonly object _gate = new();

    public string? BandLine
    {
        get
        {
            lock (_gate)
            {
                var ordered = _hints
                    .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                    .OrderBy(kv => PriorityFor(kv.Key))
                    .ThenBy(kv => kv.Key, StringComparer.Ordinal)
                    .Select(kv => kv.Value.Trim())
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                if (ordered.Count == 0)
                    return null;
                if (ordered.Count <= MaxVisible)
                    return string.Join("  ·  ", ordered);

                var visible = ordered.Take(MaxVisible);
                var hidden = ordered.Count - MaxVisible;
                return string.Join("  ·  ", visible) + $"  +{hidden} more · SoftOrgan latches";
            }
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

    /// <summary>Lower number = higher priority (parity Avalonia).</summary>
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

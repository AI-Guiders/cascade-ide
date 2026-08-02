#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>
/// Merge alert + qrh + ecl status lines into an assembled EICAS band.
/// Toolkit-agnostic; Glass WPF paints the stack on MfdHealth.
/// </summary>
public sealed class EicasBandAggregator
{
    static readonly string[] Order = ["alert", "qrh", "ecl"];

    readonly Dictionary<string, string> _lines = new(StringComparer.OrdinalIgnoreCase);
    readonly object _gate = new();

    /// <summary>Highest-priority single line (compat).</summary>
    public string? BandLine
    {
        get
        {
            var stack = BandStack;
            return stack.Count == 0 ? null : stack[0];
        }
    }

    /// <summary>Assembled multi-line band for clear Glass EICAS.</summary>
    public string? BandText
    {
        get
        {
            var stack = BandStack;
            return stack.Count == 0 ? null : string.Join('\n', stack);
        }
    }

    public IReadOnlyList<string> BandStack
    {
        get
        {
            lock (_gate)
            {
                var list = new List<string>(3);
                foreach (var key in Order)
                {
                    if (_lines.TryGetValue(key, out var line) && !string.IsNullOrWhiteSpace(line))
                        list.Add(line.Trim());
                }

                return list;
            }
        }
    }

    /// <summary>WARN &gt; CAUT &gt; ADV/ECL for band chrome color.</summary>
    public string Severity
    {
        get
        {
            lock (_gate)
            {
                if (_lines.TryGetValue("alert", out var alert) && !string.IsNullOrWhiteSpace(alert))
                {
                    if (alert.Contains("· WARN ·", StringComparison.Ordinal))
                        return "warn";
                    if (alert.Contains("· CAUT ·", StringComparison.Ordinal))
                        return "caut";
                    return "adv";
                }

                if (_lines.ContainsKey("qrh") || _lines.ContainsKey("ecl"))
                    return "adv";
                return "idle";
            }
        }
    }

    public void Apply(string source, string? statusLine)
    {
        if (string.IsNullOrWhiteSpace(source))
            return;

        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(statusLine))
                _lines.Remove(source);
            else
                _lines[source] = statusLine.Trim();
        }
    }
}

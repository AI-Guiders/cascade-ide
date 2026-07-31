#nullable enable

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Merge alert-LATEST + qrh-LATEST into MFD EICAS health line (WPF peel).
/// Alert outranks qrh; clear sources drop out of the band.
/// </summary>
internal sealed class EicasBandAggregator
{
    readonly Dictionary<string, string> _lines = new(StringComparer.OrdinalIgnoreCase);
    readonly object _gate = new();

    public string? BandLine
    {
        get
        {
            lock (_gate)
            {
                if (_lines.TryGetValue("alert", out var alert) && !string.IsNullOrWhiteSpace(alert))
                    return alert.Trim();
                if (_lines.TryGetValue("qrh", out var qrh) && !string.IsNullOrWhiteSpace(qrh))
                    return qrh.Trim();
                return null;
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

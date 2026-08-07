#nullable enable
using System.Text.Json;
using CascadeIDE.Features.Cdp;

namespace CDP.GlassCockpit.Windows;

/// <summary>SoftOrgan latch chrome_hint / pulse read for Glass band.</summary>
internal static partial class LatchPaint
{
    /// <summary>Read SoftOrgan latch chrome_hint (null if idle / missing / parse fail).</summary>
    public static string? TryReadChromeHint(string path)
    {
        try
        {
            var raw = CdpLatchIo.TryReadAllTextIfExists(path);
            if (raw is null)
                return null;
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.TryGetProperty("chrome_hint", out var hintEl)
                && hintEl.ValueKind == JsonValueKind.String)
            {
                var hint = hintEl.GetString();
                return HumanizeChromeHint(hint);
            }

            // Dark Cockpit: active=false or missing hint → silent
            if (root.TryGetProperty("active", out var activeEl)
                && activeEl.ValueKind is JsonValueKind.False)
                return null;

            if (root.TryGetProperty("pulse", out var pulseEl)
                && pulseEl.ValueKind == JsonValueKind.String)
            {
                var pulse = pulseEl.GetString();
                return HumanizeChromeHint(pulse);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}

namespace CascadeIDE.Models;

/// <summary>Skia overlay (W2) и mount (W3). TOML: <c>[display.skia]</c>.</summary>
public sealed class DisplaySkiaSettings
{
    public bool ZoneGeometryOverlay { get; set; }

    public bool InstrumentMount { get; set; }
}

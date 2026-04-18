namespace CascadeIDE.Models;

/// <summary>
/// Хосты окон, Skia, mount, слоты инструментов (ADR 0017, 0050, Wave 2–3).
/// TOML: <c>[display]</c> и вложенные таблицы.
/// </summary>
public sealed class DisplaySettings
{
    public bool MaximizeHostsOnDedicatedScreens { get; set; } = true;

    public bool PreferRepoInstruments { get; set; }

    /// <summary>Слоты PFD/MFD: ключи <see cref="InstrumentRoutingSlotKeys"/>.</summary>
    public Dictionary<string, string>? Instruments { get; set; }

    public DisplayPfdHostSettings Pfd { get; set; } = new();

    public DisplayMfdHostSettings Mfd { get; set; } = new();

    public DisplaySkiaSettings Skia { get; set; } = new();

    public DisplayMountSettings Mount { get; set; } = new();
}

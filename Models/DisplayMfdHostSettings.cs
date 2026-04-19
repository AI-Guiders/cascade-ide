namespace CascadeIDE.Models;

/// <summary>Окно-хост зоны Mfd (ADR 0017). TOML: <c>[display.mfd]</c>.</summary>
public sealed class DisplayMfdHostSettings
{
    public bool OpenOnStartup { get; set; } = true;

    public int? PixelX { get; set; }

    public int? PixelY { get; set; }

    public double? Width { get; set; }

    public double? Height { get; set; }
}

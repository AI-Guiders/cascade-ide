namespace CascadeIDE.Models;

/// <summary>Окно сплита P+M для пресета <c>(xP+yM)(F)</c> / <c>(F)(xP+yM)</c> (ADR 0017). TOML: <c>[display.pm]</c>.</summary>
public sealed class DisplayPmHostSettings
{
    public bool OpenOnStartup { get; set; } = true;

    public int? PixelX { get; set; }

    public int? PixelY { get; set; }

    public double? Width { get; set; }

    public double? Height { get; set; }
}

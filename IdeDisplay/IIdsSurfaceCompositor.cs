namespace CascadeIDE.IdeDisplay;

/// <summary>
/// IDS (Ide Display System): композитор поверхности оверлея IDE.
/// Аналог слоя «композитор» из ADR 0036, но для палитр/тостов/прочих IDE-surfaces, не для кабины (CDS).
/// </summary>
public interface IIdsSurfaceCompositor<in TIntent, out TSnapshot>
{
    TSnapshot Compose(TIntent intent);
}

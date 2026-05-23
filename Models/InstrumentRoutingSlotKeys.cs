namespace CascadeIDE.Models;

/// <summary>
/// Ключи словаря <c>[instrument_routing]</c> в workspace / <c>[display.instrument_routing]</c> в settings (ADR 0050).
/// </summary>
public static class InstrumentRoutingSlotKeys
{
    public const string PfdPrimary = "pfd_primary";
    public const string MfdPrimary = "mfd_primary";

    /// <summary>Оверлей-полоса warm-up/HCI над колонкой PFD (не заменяет <see cref="PfdPrimary"/>).</summary>
    public const string PfdStatusStrip = "pfd_status_strip";

    /// <summary>Оверлей-полоса warm-up/HCI над зоной Forward (редактор / Intercom).</summary>
    public const string ForwardStatusStrip = "forward_status_strip";

    public static bool IsStatusStripKey(string key) =>
        key.Equals(PfdStatusStrip, StringComparison.OrdinalIgnoreCase)
        || key.Equals(ForwardStatusStrip, StringComparison.OrdinalIgnoreCase);
}

#nullable enable

using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>Размещение полосы <see cref="CockpitStandardInstrumentIds.WorkspaceBackgroundStatusV1"/> по зонам (ADR 0050 extension).</summary>
public static class InstrumentStatusStripPlacement
{
    /// <summary>Ключ отсутствует в <c>[display.instruments]</c> — полоса включена (совместимость с <c>show_background_status_on_pfd</c>).</summary>
    public static bool IsZoneEnabled(DisplaySettings? display, string stripSlotKey)
    {
        var routing = display?.Instruments;
        if (routing is null || routing.Count == 0)
            return true;

        if (!TryGetRoutingValue(routing, stripSlotKey, out var raw))
            return true;

        if (!InstrumentStatusStripRouting.TryParse(raw, out var show, out _))
            return false;

        return show;
    }

    public static bool IsVisibleOnPfd(DisplaySettings? display, bool masterEnabled) =>
        masterEnabled && IsZoneEnabled(display, InstrumentRoutingSlotKeys.PfdStatusStrip);

    public static bool IsVisibleOnForward(DisplaySettings? display, bool masterEnabled) =>
        masterEnabled && IsZoneEnabled(display, InstrumentRoutingSlotKeys.ForwardStatusStrip);

    private static bool TryGetRoutingValue(
        IReadOnlyDictionary<string, string> routing,
        string stripSlotKey,
        out string raw)
    {
        foreach (var kv in routing)
        {
            if (!kv.Key.Equals(stripSlotKey, StringComparison.OrdinalIgnoreCase))
                continue;

            raw = kv.Value?.Trim() ?? "";
            return true;
        }

        raw = "";
        return false;
    }
}

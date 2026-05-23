#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>Значения <c>pfd_status_strip</c> / <c>forward_status_strip</c> в <c>[display.instruments]</c>.</summary>
public static class InstrumentStatusStripRouting
{
    public const string None = "none";

    public static bool TryParse(string? token, out bool showStrip, [NotNullWhen(true)] out string? instrumentId)
    {
        showStrip = false;
        instrumentId = null;
        var t = token?.Trim() ?? "";
        if (t.Length == 0)
            return false;

        if (t.Equals(None, StringComparison.OrdinalIgnoreCase))
        {
            instrumentId = CockpitStandardInstrumentIds.WorkspaceBackgroundStatusV1;
            return true;
        }

        if (t.Equals("background_status", StringComparison.OrdinalIgnoreCase)
            || t.Equals(CockpitStandardInstrumentIds.WorkspaceBackgroundStatusV1, StringComparison.OrdinalIgnoreCase))
        {
            showStrip = true;
            instrumentId = CockpitStandardInstrumentIds.WorkspaceBackgroundStatusV1;
            return true;
        }

        return false;
    }
}

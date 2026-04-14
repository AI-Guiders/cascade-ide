using CascadeIDE.Cockpit.Composition.HostSurface;

namespace CascadeIDE.Cockpit.Surface;

/// <summary>
/// Реестр монтирования инструментов главного окна: связывает стабильный <c>instrument_id</c>
/// с конкретным видом рендера на стороне хоста (Avalonia/Skia), не затрагивая композитор.
/// </summary>
public static class MainWindowInstrumentMountRegistry
{
    private static readonly IReadOnlyDictionary<string, CockpitInstrumentMount> Items =
        new Dictionary<string, CockpitInstrumentMount>(StringComparer.Ordinal)
        {
            [CockpitStandardInstrumentIds.SolutionExplorerTree] = new(
                CockpitStandardInstrumentIds.SolutionExplorerTree,
                CockpitInstrumentMountKind.AvaloniaView,
                "solution_explorer"),
        };

    public static bool TryResolve(string instrumentId, out CockpitInstrumentMount mount) =>
        Items.TryGetValue(instrumentId, out mount);
}

public enum CockpitInstrumentMountKind
{
    AvaloniaView,
    SkiaScene,
}

public readonly record struct CockpitInstrumentMount(
    string InstrumentId,
    CockpitInstrumentMountKind MountKind,
    string MountKey);

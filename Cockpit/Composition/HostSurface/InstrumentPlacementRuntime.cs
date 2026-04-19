using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Runtime-карта размещения инструментов (bundle/repo + user) для пары <c>surface_id + slot_id</c>.
/// </summary>
public static class InstrumentPlacementRuntime
{
    private static readonly Lock Gate = new();
    private static Dictionary<string, string> _workspaceMap = BuildCodeDefaults();

    private static Dictionary<string, string> BuildCodeDefaults() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            [BuildKey(MainWindowHostSurfaceIds.DockedGrid, CockpitSlotIds.Pfd)] = CockpitStandardInstrumentIds.SolutionExplorerTree,
            [BuildKey(MainWindowHostSurfaceIds.PlusMfdHostTopLevel, CockpitSlotIds.Pfd)] = CockpitStandardInstrumentIds.SolutionExplorerTree,
            [BuildKey(MainWindowHostSurfaceIds.PlusPfdHostTopLevel, CockpitSlotIds.Pfd)] = CockpitStandardInstrumentIds.SolutionExplorerTree,
            [BuildKey(MainWindowHostSurfaceIds.PlusPfdMfdHostTopLevel, CockpitSlotIds.Pfd)] = CockpitStandardInstrumentIds.SolutionExplorerTree,
        };

    internal static void ResetToCodeDefaults()
    {
        lock (Gate)
            _workspaceMap = BuildCodeDefaults();
    }

    internal static void ApplyWorkspaceInstrumentRouting(IReadOnlyDictionary<string, string>? routing)
    {
        lock (Gate)
        {
            var map = BuildCodeDefaults();
            ApplyRoutingOverlay(map, routing, "workspace");
            _workspaceMap = map;
        }
    }

    /// <param name="display">Если <c>null</c>, используется только workspace-карта (как при отсутствии user-слоя).</param>
    public static bool TryResolveInstrument(
        string surfaceId,
        string slotId,
        DisplaySettings? display,
        out string instrumentId)
    {
        instrumentId = "";

        if (display is null)
            return TryResolveWorkspaceOnly(surfaceId, slotId, out instrumentId);

        var userMap = BuildUserPlacementMap(display);
        var key = BuildKey(surfaceId, slotId);
        var preferRepo = display.PreferRepoInstruments;

        lock (Gate)
        {
            if (preferRepo)
            {
                if (_workspaceMap.TryGetValue(key, out var repoValue))
                {
                    instrumentId = repoValue;
                    return true;
                }

                if (userMap.TryGetValue(key, out var userValue))
                {
                    instrumentId = userValue;
                    return true;
                }

                return false;
            }

            if (userMap.TryGetValue(key, out var userFirstValue))
            {
                instrumentId = userFirstValue;
                return true;
            }

            if (_workspaceMap.TryGetValue(key, out var repoSecondValue))
            {
                instrumentId = repoSecondValue;
                return true;
            }

            return false;
        }
    }

    private static bool TryResolveWorkspaceOnly(string surfaceId, string slotId, out string instrumentId)
    {
        var key = BuildKey(surfaceId, slotId);
        lock (Gate)
        {
            if (_workspaceMap.TryGetValue(key, out var value))
            {
                instrumentId = value;
                return true;
            }
        }

        instrumentId = "";
        return false;
    }

    private static Dictionary<string, string> BuildUserPlacementMap(DisplaySettings display)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ApplyRoutingOverlay(map, display.Instruments, "user");
        return map;
    }

    /// <summary>
    /// Расширяет <c>pfd_primary</c>/<c>mfd_primary</c> в конкретные ключи поверхностей рантайма (без <c>surface_id</c> в TOML).
    /// </summary>
    internal static void ApplyRoutingOverlay(
        Dictionary<string, string> map,
        IReadOnlyDictionary<string, string>? routing,
        string source)
    {
        if (routing is null || routing.Count == 0)
            return;

        foreach (var kv in routing)
        {
            var routeKey = kv.Key.Trim();
            var raw = kv.Value?.Trim() ?? "";
            if (routeKey.Length == 0 || raw.Length == 0)
                continue;

            if (!InstrumentRoutingAliasResolver.TryResolve(raw, out var canonical))
            {
                global::System.Diagnostics.Debug.WriteLine(
                    $"InstrumentPlacementRuntime: unknown instrument alias or id '{raw}' for '{routeKey}' ({source})");
                continue;
            }

            if (routeKey.Equals(InstrumentRoutingSlotKeys.PfdPrimary, StringComparison.OrdinalIgnoreCase))
            {
                map[BuildKey(MainWindowHostSurfaceIds.DockedGrid, CockpitSlotIds.Pfd)] = canonical;
                map[BuildKey(MainWindowHostSurfaceIds.PlusMfdHostTopLevel, CockpitSlotIds.Pfd)] = canonical;
                map[BuildKey(MainWindowHostSurfaceIds.PlusPfdHostTopLevel, CockpitSlotIds.Pfd)] = canonical;
                map[BuildKey(MainWindowHostSurfaceIds.PlusPfdMfdHostTopLevel, CockpitSlotIds.Pfd)] = canonical;
            }
            else if (routeKey.Equals(InstrumentRoutingSlotKeys.MfdPrimary, StringComparison.OrdinalIgnoreCase))
            {
                map[BuildKey(MainWindowHostSurfaceIds.DockedGrid, CockpitSlotIds.Mfd)] = canonical;
                map[BuildKey(MainWindowHostSurfaceIds.PlusMfdHostTopLevel, CockpitSlotIds.Mfd)] = canonical;
                map[BuildKey(MainWindowHostSurfaceIds.PlusPfdHostTopLevel, CockpitSlotIds.Mfd)] = canonical;
                map[BuildKey(MainWindowHostSurfaceIds.PlusPfdMfdHostTopLevel, CockpitSlotIds.Mfd)] = canonical;
            }
            else
            {
                global::System.Diagnostics.Debug.WriteLine(
                    $"InstrumentPlacementRuntime: unknown routing key '{routeKey}' ({source})");
            }
        }
    }

    private static string BuildKey(string surfaceId, string slotId) =>
        $"{surfaceId.Trim().ToLowerInvariant()}::{slotId.Trim().ToLowerInvariant()}";
}

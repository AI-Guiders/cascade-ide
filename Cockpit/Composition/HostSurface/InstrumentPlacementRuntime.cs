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
        };

    internal static void ResetToCodeDefaults()
    {
        lock (Gate)
            _workspaceMap = BuildCodeDefaults();
    }

    internal static void ApplyWorkspaceRules(IReadOnlyList<InstrumentPlacementRuleSettings> rules)
    {
        lock (Gate)
        {
            var map = BuildCodeDefaults();
            if (rules.Count > 0)
                ApplyRules(map, rules, "workspace");
            _workspaceMap = map;
        }
    }

    public static bool TryResolveInstrument(
        string surfaceId,
        string slotId,
        DisplaySettings display,
        out string instrumentId)
    {
        instrumentId = "";

        if (display is null)
            return TryResolveWorkspaceOnly(surfaceId, slotId, out instrumentId);

        var userMap = BuildUserMap(display.InstrumentPlacementRules);
        var key = BuildKey(surfaceId, slotId);
        var preferRepo = display.PreferRepoInstrumentsPlacement;

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

    private static Dictionary<string, string> BuildUserMap(List<InstrumentPlacementRuleSettings> rules)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (rules.Count > 0)
            ApplyRules(map, rules, "user");
        return map;
    }

    private static void ApplyRules(
        Dictionary<string, string> map,
        IReadOnlyList<InstrumentPlacementRuleSettings> rules,
        string source)
    {
        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (!TryNormalizeRule(rule, out var surfaceId, out var slotId, out var instrumentId))
            {
                global::System.Diagnostics.Debug.WriteLine(
                    $"InstrumentPlacementRuntime: invalid {source} rule at index {i}");
                continue;
            }

            if (!IsKnownSlot(slotId))
            {
                global::System.Diagnostics.Debug.WriteLine(
                    $"InstrumentPlacementRuntime: unknown slot_id '{slotId}' in {source} rule at index {i}");
                continue;
            }

            if (!IsKnownInstrument(instrumentId))
            {
                global::System.Diagnostics.Debug.WriteLine(
                    $"InstrumentPlacementRuntime: unknown instrument_id '{instrumentId}' in {source} rule at index {i}");
                continue;
            }

            map[BuildKey(surfaceId, slotId)] = instrumentId;
        }
    }

    private static bool TryNormalizeRule(
        InstrumentPlacementRuleSettings rule,
        out string surfaceId,
        out string slotId,
        out string instrumentId)
    {
        surfaceId = rule?.SurfaceId?.Trim() ?? "";
        slotId = rule?.SlotId?.Trim() ?? "";
        instrumentId = rule?.InstrumentId?.Trim() ?? "";
        return surfaceId.Length > 0 && slotId.Length > 0 && instrumentId.Length > 0;
    }

    private static string BuildKey(string surfaceId, string slotId) =>
        $"{surfaceId.Trim().ToLowerInvariant()}::{slotId.Trim().ToLowerInvariant()}";

    private static bool IsKnownSlot(string slotId) =>
        string.Equals(slotId, CockpitSlotIds.Pfd, StringComparison.OrdinalIgnoreCase)
        || string.Equals(slotId, CockpitSlotIds.Mfd, StringComparison.OrdinalIgnoreCase)
        || string.Equals(slotId, CockpitSlotIds.Forward, StringComparison.OrdinalIgnoreCase);

    private static bool IsKnownInstrument(string instrumentId) =>
        string.Equals(instrumentId, CockpitStandardInstrumentIds.SolutionExplorerTree, StringComparison.OrdinalIgnoreCase)
        || string.Equals(instrumentId, CockpitStandardInstrumentIds.WorkspaceNavigationMap, StringComparison.OrdinalIgnoreCase)
        || string.Equals(instrumentId, CockpitStandardInstrumentIds.WorkspaceHealthStatusV1, StringComparison.OrdinalIgnoreCase);
}

using System.Text.Json;
using System.Text.Json.Nodes;

namespace CascadeIDE.Tests;

/// <summary>
/// Узкий детерминированный срез <c>get_cockpit_surface</c> для golden-сравнения (ADR 0052):
/// без <c>ui_mode</c>, строки <c>presentation</c>, вторичного shell и полей, зависящих от топологии презентации
/// и intent (колонки main, список инструментов) — они меняются с <c>display.screens.topology</c> и настройками.
/// </summary>
public static class AgentContractCockpitContractSlice
{
    private static readonly JsonSerializerOptions Compact = new() { WriteIndented = false };

    /// <summary>
    /// Из полного JSON CDS оставляет <c>schema_version</c> и укороченный <c>topology</c> (без колонки MFD в main),
    /// без <c>instruments</c> — форма элементов проверяется в тестах сериализации CDS.
    /// </summary>
    public static string ToStableSliceJson(string cockpitSurfaceJson)
    {
        var root = JsonNode.Parse(cockpitSurfaceJson)!.AsObject();
        var topo = root["topology"]?.AsObject();
        JsonObject? topoSlice = null;
        if (topo is not null)
        {
            topoSlice = new JsonObject();
            foreach (var p in topo)
            {
                if (p.Key == "mfd_column_visible_in_main")
                    continue;
                topoSlice[p.Key] = p.Value?.DeepClone();
            }
        }

        var slice = new JsonObject
        {
            ["schema_version"] = root["schema_version"]?.DeepClone(),
            ["topology"] = topoSlice,
        };

        return slice.ToJsonString(Compact);
    }
}

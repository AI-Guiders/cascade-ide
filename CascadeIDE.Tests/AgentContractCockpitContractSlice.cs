using System.Text.Json;
using System.Text.Json.Nodes;

namespace CascadeIDE.Tests;

/// <summary>
/// Узкий детерминированный срез <c>get_cockpit_surface</c> для golden-сравнения (ADR 0052):
/// без <c>ui_mode</c>, строки <c>presentation</c> и вторичного shell — они зависят от пользовательских настроек.
/// </summary>
public static class AgentContractCockpitContractSlice
{
    private static readonly JsonSerializerOptions Compact = new() { WriteIndented = false };

    /// <summary>
    /// Из полного JSON CDS оставляет <c>schema_version</c>, <c>topology</c>, отсортированный <c>instruments</c>.</summary>
    public static string ToStableSliceJson(string cockpitSurfaceJson)
    {
        var root = JsonNode.Parse(cockpitSurfaceJson)!.AsObject();
        var instruments = root["instruments"] as JsonArray ?? [];
        var sorted = instruments
            .Where(n => n is not null)
            .OrderBy(n => (string?)n!["slot_id"], StringComparer.Ordinal)
            .ThenBy(n => (string?)n!["instrument_id"], StringComparer.Ordinal)
            .Select(n => n!.DeepClone())
            .ToList();

        var slice = new JsonObject
        {
            ["schema_version"] = root["schema_version"]?.DeepClone(),
            ["topology"] = root["topology"]?.DeepClone(),
            ["instruments"] = new JsonArray([.. sorted]),
        };

        return slice.ToJsonString(Compact);
    }
}

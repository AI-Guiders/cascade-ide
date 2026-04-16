using System.Text.Json;
using System.Text.Json.Nodes;

namespace CascadeIDE.Tests;

/// <summary>
/// Убирает машинно-зависимые и «шумные» поля из <c>get_workspace_state</c> для сравнения в тестах (ADR 0052).
/// </summary>
public static class AgentContractWorkspaceStateRedaction
{
    /// <summary>Детерминированный вид для assert: два подряд вызова headless после редукции должны совпасть.</summary>
    public static string RedactForStableCompare(string workspaceStateJson)
    {
        var node = JsonNode.Parse(workspaceStateJson) as JsonObject
            ?? throw new InvalidOperationException("Expected JSON object.");
        RedactRoot(node);
        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static void RedactRoot(JsonObject o)
    {
        o["solution_path"] = null;
        o["current_file_path"] = "";
        o["selected_solution_path"] = null;
        o["diagnostics"] = new JsonArray();

        if (o["editor"] is JsonObject ed)
        {
            ed["content_length"] = 0;
            ed["selection_start"] = 0;
            ed["selection_length"] = 0;
        }

        if (o["breakpoints"] is JsonObject bp)
        {
            if (bp["current_file"] is JsonArray cur)
                cur.Clear();
            bp["debugger_count"] = 0;
        }

        if (o["debug"] is JsonObject dbg)
        {
            dbg["position_file"] = null;
            dbg["position_line"] = 0;
            dbg["stack_count"] = 0;
            dbg["variables_count"] = 0;
        }

        if (o["build"] is JsonObject b)
        {
            b["output_preview"] = "";
            b["binlog_path"] = null;
        }

        o["agent_trace_step_count"] = 0;
        o["is_autonomous_running"] = false;
    }
}

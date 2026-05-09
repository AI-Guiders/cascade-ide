using System.Text.Json;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>Валидация и ошибки MCP <c>codebase_index_*</c> без I/O HCI.</summary>
public static class IdeMcpHybridCodebaseIndexOrchestrator
{
    public static string MissingQueryJson() => """{"error":"missing_query"}""";

    public static string InvalidHitIdJson() => """{"error":"invalid_hit_id"}""";

    public static string SerializeReindexFailed(string detail) =>
        JsonSerializer.Serialize(new { error = "reindex_failed", detail });
}

#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using CasaField.Core;
using CascadeIDE.Features.Workspace;

namespace CascadeIDE.Features.CasaField.Application;

/// <summary>Native C# hot path: decode field grid + SN/CEN (parity with sn_cen_infer --from-field).</summary>
public static class CasaFieldHotPathRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static string BuildJson(string? workspaceRoot, string? query)
    {
        var store = CasaFieldStoreResolver.ResolveStoreDirectory(workspaceRoot);
        if (store is null || !Directory.Exists(store))
            return """{"error":"store_missing"}""";

        try
        {
            var result = CasaFieldHotPath.Run(store, query);
            var payload = new
            {
                pipeline = "casa_field_hot_path_csharp_v2",
                wallMs = result.WallMs,
                fieldVersion = result.FieldVersion,
                stale = result.Stale,
                decoded = new
                {
                    conceptCount = result.Decoded.ConceptIds.Count,
                    edgeCount = result.Decoded.Edges.Count,
                },
                sn = new
                {
                    mode = result.Sn.Mode,
                    ports = result.Sn.Ports,
                    cenOpen = result.Sn.CenOpen,
                },
                cen = new
                {
                    intent = result.Cen.Intent,
                    action = result.Cen.Action,
                    concepts = result.Cen.Concepts,
                    targets = result.Cen.Targets.Select(t => new
                    {
                        kind = t.Kind,
                        conceptId = t.ConceptId,
                        docPath = t.DocPath,
                        section = t.Section,
                        file = t.File,
                        line = t.Line,
                    }),
                    taskBand = result.Cen.TaskBand,
                    queryToDmn = result.Cen.QueryToDmn,
                    confidence = result.Cen.Confidence,
                    answerBlocks = result.Cen.AnswerBlocks.Select(b => new
                    {
                        conceptId = b.ConceptId,
                        text = b.Text,
                        section = b.Section,
                        docPath = b.DocPath,
                        score = b.Score,
                        claimKind = b.ClaimKind,
                    }),
                },
            };
            return JsonSerializer.Serialize(payload, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);
        }
    }
}

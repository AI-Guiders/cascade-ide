using System.Text.Json;
using CascadeIDE.Features.Editor.Application.Monaco;

#nullable enable

namespace CascadeIDE.Features.Lsp.DataAcquisition;

/// <summary>Parses LSP semantic tokens legend and encoded <c>data</c> arrays.</summary>
public static class CSharpLspSemanticTokensParser
{
    public static CideEditorSemanticTokensLegend? TryParseLegend(JsonElement initializeResult)
    {
        if (!initializeResult.TryGetProperty("capabilities", out var caps))
            return null;
        if (!caps.TryGetProperty("semanticTokensProvider", out var provider))
            return null;
        if (!provider.TryGetProperty("legend", out var legend))
            return null;

        var types = ReadStringArray(legend, "tokenTypes");
        var mods = ReadStringArray(legend, "tokenModifiers");
        if (types.Count == 0)
            return null;

        return new CideEditorSemanticTokensLegend(types, mods);
    }

    public static CideEditorSemanticTokensData? TryParseFullResponse(JsonDocument? doc)
    {
        if (doc is null)
            return null;
        var root = doc.RootElement;
        if (root.TryGetProperty("error", out _))
            return null;
        if (!root.TryGetProperty("result", out var result) || result.ValueKind == JsonValueKind.Null)
            return null;
        if (!result.TryGetProperty("data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Array)
            return null;

        var list = new List<uint>(dataEl.GetArrayLength());
        foreach (var item in dataEl.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Number && item.TryGetUInt32(out var v))
                list.Add(v);
        }

        if (list.Count % 5 != 0)
            return null;

        string? resultId = result.TryGetProperty("resultId", out var rid) && rid.ValueKind == JsonValueKind.String
            ? rid.GetString()
            : null;

        return new CideEditorSemanticTokensData(list, resultId);
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return [];

        var list = new List<string>();
        foreach (var item in arr.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var s = item.GetString();
                if (!string.IsNullOrEmpty(s))
                    list.Add(s);
            }
        }

        return list;
    }
}

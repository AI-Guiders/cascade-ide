using System.Text.Json;
using System.Text.Json.Nodes;
using CascadeIDE.Features.Editor.Application.Monaco;
using CascadeIDE.Services.Lsp;

#nullable enable

namespace CascadeIDE.Features.Lsp.DataAcquisition;

/// <summary>LSP textDocument/* intelligence (ADR 0163 M8).</summary>
public sealed partial class CSharpLspDiagnosticsHost
{
    public async Task<IReadOnlyList<CideEditorCompletionItem>> RequestCompletionAsync(
        string filePath,
        string text,
        int line1,
        int col1,
        CancellationToken ct)
    {
        if (!IsActive || !IsCSharpPath(filePath) || _session is null || line1 < 1 || col1 < 1)
            return [];

        await SyncFullTextForRequestAsync(filePath, text, ct).ConfigureAwait(false);
        var uri = LspFileUri.PathToFileUri(CanonicalFilePath.Normalize(filePath));
        var id = _session.AllocateRequestId();
        var msg = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = "textDocument/completion",
            ["params"] = new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = uri },
                ["position"] = new JsonObject { ["line"] = line1 - 1, ["character"] = col1 - 1 },
            },
        };

        try
        {
            using var doc = await _session.SendRequestAsync(msg, id, TimeSpan.FromSeconds(12), ct).ConfigureAwait(false);
            return ParseCompletionResponse(doc);
        }
        catch
        {
            return [];
        }
    }

    public async Task<string?> RequestSignatureHelpAsync(
        string filePath,
        string text,
        int line1,
        int col1,
        CancellationToken ct)
    {
        if (!IsActive || !IsCSharpPath(filePath) || _session is null || line1 < 1 || col1 < 1)
            return null;

        await SyncFullTextForRequestAsync(filePath, text, ct).ConfigureAwait(false);
        var uri = LspFileUri.PathToFileUri(CanonicalFilePath.Normalize(filePath));
        var id = _session.AllocateRequestId();
        var msg = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = "textDocument/signatureHelp",
            ["params"] = new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = uri },
                ["position"] = new JsonObject { ["line"] = line1 - 1, ["character"] = col1 - 1 },
            },
        };

        try
        {
            using var doc = await _session.SendRequestAsync(msg, id, TimeSpan.FromSeconds(8), ct).ConfigureAwait(false);
            return ParseSignatureHelpResponse(doc);
        }
        catch
        {
            return null;
        }
    }

    public async Task<CideEditorDefinitionLocation?> RequestDefinitionAsync(
        string filePath,
        string text,
        int line1,
        int col1,
        CancellationToken ct)
    {
        if (!IsActive || !IsCSharpPath(filePath) || _session is null || line1 < 1 || col1 < 1)
            return null;

        await SyncFullTextForRequestAsync(filePath, text, ct).ConfigureAwait(false);
        var uri = LspFileUri.PathToFileUri(CanonicalFilePath.Normalize(filePath));
        var id = _session.AllocateRequestId();
        var msg = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = "textDocument/definition",
            ["params"] = new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = uri },
                ["position"] = new JsonObject { ["line"] = line1 - 1, ["character"] = col1 - 1 },
            },
        };

        try
        {
            using var doc = await _session.SendRequestAsync(msg, id, TimeSpan.FromSeconds(8), ct).ConfigureAwait(false);
            return ParseDefinitionResponse(doc);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsCSharpPath(string filePath) =>
        filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
        || filePath.EndsWith(".csx", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<CideEditorCompletionItem> ParseCompletionResponse(JsonDocument? doc)
    {
        if (doc is null)
            return [];
        var root = doc.RootElement;
        if (root.TryGetProperty("error", out _))
            return [];
        if (!root.TryGetProperty("result", out var result) || result.ValueKind == JsonValueKind.Null)
            return [];

        JsonElement itemsEl;
        if (result.ValueKind == JsonValueKind.Array)
            itemsEl = result;
        else if (result.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            itemsEl = items;
        else
            return [];

        var list = new List<CideEditorCompletionItem>();
        foreach (var item in itemsEl.EnumerateArray())
        {
            var label = item.TryGetProperty("label", out var l)
                ? l.ValueKind == JsonValueKind.String ? l.GetString() ?? "" : l.GetRawText()
                : "";
            if (string.IsNullOrEmpty(label))
                continue;
            var insert = item.TryGetProperty("insertText", out var ins) && ins.ValueKind == JsonValueKind.String
                ? ins.GetString() ?? label
                : label;
            var detail = item.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String
                ? d.GetString()
                : null;
            list.Add(new CideEditorCompletionItem(label, insert, detail));
        }

        return list;
    }

    private static string? ParseSignatureHelpResponse(JsonDocument? doc)
    {
        if (doc is null)
            return null;
        var root = doc.RootElement;
        if (root.TryGetProperty("error", out _))
            return null;
        if (!root.TryGetProperty("result", out var result) || result.ValueKind == JsonValueKind.Null)
            return null;
        if (!result.TryGetProperty("signatures", out var sigs) || sigs.ValueKind != JsonValueKind.Array)
            return null;

        var idx = result.TryGetProperty("activeSignature", out var active) && active.TryGetInt32(out var ai)
            ? ai
            : 0;
        var i = 0;
        foreach (var sig in sigs.EnumerateArray())
        {
            if (i++ != idx)
                continue;
            if (sig.TryGetProperty("label", out var label))
            {
                return label.ValueKind == JsonValueKind.String
                    ? label.GetString()
                    : label.GetRawText();
            }

            break;
        }

        return null;
    }

    private static CideEditorDefinitionLocation? ParseDefinitionResponse(JsonDocument? doc)
    {
        if (doc is null)
            return null;
        var root = doc.RootElement;
        if (root.TryGetProperty("error", out _))
            return null;
        if (!root.TryGetProperty("result", out var result) || result.ValueKind == JsonValueKind.Null)
            return null;

        if (result.ValueKind == JsonValueKind.Array)
        {
            foreach (var loc in result.EnumerateArray())
            {
                var mapped = MapLocation(loc);
                if (mapped is not null)
                    return mapped;
            }

            return null;
        }

        return MapLocation(result);
    }

    private static CideEditorDefinitionLocation? MapLocation(JsonElement loc)
    {
        if (!loc.TryGetProperty("uri", out var uriEl))
            return null;
        var uri = uriEl.GetString();
        if (string.IsNullOrEmpty(uri) || !LspFileUri.TryUriToPath(uri, out var path))
            return null;
        if (!loc.TryGetProperty("range", out var range)
            || !range.TryGetProperty("start", out var start))
            return null;
        if (!TryGetPosition(start, out var line0, out var char0))
            return null;
        return new CideEditorDefinitionLocation(path, line0 + 1, char0 + 1);
    }
}

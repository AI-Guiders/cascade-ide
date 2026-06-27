using System.Text.Json;
using System.Text.Json.Nodes;
using CascadeIDE.Features.Editor.Application.Monaco;
using CascadeIDE.Services.Lsp;

#nullable enable

namespace CascadeIDE.Features.Lsp.DataAcquisition;

/// <summary>LSP <c>textDocument/semanticTokens</c> (ADR 0163 M8 stretch).</summary>
public sealed partial class CSharpLspDiagnosticsHost
{
    private CideEditorSemanticTokensLegend? _semanticLegend;
    private volatile bool _supportsSemanticTokens;

    public bool SupportsSemanticTokens => _supportsSemanticTokens && _semanticLegend is not null;

    public CideEditorSemanticTokensLegend? SemanticLegend => _semanticLegend;

    internal void ApplyServerCapabilities(JsonElement initializeResult)
    {
        _semanticLegend = CSharpLspSemanticTokensParser.TryParseLegend(initializeResult);
        _supportsSemanticTokens = _semanticLegend is not null;
    }

    private void ClearSemanticTokensState()
    {
        _semanticLegend = null;
        _supportsSemanticTokens = false;
    }

    public async Task<CideEditorSemanticTokensData?> RequestSemanticTokensFullAsync(
        string filePath,
        string text,
        CancellationToken ct)
    {
        if (!SupportsSemanticTokens || !IsCSharpPath(filePath) || _session is null)
            return null;

        await SyncFullTextForRequestAsync(filePath, text, ct).ConfigureAwait(false);
        var uri = LspFileUri.PathToFileUri(CanonicalFilePath.Normalize(filePath));
        var id = _session.AllocateRequestId();
        var msg = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = "textDocument/semanticTokens/full",
            ["params"] = new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = uri },
            },
        };

        try
        {
            using var doc = await _session.SendRequestAsync(msg, id, TimeSpan.FromSeconds(15), ct).ConfigureAwait(false);
            return CSharpLspSemanticTokensParser.TryParseFullResponse(doc);
        }
        catch
        {
            return null;
        }
    }
}

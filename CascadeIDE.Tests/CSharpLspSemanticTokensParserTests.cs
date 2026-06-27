using System.Text.Json;
using CascadeIDE.Features.Editor.Application.Monaco;
using CascadeIDE.Features.Lsp.DataAcquisition;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CSharpLspSemanticTokensParserTests
{
    private const string InitializeWithLegend = """
        {
          "capabilities": {
            "semanticTokensProvider": {
              "legend": {
                "tokenTypes": ["namespace", "class", "method"],
                "tokenModifiers": ["declaration", "static"]
              }
            }
          }
        }
        """;

    [Fact]
    public void TryParseLegend_reads_token_types_and_modifiers()
    {
        using var doc = JsonDocument.Parse(InitializeWithLegend);
        var legend = CSharpLspSemanticTokensParser.TryParseLegend(doc.RootElement);
        Assert.NotNull(legend);
        Assert.Equal(["namespace", "class", "method"], legend!.TokenTypes);
        Assert.Equal(["declaration", "static"], legend.TokenModifiers);
    }

    [Fact]
    public void TryParseLegend_returns_null_when_provider_missing()
    {
        using var doc = JsonDocument.Parse("""{"capabilities":{}}""");
        Assert.Null(CSharpLspSemanticTokensParser.TryParseLegend(doc.RootElement));
    }

    [Fact]
    public void TryParseFullResponse_decodes_data_array()
    {
        const string response = """
            {
              "result": {
                "data": [0, 0, 3, 1, 0],
                "resultId": "abc"
              }
            }
            """;
        using var doc = JsonDocument.Parse(response);
        var data = CSharpLspSemanticTokensParser.TryParseFullResponse(doc);
        Assert.NotNull(data);
        Assert.Equal("abc", data!.ResultId);
        Assert.Equal([0u, 0u, 3u, 1u, 0u], data.Data);
    }

    [Fact]
    public void TryParseFullResponse_accepts_empty_data()
    {
        using var doc = JsonDocument.Parse("""{"result":{"data":[]}}""");
        var data = CSharpLspSemanticTokensParser.TryParseFullResponse(doc);
        Assert.NotNull(data);
        Assert.Empty(data!.Data);
    }

    [Fact]
    public void TryParseFullResponse_rejects_malformed_length()
    {
        using var doc = JsonDocument.Parse("""{"result":{"data":[0,0,3,1]}}""");
        Assert.Null(CSharpLspSemanticTokensParser.TryParseFullResponse(doc));
    }
}

using System.Text.Json;
using CascadeIDE.Services.Fm;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class FmOpenAiUsageParserTests
{
    [Fact]
    public void TryParseFromCompletionChunk_reads_usage()
    {
        const string json =
            """
            {
              "choices": [],
              "usage": { "prompt_tokens": 1200, "completion_tokens": 340, "total_tokens": 1540 }
            }
            """;

        var usage = FmOpenAiUsageParser.TryParseFromCompletionChunk(json);
        Assert.NotNull(usage);
        Assert.Equal(1200, usage.PromptTokens);
        Assert.Equal(340, usage.CompletionTokens);
        Assert.Equal(1540, usage.TotalTokens);
    }
}

public sealed class FmUsagePresentationTests
{
    [Fact]
    public void FormatSubtitle_includes_turn_session_and_warn()
    {
        var last = new FmTurnUsage(100_000, 1200, 101_200);
        var session = new FmTurnUsage(150_000, 2400, 152_400);
        var text = FmUsagePresentation.FormatSubtitle(last, session, maxModelLen: 128_000, contextWarnPct: 75);
        Assert.Contains("ход in 100k", text, StringComparison.Ordinal);
        Assert.Contains("сессия 152.4k", text, StringComparison.Ordinal);
        Assert.Contains("⚠ budget", text, StringComparison.Ordinal);
    }
}

public sealed class FmModelCatalogTests
{
    [Fact]
    public void ParseModelList_reads_max_model_len_and_costs()
    {
        using var doc = JsonDocument.Parse(
            """
            {
              "data": [
                {
                  "id": "openai/gpt-oss-120b",
                  "max_model_len": 128000,
                  "metadata": {
                    "prompt_tokens_cost": 0.1,
                    "generated_tokens_cost": 0.2
                  }
                }
              ]
            }
            """);

        var models = FmModelCatalog.ParseModelList(doc.RootElement);
        Assert.Single(models);
        Assert.Equal("openai/gpt-oss-120b", models[0].ModelId);
        Assert.Equal(128_000, models[0].MaxModelLen);
        Assert.Equal(0.1, models[0].PromptTokensCost);
        Assert.Equal(0.2, models[0].GeneratedTokensCost);
    }
}

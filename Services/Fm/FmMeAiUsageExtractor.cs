using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CascadeIDE.Services.Fm;

/// <summary>Извлечение usage из Microsoft.Extensions.AI / MAF ответов.</summary>
public static class FmMeAiUsageExtractor
{
    public static FmTurnUsage? TryFromChatResponse(ChatResponse? response)
    {
        if (response is null)
            return null;

        var usage = response.Usage;
        if (usage is not null)
            return FromUsageDetails(usage);

        return TryFromRawRepresentation(response.RawRepresentation);
    }

    public static FmTurnUsage? TryFromAgentResponse(AgentResponse? response)
    {
        if (response is null)
            return null;

        if (response.Usage is { } agentUsage)
            return FromUsageDetails(agentUsage);

        if (response.Messages is { Count: > 0 })
        {
            FmTurnUsage? merged = null;
            foreach (var msg in response.Messages)
            {
                foreach (var content in msg.Contents)
                {
                    if (content is UsageContent usageContent)
                    {
                        var u = FromUsageDetails(usageContent.Details);
                        if (u is not null)
                            merged = merged is null ? u : merged.Add(u);
                    }
                }
            }

            if (merged is not null)
                return merged;
        }

        return TryFromRawRepresentation(response.RawRepresentation);
    }

    private static FmTurnUsage? FromUsageDetails(UsageDetails? details)
    {
        if (details is null)
            return null;

        int? input = details.InputTokenCount is long i ? (int)Math.Min(i, int.MaxValue) : null;
        int? output = details.OutputTokenCount is long o ? (int)Math.Min(o, int.MaxValue) : null;
        int? total = details.TotalTokenCount is long t ? (int)Math.Min(t, int.MaxValue) : null;
        return FmTurnUsage.TryCreate(input, output, total);
    }

    private static FmTurnUsage? TryFromRawRepresentation(object? raw)
    {
        if (raw is null)
            return null;

        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(raw);
            return FmOpenAiUsageParser.TryParseFromCompletionChunk(json);
        }
        catch
        {
            return null;
        }
    }
}

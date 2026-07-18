namespace CascadeIDE.Services.Fm;

/// <summary>Токены одного FM-хода (OpenAI-compatible <c>usage</c>).</summary>
public sealed record FmTurnUsage(
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens)
{
    public static FmTurnUsage? TryCreate(int? prompt, int? completion, int? total)
    {
        if (prompt is null && completion is null && total is null)
            return null;

        var p = Math.Max(0, prompt ?? 0);
        var c = Math.Max(0, completion ?? 0);
        var t = total ?? p + c;
        if (t <= 0 && p <= 0 && c <= 0)
            return null;

        if (t <= 0)
            t = p + c;

        return new FmTurnUsage(p, c, t);
    }

    public FmTurnUsage Add(FmTurnUsage other) =>
        new(PromptTokens + other.PromptTokens, CompletionTokens + other.CompletionTokens, TotalTokens + other.TotalTokens);
}

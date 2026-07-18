namespace CascadeIDE.Services.Fm;

/// <summary>Форматирование FM usage для Intercom chrome subtitle.</summary>
public static class FmUsagePresentation
{
    public static string FormatSubtitle(
        FmTurnUsage? lastTurn,
        FmTurnUsage? sessionTotals,
        int? maxModelLen,
        int contextWarnPct)
    {
        if (lastTurn is null && sessionTotals is null)
            return "";

        var parts = new List<string>(4);
        if (lastTurn is not null)
            parts.Add($"ход in {FormatK(lastTurn.PromptTokens)} · out {FormatK(lastTurn.CompletionTokens)}");

        if (sessionTotals is not null && (lastTurn is null || sessionTotals.TotalTokens != lastTurn.TotalTokens))
            parts.Add($"сессия {FormatK(sessionTotals.TotalTokens)}");

        if (maxModelLen is > 0 && lastTurn is not null)
        {
            var pct = (int)Math.Round(100.0 * lastTurn.PromptTokens / maxModelLen.Value);
            parts.Add($"ctx {FormatK(lastTurn.PromptTokens)}/{FormatK(maxModelLen.Value)} ({pct}%)");
            if (contextWarnPct > 0 && pct >= contextWarnPct)
                parts.Add("⚠ budget");
        }

        return string.Join(" · ", parts);
    }

    private static string FormatK(int tokens) =>
        tokens >= 10_000
            ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{tokens / 1000.0:0.#}k")
            : tokens.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

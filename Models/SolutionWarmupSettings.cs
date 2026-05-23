namespace CascadeIDE.Models;

/// <summary>Прогрев при открытии solution (ADR 0141). TOML: <c>[solution_warmup]</c>.</summary>
public sealed class SolutionWarmupSettings
{
    public bool Enabled { get; set; } = true;

    public bool WarmActiveFileOnSolutionOpen { get; set; } = true;

    public bool WarmFeedAnchorsAfterSymbolSidecar { get; set; } = true;

    public bool WarmOpenDocuments { get; set; } = true;

    public bool WarmRecentCsFiles { get; set; } = true;

    /// <summary>Макс. параллельных file-job (bracket/L1). Отдельно от HCI I/O.</summary>
    public int MaxParallelFileJobs { get; set; } = 2;

    /// <summary>Сколько открытых вкладок .cs прогревать (P2).</summary>
    public int MaxOpenDocumentFiles { get; set; } = 6;

    /// <summary>Компактная полоса на зоне Forward пока идёт warm-up / HCI (ADR 0141).</summary>
    public bool ShowBackgroundStatusOnPfd { get; set; } = true;
}
